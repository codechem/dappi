namespace Dappi.HeadlessCms.UsersAndPermissions.Core
{
    public class AppPermission
    {
        private AppPermission() { } // For EF Core

        public AppPermission(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public int Id { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }

        public List<AppRole> Roles { get; private set; } = [];

        public override string ToString()
        {
            return Name;
        }
    }
}
