namespace Mutex.Zendesk.Support.API

[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module Organization =

    val getAll : GetCommand<OrganizationsModel, Organization array>
        
    val get : OrganizationId -> GetCommand<OrganizationModel, Organization>

    val getByExternalId : ExternalId -> GetCommand<OrganizationsModel, Organization array>

    val post : NewOrganization -> PostCommand<OrganizationModel, Organization>

    val put : Organization -> PutCommand<OrganizationModel, Organization>

    val delete : OrganizationId -> DeleteCommand
