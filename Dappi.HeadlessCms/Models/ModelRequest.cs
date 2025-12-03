using Dappi.Core.Enums;

namespace Dappi.HeadlessCms.Models;

public class ModelRequest
{
    public string ModelName { get; set; }
    public bool IsAuditableEntity { get; set; } = false;
    public List<CrudActions>? CrudActions { get; set; } = null;
}

public class ModelResponse
{
    public List<FieldsInfo> Fields { get; set; }
    public List<CrudActions> AllowedActions { get; set; }
}

public class ConfigureModelActionsRequest
{
    public List<CrudActions> CrudActions { get; set; } = [];
}