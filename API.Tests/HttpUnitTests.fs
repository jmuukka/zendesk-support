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

type ItemModel = {
    item : Item
}

type ItemsModel() =
    inherit PagedModel()

    member val items : Item array = null with get, set

let createItemModel id =
    {
        item = {id = id}
    }

let createItemsModel id next =
    let model = ItemsModel()
    model.items <- [|{id = id}|]
    model.next_page <- next
    model

let createQueue uris =
    let queue = Queue<ItemsModel>()

    let enqueue uri =
        let items = createItemsModel (queue.Count + 1) uri
        queue.Enqueue(items)

    uris |> List.iter enqueue
    queue

let createResponse model =
    let json = Json.serialize model
    let response = new HttpResponseMessage(HttpStatusCode.OK)
    response.Content <- new StringContent(json, System.Text.Encoding.UTF8, "application/json")
    response

// ------------------------------------------------------------
// get
// ------------------------------------------------------------

let getItem (model : ItemModel) = model.item

[<Fact>]
let ``get returns expected model when send succeeds`` () =
    let send ctx createReq =
        createItemModel 1
        |> createResponse
        |> Ok
            
    let actual = Http.get send context getItem "https://...will be ignored"

    match actual with
    | Ok item ->
        Assert.equals {id=1} item
    | _ ->
        failwith "Error"

[<Fact>]
let ``get returns HTTP NotFound Error when send fails`` () =
    let notFound = StatusCode HttpStatusCode.NotFound
    let send ctx createReq = Error notFound
            
    let actual = Http.get send context getItem "https://...will be ignored"

    match actual with
    | Ok _ ->
        failwith "Why Ok? Should be Error!"
    | Error failure ->
        Assert.equals notFound failure

[<Fact>]
let ``get returns ParseError when server returns HTML page`` () =
    let responseText = "<html>...</html>"
    let createResponse text =
        let response = new HttpResponseMessage(HttpStatusCode.OK)
        // Sometimes they lie that the response is JSON even though it's an HTML error page.
        response.Content <- new StringContent(text, System.Text.Encoding.UTF8, "application/json")
        response
        
    let send ctx createReq =
        responseText
        |> createResponse
        |> Ok
            
    let actual = Http.get send context getItem "https://...will be ignored"

    match actual with
    | Ok _ ->
        failwith "Why Ok? Should be Error!"
    | Error failure ->
        match failure with
        | ParseError (html, _) ->
            Assert.equals responseText html
        | _ ->
            failwith "ParseError was expected!"

// ------------------------------------------------------------
// getArray
// ------------------------------------------------------------

let ``arrange for getArray`` () =
    let firstPage = "http://api/"
    let nextPages = [firstPage + "?page=2"; firstPage + "?page=3"; null]
    let queue = createQueue nextPages
    let uris = List<string>()
    let send : HttpSend =
        fun ctx createReq ->
            let request = createReq ctx
            uris.Add(request.RequestUri.AbsoluteUri)

            queue.Dequeue()
            |> createResponse
            |> Ok

    firstPage, nextPages, queue, uris, send

let getArray = Http.getArray<ItemsModel, Item>

let getItems (model : ItemsModel) = model.items

[<Fact>]
let ``getArray with many pages then data of all pages received`` () =
    let firstPage, _, _, _, send = ``arrange for getArray``()

    let actual = getArray send context getItems firstPage

    match actual with
    | Ok items ->
        Assert.equals [|{id=1}; {id=2}; {id=3}|] items
    | _ ->
        failwith "Why Error? Should be Ok!"

[<Fact>]
let ``getArray with many pages then pages consumed and expected pages requested`` () =
    let firstPage, nextPages, queue, uris, send = ``arrange for getArray``()

    getArray send context getItems firstPage
    |> ignore

    Assert.equals 0 queue.Count
    Assert.equals (firstPage :: List.take 2 nextPages) (List.ofSeq uris)

//[<Fact>]
//let ``send`` () =
//    ()
