module Configuration

open System
open Mutex.Zendesk.Support.API

type Configuration = {
    BaseUrl : string
    Username : string
    Token : string
    ExistingOrganizationId: OrganizationId
}

let config =
    let res =
        System.IO.File.ReadAllText("..\..\..\Configuration.json")
        |> Json.deserialize<Configuration>

    match res with
    | Ok config ->
        config
    | Error (text, ex) ->
        failwith ex.Message

let context =
    {
        BaseUrl = Uri(config.BaseUrl, UriKind.Absolute)
        Credentials = UsernameToken {
            Username = config.Username
            Token = config.Token
        }
    }

let existingOrganizationId =
    config.ExistingOrganizationId
