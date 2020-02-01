namespace Mutex.Zendesk.Support.API

module Result =

    open System.Net

    let mapNotFoundErrorToOkNone res =
        match res with
        | Ok value ->
            Ok (Some value)
        | Error err ->
            match err with
            | ResponseError response when response.StatusCode = HttpStatusCode.NotFound ->
                Ok None
            | _ ->
                Error err

    open System.Net.Http

    let mapNotFoundErrorToOk (res : Result<HttpResponseMessage, Failure>) =
        match res with
        | Error (ResponseError response) when response.StatusCode = HttpStatusCode.NotFound ->
            Ok response
        | _ ->
            res
