namespace Mutex.Zendesk.Support.API

open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Text
open System.Threading

module Http =

    let private client = new HttpClient()

    let private authorization context (request : HttpRequestMessage) =

        let authentication creds =

            let basicAuthentication parameter =
                AuthenticationHeaderValue("Basic", parameter)

            let authentication (format : string) (first : string) (second : string) =
                System.String.Format(format, first, second)
                |> Encoding.UTF8.GetBytes
                |> System.Convert.ToBase64String
                |> basicAuthentication

            match creds with
            | UsernamePassword creds ->
                authentication "{0}:{1}" creds.Username creds.Password
            | UsernameToken creds ->
                authentication "{0}/token:{1}" creds.Username creds.Token

        request.Headers.Authorization <- authentication context.Credentials
        request

    let private content obj (request : HttpRequestMessage) =
        let json = Json.serialize obj
        let content = new StringContent(json, Encoding.UTF8, "application/json")

        request.Content <- content
        request

    let private request httpMethod (requestUri : string) context =
        let uri = System.Uri(context.BaseUrl, requestUri)

        new HttpRequestMessage(httpMethod, uri)
        |> authorization context

    let private GET uri context =
        request HttpMethod.Get uri context

    let private POST uri context obj =
        request HttpMethod.Post uri context
        |> content obj

    let send context createRequest =
        let request = createRequest context

        try
            client.SendAsync(request)
            |> Async.AwaitTask
            |> Async.RunSynchronously
            |> Ok
        with
            | ex ->
                Error (Exception ex)

    let sendWithRetryWhenTooManyRequests (send : HttpSend) context createRequest =
        let rec sendRec tryCount maxCount =
            let result = send context createRequest
            match result with
            | Ok (response : HttpResponseMessage) ->
                if (int response.StatusCode) <> 429 then
                    result
                elif tryCount >= maxCount then
                    result
                else
                    let retryCondition = response.Headers.RetryAfter
                    let seconds = retryCondition.Delta
                    if seconds.HasValue then
                        Thread.Sleep(seconds.Value)
                    sendRec (tryCount + 1) maxCount
            | _ ->
                result

        sendRec 1 5

    let sendWithRetry firstWaitTimeInSeconds maxRetryCount (send : HttpSend) context createRequest =
        let shouldFail tryCount maxRetryCount failure =
            let trueWhenShouldNotRetry =
                tryCount >= maxRetryCount

            match failure with
            | StatusCode (code, _) when code >= HttpStatusCode.InternalServerError ->
                trueWhenShouldNotRetry
            | StatusCode _ ->
                // HttpStatusCodes less than 500 are considered permanent.
                // 429 at least is an exception to this, as it instructs to try again after a delay.
                // Function sendWithRetryWhenTooManyRequests can be used to handle this case.
                true
            | ParseError _ ->
                // We will expect that it's a permanent error.
                true
            | CustomError _ ->
                // We will expect that it's a permanent error.
                true
            | Exception _ ->
                trueWhenShouldNotRetry

        let rec sendRec tryCount waitTimeInSeconds =
            let result = send context createRequest
            match result with
            | Ok value ->
                Ok value
            | Error failure ->
                if shouldFail tryCount maxRetryCount failure then
                    Error failure
                else
                    Thread.Sleep (waitTimeInSeconds * 1000)
                    sendRec (tryCount + 1) (waitTimeInSeconds * 2)

        sendRec 1 firstWaitTimeInSeconds

    let private read (content : HttpContent) =
        content.ReadAsStringAsync() // Hopefully the position in the stream is at the beginning when this is called.
        |> Async.AwaitTask
        |> Async.RunSynchronously

    let private deserialize<'t> (content : string) =
        let result = Json.deserialize<'t> content

        match result with
        | Ok value -> Ok value
        | Error (json, ex) -> Error (ParseError (json, ex))

    let private parse<'t> (response : HttpResponseMessage) =
        let content = read response.Content
        if response.IsSuccessStatusCode then
            deserialize<'t> content
        else
            Error (StatusCode (response.StatusCode, content))

    let get<'a, 'b> (send : HttpSend) context (inner : 'a -> 'b) requestUri =

        let createRequest : CreateRequest =
            fun ctx ->
                GET requestUri ctx

        send context createRequest
        |> Result.bind parse<'a>
        |> Result.map inner

    let getArray<'a, 'b when 'a :> PagedModel> (send : HttpSend) context (inner : 'a -> 'b array) requestUri =

        let rec get requestUri' acc =
            let createRequest : CreateRequest =
                fun ctx ->
                    GET requestUri' ctx

            let result =
                send context createRequest
                |> Result.bind parse<'a>

            match result with
            | Error err ->
                Error err
            | Ok page ->
                let elements = inner page
                let acc' = Array.append acc elements
                if page.next_page = null then
                    Ok acc'
                else
                    get page.next_page acc'

        get requestUri [||]

    let post<'a, 'b> (send : HttpSend) context obj (inner : 'a -> 'b) requestUri =

        let createRequest : CreateRequest =
            fun ctx ->
                request HttpMethod.Post requestUri ctx
                |> content obj

        send context createRequest
        |> Result.bind parse<'a>
        |> Result.map inner

    let put<'a, 'b> (send : HttpSend) context obj (inner : 'a -> 'b) requestUri =

        let createRequest : CreateRequest =
            fun ctx ->
                request HttpMethod.Put requestUri ctx
                |> content obj

        send context createRequest
        |> Result.bind parse<'a>
        |> Result.map inner
