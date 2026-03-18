using Microsoft.AspNetCore.Identity;

namespace Dappi.HeadlessCms.Models;

public class DappiUser : IdentityUser
{
	public bool AcceptedInvitation { get; set; } = true;
}

public class DappiRole : IdentityRole
{

}