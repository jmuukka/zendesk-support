namespace Mutex.Zendesk.Support.API

module Result =

    open System.Net

    let mapNotFoundToNone res =
        match res with
        | Ok value ->
            Ok (Some value)
        | Error err ->
            match err with
            | StatusCode (code, _) when code = HttpStatusCode.NotFound ->
                Ok None
            | _ ->
                Error err
