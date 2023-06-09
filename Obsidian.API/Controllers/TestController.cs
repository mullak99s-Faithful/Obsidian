﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Obsidian.API.Services;
using Swashbuckle.AspNetCore.Annotations;
using System.Security.Claims;

namespace Obsidian.API.Controllers
{
	[ApiController]
	[ApiVersion("1.0")]
	[Route("api/v{apiVersion:apiVersion}/[controller]")]
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
		public IActionResult AdminAuthTest()
		{
			return Ok();
		}

		[HttpGet("user")]
		[Authorize]
		public IActionResult UserAuthTest()
		{
			return Ok();
		}

		[HttpGet("anon")]
		public IActionResult AnonAuthTest()
		{
			return Ok();
		}

		[HttpGet("perms")]
		[Authorize]
		public IActionResult PermissionsTest()
		{
			ClaimsIdentity? claimsIdentity = User.Identity as ClaimsIdentity;
			Claim[]? permissionClaims = claimsIdentity?.FindAll("permissions").ToArray();
			List<string> permissions = new();
			if (permissionClaims != null && permissionClaims.Any())
				permissions = permissionClaims.Select(c => c.Value).ToList();

			return Ok(permissions);
		}
	}
}
