namespace Mutex.Zendesk.Support.API

open System.Net.Http

module Http =

    val createRequest : HttpMethod -> string -> Context -> HttpRequestMessage

    val acceptJson : HttpRequestMessage -> HttpRequestMessage

    val content : Content -> HttpRequestMessage -> HttpRequestMessage

    val parse<'t> : HttpResponseMessage -> Result<'t, Failure>

    val send : CreateRequest -> Result<HttpResponseMessage, Failure>

    val sendWithRetryWhenTooManyRequests : HttpSend -> CreateRequest -> Result<HttpResponseMessage, Failure>

    val sendWithRetry : int -> int -> HttpSend -> CreateRequest -> Result<HttpResponseMessage, Failure>
