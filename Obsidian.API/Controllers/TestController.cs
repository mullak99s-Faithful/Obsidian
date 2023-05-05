using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;
using Obsidian.API.Services;

namespace Obsidian.API.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[SwaggerResponse(401, "You are not authorized to access this")]
	[SwaggerResponse(500, "An unexpected error occurred")]
	public class TestController : ControllerBase
	{
		private ICurrentUserService _currentUserService;

		public TestController(ICurrentUserService currentUserService)
		{
			_currentUserService = currentUserService;
		}

		[HttpGet("admin")]
		[Authorize("admin-action")]
		public IActionResult AdminTest()
		{
			return Ok();
		}

		[HttpGet("user")]
		[Authorize]
		public IActionResult UserTest()
		{
			return Ok();
		}

		[HttpGet("anon")]
		public IActionResult AnonTest()
		{
			return Ok();
		}

		[HttpGet("perms")]
		[Authorize]
		public IActionResult PermissionsTest()
		{
			using HttpClient client = new();
			ClaimsIdentity? claimsIdentity = User.Identity as ClaimsIdentity;
			Claim[]? permissionClaims = claimsIdentity?.FindAll("permissions").ToArray();
			List<string> permissions = new();
			if (permissionClaims != null && permissionClaims.Any())
				permissions = permissionClaims.Select(c => c.Value).ToList();

			return Ok(permissions);
		}
	}
}
