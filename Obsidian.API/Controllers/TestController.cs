using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Obsidian.API.Services;
using Swashbuckle.AspNetCore.Annotations;

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
		[Authorize("is-superuser")]
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
	}
}
