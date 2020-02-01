namespace Mutex.Zendesk.Support.API

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Organization =

    let mapOne (model : OrganizationModel) = model.organization
    let mapArray (model : OrganizationsModel) = model.organizations

    let infraModelForNew (org : NewOrganization) : NewOrganizationModel = { organization = org }
    let infraModel org = { organization = org }

    let organizationsUri = "/api/v2/organizations.json"
    let organizationUri (id : OrganizationId) = sprintf "/api/v2/organizations/%i.json" id
    let organizationByExternalIdUri externalId = sprintf "/api/v2/organizations/search.json?external_id=%s" externalId

    let getAll =
        Command.get organizationsUri mapArray
        
    let get id =
        let uri = organizationUri id
        Command.get uri mapOne

    let getByExternalId externalId : GetCommand<OrganizationsModel, Organization array> =
        let uri = organizationByExternalIdUri externalId
        Command.get uri mapArray

    let post newOrganization =
        Command.post organizationsUri newOrganization infraModelForNew mapOne

    let put organization =
        Command.put organizationsUri organization infraModel mapOne

    let delete id =
        let uri = organizationUri id
        Command.delete uri
