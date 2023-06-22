using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Obsidian.API.Logic;
using Obsidian.SDK.Enums;
using Obsidian.SDK.Models.Assets;
using Swashbuckle.AspNetCore.Annotations;

namespace Obsidian.API.Controllers
{
	[ApiController]
	[ApiVersion("1.0")]
	[Route("api/v{apiVersion:apiVersion}/[controller]")]
	[SwaggerResponse(401, "You are not authorized to access this")]
	[SwaggerResponse(500, "An unexpected error occurred")]
	public class AutoController : ControllerBase
	{
		private readonly IAutoGenerationLogic _autoGenerationLogic;

		public AutoController(IAutoGenerationLogic autoGenerationLogic)
		{
			_autoGenerationLogic = autoGenerationLogic;
		}

		[HttpGet("generatemissingtextures")]
		[Authorize("write:add-mapping")] // TODO: Temp permission
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

		[HttpPost("generatemissingoptifinetextures")]
		[Authorize("write:add-mapping")] // TODO: Temp permission
		[SwaggerResponse(200, Type = typeof(IEnumerable<TextureAsset>), Description = "Generate texture map of missing Optifine CTM textures for a specific version")]
		public async Task<IActionResult> GenerateMissingOptifineTextures(Guid packId, MinecraftVersion version, IFormFile packZipFile)
		{
			try
			{
				if (!await Utils.IsZipFile(packZipFile))
					return BadRequest("Invalid file!");

				List<TextureAsset> assets = await _autoGenerationLogic.GenerateOptifineMappings(packId, version, packZipFile);

				if (assets.Any())
					return Ok(assets);
				return BadRequest("No assets generated!"); // TODO: Include error message from logic method call
			}
			catch (Exception ex)
			{
				return StatusCode(500, ex.Message);
			}
		}

		[HttpPost("parsecredits")]
		public async Task<IActionResult> ParseCredits(IFormFile file)
		{
			try
			{
				if (file.Length == 0)
				{
					return BadRequest("No file was uploaded.");
				}

				using var reader = new StreamReader(file.OpenReadStream());
				var lines = new List<string>();

				while (!reader.EndOfStream)
				{
					var line = await reader.ReadLineAsync();
					if (line != null)
						lines.Add(line);
				}

				var credits = _autoGenerationLogic.ParseCreditsTxt(lines.ToArray());
				return Ok(credits);
			}
			catch (Exception ex)
			{
				return StatusCode(500, ex.Message);
			}
		}
	}
}
