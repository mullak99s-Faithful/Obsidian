using System.Security.Claims;

namespace Obsidian.API.Abstractions
{
	public interface IRoleValidator
	{
		bool CurrentUserHasRole(ClaimsIdentity user, string name);
	}
}
