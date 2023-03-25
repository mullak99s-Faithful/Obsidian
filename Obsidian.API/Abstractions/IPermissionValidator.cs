using System.Security.Claims;

namespace Obsidian.API.Abstractions
{
	public interface IPermissionValidator
	{
		bool CurrentUserHasPermission(ClaimsIdentity user, string permission);
	}
}
