module HttpUnitTests

open System
open System.Collections.Generic
open System.Net
open System.Net.Http
open Xunit
open Mutex.Zendesk.Support.API

let context =
    {
        BaseUrl = Uri("https://localhost/", UriKind.Absolute)
        Credentials = UsernameToken {
            Username = "will.be@igno.red"
            Token = "it's a secret :)"
        }
    }

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

let createResponse (queue : Queue<ItemsModel>) =
    let items = queue.Dequeue()
    let content = Json.serialize items
    let response = new HttpResponseMessage(HttpStatusCode.OK)
    response.Content <- new StringContent(content, System.Text.Encoding.UTF8, "application/json")
    response

let ``arrange for getArray`` () =
    let firstPage = "http://api/"
    let nextPages = [firstPage + "?page=2"; firstPage + "?page=3"; null]
    let queue = createQueue nextPages
    let uris = List<string>()
    let send : HttpSend =
        fun ctx createReq ->
            let request = createReq ctx
            uris.Add(request.RequestUri.AbsoluteUri)
            createResponse queue
            |> Ok
    firstPage, nextPages, queue, uris, send

[<Fact>]
let ``getArray with many pages then data of all pages received`` () =
    let firstPage, _, _, _, send = ``arrange for getArray``()

    let actual = Http.getArray<ItemsModel, Item> send context (fun model -> model.items) firstPage

    match actual with
    | Ok items ->
        Assert.equals [|{id=1}; {id=2}; {id=3}|] items
    | _ ->
        failwith "Error"

[<Fact>]
let ``getArray with many pages then pages consumed and expected pages requested`` () =
    let firstPage, nextPages, queue, uris, send = ``arrange for getArray``()

    Http.getArray<ItemsModel, Item> send context (fun model -> model.items) firstPage
    |> ignore

    Assert.equals 0 queue.Count
    Assert.equals (firstPage :: List.take 2 nextPages) (uris |> List.ofSeq)

//[<Fact>]
//let ``send`` () =
//    ()
