namespace Mutex.Zendesk.Support.API

open System
open System.Collections.Generic

type OrganizationId = int64
type ExternalId = string
type GroupId = int64

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
type OrganizationModel = {
    organization : Organization
}

type OrganizationsModel() =
    inherit PagedModel()

    member val organizations : Organization array = null with get, set

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

module Organization =

    let getAll send context =
        sprintf "/api/v2/organizations.json"
        |> Http.getArray<OrganizationsModel, Organization> send context (fun model -> model.organizations)

    let get send context (id : OrganizationId) =
        sprintf "/api/v2/organizations/%i.json" id
        |> Http.get<OrganizationModel, Organization> send context (fun model -> model.organization)

    let tryGet send context id =
        get send context id
        |> Result.mapNotFoundToNone

    let getByExternalId send context externalId =
        sprintf "/api/v2/organizations/search.json?external_id=%s" externalId
        |> Http.getArray<OrganizationsModel, Organization> send context (fun model -> model.organizations)
