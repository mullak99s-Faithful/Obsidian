﻿using Microsoft.AspNetCore.Mvc;
using Obsidian.API.Logic;
using Obsidian.SDK.Enums;
using Obsidian.SDK.Models.Assets;
using Swashbuckle.AspNetCore.Annotations;

namespace Obsidian.API.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[SwaggerResponse(401, "You are not authorized to access this")]
	[SwaggerResponse(500, "An unexpected error occurred")]
	public class AutoController : ControllerBase
	{
		private readonly IAutoGenerationLogic _autoGenerationLogic;

		public AutoController(IAutoGenerationLogic autoGenerationLogic)
		{
			_autoGenerationLogic = autoGenerationLogic;
		}

		[HttpGet("generatemissingtextures")] // TODO: Need auth
		[SwaggerResponse(200, Type = typeof(IEnumerable<TextureAsset>), Description = "Generate texture map of missing textures for a specific version")]
		public async Task<IActionResult> GenerateMissingTextures(Guid packId, MinecraftVersion version)
		{
			try
			{
				List<TextureAsset> assets = await _autoGenerationLogic.GenerateMissingMappings(packId, version);

				if (assets.Any())
					return Ok(assets);
				return BadRequest("No assets generated!"); // TODO: Include error message from logic method call
			}
			catch (Exception ex)
			{
				return StatusCode(500, ex.Message);
			}
		}
	}
}