module HttpUnitTests

open System
open System.Collections.Generic
open System.Net
open System.Net.Http
open Xunit
open Mutex.Zendesk.Support.API

type Item = {
    id : int
}

type ItemsModel() =
    inherit PagedModel()

    member val items : Item array = null with get, set

let createItemsModel id next =
    let model = ItemsModel()
    model.items <- [|{id = id}|]
    model.next_page <- next
    model

let createQueue uris =
    let queue = new Queue<ItemsModel>()

    let enqueue uri =
        let items = createItemsModel (queue.Count + 1) uri
        queue.Enqueue(items)

    uris |> List.iter enqueue
    queue

let context =
    {
        BaseUrl = Uri("https://localhost/", UriKind.Absolute)
        Credentials = UsernameToken {
            Username = "will.be@igno.red"
            Token = "it's a secret :)"
        }
    }

// This has too many responsibilities. Need to split it.
[<Fact>]
let ``getArray with many pages then all pages received`` () =
    let firstPage = "http://api/"
    let nextPages = [firstPage + "?page=2"; firstPage + "?page=3"; null]
    let expectedPages = firstPage :: List.take 2 nextPages
    let queue = createQueue nextPages
    let mutable uris = []

    let send : HttpSend =
        fun ctx createReq ->
            let request = createReq ctx
            uris <- uris @ [request.RequestUri.AbsoluteUri]
            let items = queue.Dequeue()
            let content = Json.serialize items
            let response = new HttpResponseMessage(HttpStatusCode.OK)
            response.Content <- new StringContent(content, System.Text.Encoding.UTF8, "application/json")
            Ok response

    let actual = Http.getArray<ItemsModel, Item> send context (fun model -> model.items) firstPage

    Assert.equals 0 queue.Count
    Assert.equals expectedPages uris
    Assert.ok actual
    match actual with
    | Ok items ->
        Assert.equals [|{id=1}; {id=2}; {id=3}|] items
        ()
    | _ -> ()

//[<Fact>]
//let ``send`` () =
//    ()
