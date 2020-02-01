namespace Mutex.Zendesk.Support.API

open System
open System.Net.Http

type UsernamePasswordCredentials = {
    Username : string
    Password : string
}

type UsernameTokenCredentials = {
    Username : string
    Token : string
}

type Credentials =
| UsernamePassword of UsernamePasswordCredentials
| UsernameToken of UsernameTokenCredentials

[<NoComparison>]
type Context = {
    BaseUrl : Uri
    Credentials : Credentials
}

[<NoComparison>]
type Failure =
| ResponseError of HttpResponseMessage
| ParseError of string * exn // The content that we could not parse and an exception.
| CustomError of string
| Exception of exn

type CreateRequest = unit -> HttpRequestMessage

type HttpSend = CreateRequest -> Result<HttpResponseMessage, Failure>

type PagedModel() =
    member val next_page : string = null with get, set
    member val previous_page : string = null with get, set
    member val count : int = 0 with get, set

type JsonString = JsonString of string

type Content =
| JsonContent of JsonString

module JsonString =
    let create obj =
        Json.serialize obj
        |> JsonString

type DeleteCommand = {
    Uri : string
}

[<NoComparison>]
[<NoEquality>]
type GetCommand<'infra, 'model> = {
    Uri : string
    Map : 'infra -> 'model
}

[<NoComparison>]
[<NoEquality>]
type PostCommand<'infra, 'model> = {
    Uri : string
    Map : 'infra -> 'model
    Content : Content
}

[<NoComparison>]
[<NoEquality>]
type PutCommand<'infra, 'model> = {
    Uri : string
    Map : 'infra -> 'model
    Content : Content
}

module Command =

    let private jsonContent model map =
        model
        |> map
        |> JsonString.create
        |> JsonContent

    let get uri map : GetCommand<'infra, 'model> =
        {
            Uri = uri
            Map = map
        }

    let post uri (model : 'newmodel) (mapToInfra : 'newmodel -> 'newinfra) (mapFromInfra : 'infra -> 'model) : PostCommand<'infra, 'model> =
        {
            Uri = uri
            Content = jsonContent model mapToInfra
            Map = mapFromInfra
        }

    let put uri model (mapToInfra : 'model -> 'infra) (mapFromInfra : 'infra -> 'model) : PutCommand<'infra, 'model> =
        {
            Uri = uri
            Content = jsonContent model mapToInfra
            Map = mapFromInfra
        }

    let delete uri : DeleteCommand =
        {
            Uri = uri
        }
