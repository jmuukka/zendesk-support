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

    let content (content : Content) (request : HttpRequestMessage) =
        let content = new StringContent(content.Content, Encoding.UTF8, content.ContentType)

        request.Content <- content
        request

    let createRequest httpMethod (requestUri : string) context =
        let uri = System.Uri(context.BaseUrl, requestUri)

        new HttpRequestMessage(httpMethod, uri)
        |> authorization context

    let read (content : HttpContent) =
        content.ReadAsStringAsync() // Hopefully the position in the stream is at the beginning when this is called.
        |> Async.AwaitTask
        |> Async.RunSynchronously

    let deserialize<'t> (content : string) =
        let result = Json.deserialize<'t> content

        match result with
        | Ok value -> Ok value
        | Error (json, ex) -> Error (ParseError (json, ex))

    let parse<'t> (response : HttpResponseMessage) =
        let content = read response.Content
        if response.IsSuccessStatusCode then
            deserialize<'t> content
        else
            Error (StatusCode (response.StatusCode, content))

    let send (createRequest : CreateRequest) =
        try
            createRequest()
            |> client.SendAsync
            |> Async.AwaitTask
            |> Async.RunSynchronously
            |> Ok
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

    let get (send : HttpSend) (command : GetCommand<'infra, 'model>) context =
        fun () ->
            createRequest HttpMethod.Get command.Uri context
        |> send
        |> Result.bind parse<'infra>
        |> Result.map command.Map

    let tryGet send command context =
        get send command context
        |> Result.mapNotFoundToNone

    let getArray<'infra, 'model when 'infra :> PagedModel> (send : HttpSend) (command : GetCommand<'infra, 'model array>) context : Result<'model array, Failure> =
        let rec get (cmd : GetCommand<'infra, 'model array>) acc =
            let result =
                fun () ->
                    createRequest HttpMethod.Get cmd.Uri context
                |> send
                |> Result.bind parse<'infra>
            match result with
            | Error err ->
                Error err
            | Ok page ->
                let elements = cmd.Map page
                let accumulated = Array.append acc elements
                if page.next_page = null then
                    Ok accumulated
                else
                    let cmd = { cmd with Uri = page.next_page }
                    get cmd accumulated
        get command [||]

    let tryGetFromArray send (command : GetCommand<'infra, 'model array>) context =
        let result = get send command context
        match result with
        | Ok [||] ->
            Ok None
        | Ok [|model|] ->
            Ok (Some model)
        | Ok models ->
            sprintf "Expected zero or one %s. The response contains %i." typeof<'model>.Name models.Length
            |> CustomError
            |> Error
        | Error err ->
            Error err

    let post (send : HttpSend) (command : PostCommand<'infra, 'model>) context =
        fun () ->
            createRequest HttpMethod.Post command.Uri context
            |> content command.Content
        |> send
        |> Result.bind parse<'infra>
        |> Result.map command.Map

    let put (send : HttpSend) (command : PutCommand<'infra, 'model>) context =
        fun () ->
            createRequest HttpMethod.Put command.Uri context
            |> content command.Content
        |> send
        |> Result.bind parse<'infra>
        |> Result.map command.Map

    let delete (send : HttpSend) (command : DeleteCommand) context =
        fun () ->
            createRequest HttpMethod.Delete command.Uri context
        |> send
        |> Result.map (fun response -> ())
