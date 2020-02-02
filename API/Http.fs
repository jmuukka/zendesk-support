namespace Mutex.Zendesk.Support.API

open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Text
open System.Threading

module Http =

    let private client = new HttpClient()

    let authorization context (request : HttpRequestMessage) =

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

    let acceptJson (request : HttpRequestMessage) =
        let jsonMediaType = MediaTypeWithQualityHeaderValue("application/json")

        request.Headers.Accept.Add(jsonMediaType)
        request

    let content (content : Content) (request : HttpRequestMessage) =
        let content =
            match content with
            | JsonContent (JsonString json) ->
                new StringContent(json, Encoding.UTF8, "application/json")

        request.Content <- content
        request

    let createRequest httpMethod (requestUri : string) context =
        let uri = System.Uri(context.BaseUrl, requestUri)

        new HttpRequestMessage(httpMethod, uri)
        |> authorization context

    let read (content : HttpContent) =
        content.ReadAsStringAsync()
        |> Async.AwaitTask
        |> Async.RunSynchronously

    let deserialize<'t> (content : string) =
        let result = Json.deserialize<'t> content

        match result with
        | Ok value -> Ok value
        | Error (json, ex) -> Error (ParseError (json, ex))

    let parse<'t> (response : HttpResponseMessage) =
        read response.Content
        |> deserialize<'t>

    let mapResponse (response : HttpResponseMessage) =
        if response.IsSuccessStatusCode then
            Ok response
        else
            response
            |> ResponseError
            |> Error

    let send (createRequest : CreateRequest) =
        try
            createRequest()
            |> client.SendAsync
            |> Async.AwaitTask
            |> Async.RunSynchronously
            |> mapResponse
        with
            | ex ->
                Error (Exception ex)

    let sendWithRetryWhenTooManyRequests (send : HttpSend) (createRequest : CreateRequest) =
        let rec sendRec tryCount maxCount =
            let result = send createRequest
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

    let sendWithRetry firstWaitTimeInSeconds maxRetryCount (send : HttpSend) (createRequest : CreateRequest) =
        let shouldFail tryCount maxRetryCount failure =
            let trueWhenShouldNotRetry =
                tryCount >= maxRetryCount

            match failure with
            | ResponseError response when response.StatusCode >= HttpStatusCode.InternalServerError ->
                trueWhenShouldNotRetry
            | ResponseError _ ->
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
            let result = send createRequest
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
