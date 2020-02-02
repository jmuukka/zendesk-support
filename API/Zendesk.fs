namespace Mutex.Zendesk.Support.API

open System.Net
open System.Net.Http

module Zendesk =

    let get (send : HttpSend) (command : GetCommand<'infra, 'model>) context =
        fun () ->
            Http.createRequest HttpMethod.Get command.Uri context
            |> Http.acceptJson
        |> send
        |> Result.bind Http.parse<'infra>
        |> Result.map command.Map

    // TODO we could add get function as parameter so the caller can add functionality to it
    let tryGet send command context =
        get send command context
        |> Result.mapNotFoundErrorToOkNone

    let getArray<'infra, 'model when 'infra :> PagedModel> (send : HttpSend) (command : GetCommand<'infra, 'model array>) context : Result<'model array, Failure> =
        let rec get (cmd : GetCommand<'infra, 'model array>) acc =
            let result =
                fun () ->
                    Http.createRequest HttpMethod.Get cmd.Uri context
                    |> Http.acceptJson
                |> send
                |> Result.bind Http.parse<'infra>
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
            Http.createRequest HttpMethod.Post command.Uri context
            |> Http.content command.Content
            |> Http.acceptJson
        |> send
        |> Result.bind Http.parse<'infra>
        |> Result.map command.Map

    let put (send : HttpSend) (command : PutCommand<'infra, 'model>) context =
        fun () ->
            Http.createRequest HttpMethod.Put command.Uri context
            |> Http.content command.Content
            |> Http.acceptJson
        |> send
        |> Result.bind Http.parse<'infra>
        |> Result.map command.Map

    let delete (send : HttpSend) (command : DeleteCommand) context =
        fun () ->
            Http.createRequest HttpMethod.Delete command.Uri context
        |> send
        |> Result.mapNotFoundErrorToOk
        |> Result.map (fun response -> ())
