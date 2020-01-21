namespace Mutex.Zendesk.Support.API

module Json =

    open Newtonsoft.Json

    let serialize obj =
        JsonConvert.SerializeObject(obj)

    let deserialize<'t> json =
        try
            JsonConvert.DeserializeObject<'t>(json)
            |> Ok
        with ex ->
            Error ex
