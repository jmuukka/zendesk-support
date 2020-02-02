namespace Mutex.Zendesk.Support.API

module Zendesk =

    val get : HttpSend -> GetCommand<'infra, 'model> -> Context -> Result<'model, Failure>

    val tryGet : HttpSend -> GetCommand<'infra, 'model> -> Context -> Result<'model option, Failure>

    val getArray<'infra, 'model when 'infra :> PagedModel> : HttpSend -> GetCommand<'infra, 'model array> -> Context -> Result<'model array, Failure>

    val tryGetFromArray : HttpSend -> GetCommand<'infra, 'model array> -> Context -> Result<'model option, Failure>

    val post : HttpSend -> PostCommand<'infra, 'model> -> Context -> Result<'model, Failure>

    val put : HttpSend -> PutCommand<'infra, 'model> -> Context -> Result<'model, Failure>

    val delete : HttpSend -> DeleteCommand -> Context -> Result<unit, Failure>
