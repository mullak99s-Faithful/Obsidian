using System.Security.Claims;

namespace ObsidianAPI.Abstractions
{
	public interface IPermissionValidator
	{
		bool CurrentUserHasPermission(ClaimsIdentity user, string permission);
	}
}
