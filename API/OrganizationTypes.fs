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
type NewOrganizationModel = {
    organization : NewOrganization
}

[<NoComparison>]
type OrganizationModel = {
    organization : Organization
}

type OrganizationsModel() =
    inherit PagedModel()

    member val organizations : Organization array = [||] with get, set
