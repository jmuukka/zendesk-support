module OrganizationTests

open System
open System.Net
open Xunit
open Mutex.Zendesk.Support.API

let context = Configuration.context

[<Fact>]
let ``getAll organizations returns Ok`` () =

    let actual = Zendesk.getArray Http.send Organization.getAll context

    Assert.ok actual

[<Fact>]
let ``get organization by identifier returns Ok`` () =
    let getById = Organization.get Configuration.existingOrganizationId

    let actual = Zendesk.get Http.send getById context

    Assert.ok actual

[<Fact>]
let ``delete unknown organization by identifier returns Ok`` () =
    let deleteById = Organization.delete Int64.MaxValue

    let actual = Zendesk.delete Http.send deleteById context

    Assert.ok actual
