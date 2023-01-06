namespace Xero_API_PKCE.Dtos;

public class Tenant
{
    public string? Id { get; set; }
    public string? AuthEventId { get; set; }
    public string? TenantId { get; set; }
    public string? TenantType { get; set; }
    public string? TenantName { get; set; }
    public DateTime? CreatedDateUtc { get; set; }
    public DateTime? UpdatedDateUtc { get; set; }
}

public class Organisation
{
    public string? Id { get; set; }
    public string? TenantId { get; set; }
    public string? Name { get; set; }
    public DateTime? CreatedOn { get; set; }
}