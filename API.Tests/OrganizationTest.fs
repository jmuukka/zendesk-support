module Tests

open System
open Mutex.Zendesk.Support.API

let context =
    {
        BaseUrl = Uri("https://XXXXXXXXXX.zendesk.com", UriKind.Absolute)
        Credentials = UsernameToken {
            Username = "XXXXXXXXXX@XXXXXXXXXX.XXXXXXXXXX"
            Token = "XXXXXXXXXX"
        }
    }

open System.Net
open Xunit

[<Fact>]
let ``getAll organizations returns Ok`` () =

    let actual = Http.getArray Http.send Organization.getAll context

    Assert.ok actual

[<Fact>]
let ``get organization by identifier returns Ok`` () =
    let getById = Organization.get 0000000000L

    let actual = Http.get Http.send getById context

    Assert.ok actual
