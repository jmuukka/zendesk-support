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
| Exception of exn

type CreateRequest = Context -> HttpRequestMessage

type HttpSend = Context -> CreateRequest -> Result<HttpResponseMessage, Failure>

type PagedModel() =
    member val next_page : string = null with get, set
    member val previous_page : string = null with get, set
    member val count : int = 0 with get, set
