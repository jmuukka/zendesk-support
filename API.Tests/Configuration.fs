module Configuration

open System
open System.IO
open Mutex.Zendesk.Support.API

type Configuration = {
    BaseUrl : string
    Username : string
    Token : string
    ExistingOrganizationId: OrganizationId
}

let private configFilename = "Configuration.json"
let private configTemplateFilename = "ConfigurationTemplate.json"

let private makeFilepath filename = "..\\..\\..\\" + filename

let private fileExists filename =
    filename
    |> makeFilepath
    |> File.Exists

let private copyFile sourceFilename targetFilename =
    let sourceFilepath = makeFilepath sourceFilename
    let targetFilepath = makeFilepath targetFilename
    File.Copy(sourceFilepath, targetFilepath)

let private readFile filename =
    filename
    |> makeFilepath
    |> File.ReadAllText

let private failwithPleaseUpdate () =
    configFilename
    |> sprintf "Please, update the content of the %s file before running the tests."
    |> failwith

let private validateString (s : string) =
    if s = null || s.Trim().Length = 0 || s.Contains("?") then
        failwithPleaseUpdate()

let private validate config =
    validateString config.BaseUrl
    validateString config.Username
    validateString config.Token
    if config.ExistingOrganizationId = 0L then
        failwithPleaseUpdate()

let private readConfig filename =
    let res =
        readFile filename
        |> Json.deserialize<Configuration>

    match res with
    | Ok config ->
        validate config
        config
    | Error (_, ex) ->
        failwith ex.Message

let private config =
    if fileExists configFilename then
        readConfig configFilename
    else
        copyFile configTemplateFilename configFilename

        sprintf "The file %s was created. Please, update the content of the file before running the tests." configFilename
        |> failwith

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
