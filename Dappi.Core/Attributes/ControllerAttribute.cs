using Dappi.Core.Enums;


namespace Dappi.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class CcControllerAttribute : Attribute
    {
        public CrudActions[] AllowedCrudActions { get; } = [];
        public static string ShortName = nameof(CcControllerAttribute).Replace("Attribute", "");
        public static readonly List<CrudActions> DefaultActions = [CrudActions.Get,
            CrudActions.GetOne,
            CrudActions.Create,
            CrudActions.Update,
            CrudActions.Delete];
        
        public string ExcludeOperation { get; set; } = string.Empty;
        public bool CreateDto { get; set; } = false;
        
        public CcControllerAttribute()
        {
            
        }
        public CcControllerAttribute(params CrudActions[] allowedCrudActions)
        {
            AllowedCrudActions = allowedCrudActions;
        }
    }
}


// idea for maybe [MeasureTimeDebugOnly] to give execution time for sth