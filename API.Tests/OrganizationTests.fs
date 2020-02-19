module OrganizationTests

open System
open System.Net
open Xunit
open Mutex.Zendesk.Support.API

let context = Configuration.context

[<Fact>]
let ``getAll organizations returns Ok`` () =

    let actual = Zendesk.getArray Http.send context Organization.getAll

    Assert.ok actual

[<Fact>]
let ``get organization by identifier returns Ok`` () =
    let getById = Organization.get Configuration.existingOrganizationId

    let actual = Zendesk.get Http.send context getById

    Assert.ok actual

[<Fact>]
let ``delete unknown organization by identifier returns Ok`` () =
    let deleteById = Organization.delete Int64.MaxValue

    let actual = Zendesk.delete Http.send context deleteById

    Assert.ok actual
