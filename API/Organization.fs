namespace Mutex.Zendesk.Support.API

open System
open System.Collections.Generic

type OrganizationId = int64
type ExternalId = string
type GroupId = int64

[<NoComparison>]
type NewOrganization = {
    name : string
    shared_tickets : bool
    shared_comments : bool
    external_id : ExternalId // May be null.
    domain_names : string array // May be null.
    details : string // May be null.
    notes : string // May be null.
    group_id : Nullable<GroupId>
    tags : string array // May be null.
    organization_fields : IDictionary<string, obj> // May be null.
}

[<NoComparison>]
type Organization = {
    url : string
    id : OrganizationId
    name : string
    shared_tickets : bool
    shared_comments : bool
    external_id : ExternalId // May be null.
    created_at : DateTime
    updated_at : DateTime
    domain_names : string array // May be null.
    details : string // May be null.
    notes : string // May be null.
    group_id : Nullable<GroupId>
    tags : string array // May be null.
    organization_fields : IDictionary<string, obj> // May be null.
}

[<NoComparison>]
type private NewOrganizationModel = {
    organization : NewOrganization
}

[<NoComparison>]
type OrganizationModel = {
    organization : Organization
}

type OrganizationsModel() =
    inherit PagedModel()

    member val organizations : Organization array = null with get, set

module Organization =

    let private getOne = Http.get<OrganizationModel, Organization>
    let private getArray = Http.getArray<OrganizationsModel, Organization>
    let private mapOne model = model.organization
    let private mapArray (model : OrganizationsModel) = model.organizations

    let getAll send context =
        "/api/v2/organizations.json"
        |> getArray send context mapArray

    let get send context (id : OrganizationId) =
        sprintf "/api/v2/organizations/%i.json" id
        |> getOne send context mapOne

    let tryGet send context id =
        get send context id
        |> Result.mapNotFoundToNone

    let getByExternalId send context externalId =
        sprintf "/api/v2/organizations/search.json?external_id=%s" externalId
        |> getArray send context mapArray

    let tryGetByExternalId send context externalId =
        let result = getByExternalId send context externalId
        match result with
        | Ok [||] ->
            Ok None
        | Ok [|org|] ->
            Ok (Some org)
        | Ok orgs ->
            orgs
            |> Array.map (fun org -> org.name)
            |> String.concat ", "
            |> sprintf "More than one organization found with external id '%s': %s." externalId
            |> CustomError
            |> Error
        | Error err ->
            Error err

    let post send context (org : NewOrganization) =
        let model : NewOrganizationModel = {
            organization = org
        }
        "/api/v2/organizations.json"
        |> Http.post<OrganizationModel, Organization> send context model mapOne

    let put send context org =
        let model : OrganizationModel = {
            organization = org
        }
        sprintf "/api/v2/organizations/%i.json" org.id
        |> Http.put<OrganizationModel, Organization> send context model mapOne
