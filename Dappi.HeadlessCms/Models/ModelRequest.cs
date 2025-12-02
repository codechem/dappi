using Dappi.Core.Enums;

namespace Dappi.HeadlessCms.Models;

public class ModelRequest
{
    public string ModelName { get; set; }
    public bool IsAuditableEntity { get; set; } = false;
    public List<CrudActions>? CrudActions { get; set; } = null;
}