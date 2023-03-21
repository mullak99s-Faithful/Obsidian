using System.Security.Claims;

namespace ObsidianAPI.Abstractions
{
	public interface IRoleValidator
	{
		bool CurrentUserHasRole(ClaimsIdentity user, string name);
	}
}
