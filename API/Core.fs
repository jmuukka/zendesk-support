namespace Mutex.Zendesk.Support.API

open System
open System.Net
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
| StatusCode of HttpStatusCode * string // The response status code and the content.
| ParseError of string * exn // The content that we could not parse and an exception.
| CustomError of string
| Exception of exn

type CreateRequest = unit -> HttpRequestMessage

type HttpSend = CreateRequest -> Result<HttpResponseMessage, Failure>

type PagedModel() =
    member val next_page : string = null with get, set
    member val previous_page : string = null with get, set
    member val count : int = 0 with get, set

type Content = {
    ContentType : string
    Content : string
}

module Content =

    let json obj =
        {
            ContentType = "application/json"
            Content = Json.serialize obj
        }

type DeleteCommand = {
    Uri : string
}

[<NoComparison>]
[<NoEquality>]
type GetCommand<'infraModel, 'model> = {
    Uri : string
    Map : 'infraModel -> 'model
}

[<NoComparison>]
[<NoEquality>]
type PostCommand<'infraModel, 'model> = {
    Uri : string
    Map : 'infraModel -> 'model
    Content : Content
}

[<NoComparison>]
[<NoEquality>]
type PutCommand<'infraModel, 'model> = {
    Uri : string
    Map : 'infraModel -> 'model
    Content : Content
}

module Request =

    let get (uri : string) (mapFromInfra : 'infra -> 'model) : GetCommand<'infra, 'model> =
        {
            Uri = uri
            Map = mapFromInfra
        }

    let post (uri : string) (model : 'newmodel) (mapToInfra : 'newmodel -> 'newinfra) (mapFromInfra : 'infra -> 'model) : PostCommand<'infra, 'model> =
        {
            Uri = uri
            Content = Content.json (mapToInfra model)
            Map = mapFromInfra
        }

    let put (uri : string) (model : 'model) (mapToInfra : 'model -> 'infra) (mapFromInfra : 'infra -> 'model) : PutCommand<'infra, 'model> =
        {
            Uri = uri
            Content = Content.json (mapToInfra model)
            Map = mapFromInfra
        }

    let delete (uri : string) : DeleteCommand =
        {
            Uri = uri
        }
