namespace Mutex.Zendesk.Support.API

module Zendesk =

    val get : HttpSend -> Context -> GetCommand<'infra, 'model> -> Result<'model, Failure>

    val tryGet : HttpSend -> Context -> GetCommand<'infra, 'model> -> Result<'model option, Failure>

    val getArray<'infra, 'model when 'infra :> PagedModel> : HttpSend -> Context -> GetCommand<'infra, 'model array> -> Result<'model array, Failure>

    val tryGetFromArray : HttpSend -> Context -> GetCommand<'infra, 'model array> -> Result<'model option, Failure>

    val post : HttpSend -> Context -> PostCommand<'infra, 'model> -> Result<'model, Failure>

    val put : HttpSend -> Context -> PutCommand<'infra, 'model> -> Result<'model, Failure>

    val delete : HttpSend -> Context -> DeleteCommand -> Result<unit, Failure>
