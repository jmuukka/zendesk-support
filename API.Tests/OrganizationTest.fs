module Tests

open System
open Mutex.Zendesk.Support.API

let context = Configuration.context

open System.Net
open Xunit

[<Fact>]
let ``getAll organizations returns Ok`` () =

    let actual = Http.getArray Http.send Organization.getAll context

    Assert.ok actual

[<Fact>]
let ``get organization by identifier returns Ok`` () =
    let getById = Organization.get Configuration.existingOrganizationId

    let actual = Http.get Http.send getById context

    Assert.ok actual

[<Fact>]
let ``delete unknown organization by identifier returns Ok`` () =
    let deleteById = Organization.delete Int64.MaxValue

    let actual = Http.delete Http.send deleteById context

    Assert.ok actual
