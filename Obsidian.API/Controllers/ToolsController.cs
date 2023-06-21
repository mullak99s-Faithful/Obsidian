using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Obsidian.API.Logic;
using Obsidian.SDK.Models.Tools;

namespace Obsidian.API.Controllers
{
	[ApiController]
	[ApiVersion("1.0")]
	[Route("api/v{apiVersion:apiVersion}/[controller]")]
	[SwaggerResponse(401, "You are not authorized to access this")]
	[SwaggerResponse(500, "An unexpected error occurred")]
	public class ToolsController : ControllerBase
	{
		private readonly IToolsLogic _toolsLogic;

		public ToolsController(IToolsLogic toolsLogic)
		{
			_toolsLogic = toolsLogic;
		}

		[HttpGet("versions")]
		[SwaggerResponse(200, Type = typeof(IEnumerable<AssetMCVersion>), Description = "A list of all supported Java versions")]
		[SwaggerResponse(400, "No versions could be found")]
		[ResponseCache(Duration = 600, Location = ResponseCacheLocation.Any, NoStore = false)]
		public async Task<IActionResult> GetAllVersions()
		{
			try
			{
				var versions = await _toolsLogic.GetJavaMCVersions();

				if (versions.Any())
					return Ok(versions);
				return BadRequest("No versions could be found!");
			}
			catch (Exception ex)
			{
				return StatusCode(500, ex.Message);
			}
		}

		[HttpGet("bedrock/versions")]
		[SwaggerResponse(200, Type = typeof(IEnumerable<AssetMCVersion>), Description = "A list of all supported Bedrock versions")]
		[SwaggerResponse(400, "No versions could be found")]
		[ResponseCache(Duration = 1800, Location = ResponseCacheLocation.Any, NoStore = false)]
		public async Task<IActionResult> GetAllBedrockVersions()
		{
			try
			{
				var versions = await _toolsLogic.GetBedrockMCVersions();

				if (versions.Any())
					return Ok(versions);
				return BadRequest("No versions could be found!");
			}
			catch (Exception ex)
			{
				return StatusCode(500, ex.Message);
			}
		}

		[HttpGet("version/{mcVersion}")]
		[SwaggerResponse(200, Type = typeof(MCAssets), Description = "Assets for a specified Java version")]
		[SwaggerResponse(400, "No assets could be found")]
		[ResponseCache(Duration = 1800, Location = ResponseCacheLocation.Any, NoStore = false)]
		public async Task<IActionResult> GetJavaVersionAssets([FromRoute] string mcVersion)
		{
			try
			{
				var assets = await _toolsLogic.GetMinecraftJavaAssets(mcVersion);

				if (assets.IsSuccess)
					return Ok(assets.Data);
				return BadRequest(assets.Message);
			}
			catch (Exception ex)
			{
				return StatusCode(500, ex.Message);
			}
		}

		[HttpGet("version/{mcVersion}/jar")]
		[SwaggerResponse(200, Type = typeof(MCAssets), Description = "Jar for a specified Java version")]
		[SwaggerResponse(400, "Invalid version")]
		[ResponseCache(Duration = 1800, Location = ResponseCacheLocation.Any, NoStore = false)]
		public async Task<IActionResult> GetMinecraftJarUrl([FromRoute] string mcVersion)
		{
			try
			{
				var assets = await _toolsLogic.GetMinecraftJavaJar(mcVersion);

				if (assets.IsSuccess)
					return Ok(assets.Data);
				return BadRequest(assets.Message);
			}
			catch (Exception ex)
			{
				return StatusCode(500, ex.Message);
			}
		}

		[HttpGet("bedrock/version/{mcVersion}")]
		[SwaggerResponse(200, Type = typeof(MCAssets), Description = "Assets for a specified Bedrock version")]
		[SwaggerResponse(400, "No assets could be found")]
		[ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any, NoStore = false)]
		public async Task<IActionResult> GetBedrockVersionAssets([FromRoute] string mcVersion)
		{
			try
			{
				var assets = await _toolsLogic.GetMinecraftBedrockAssets(mcVersion);

				if (assets.IsSuccess)
					return Ok(assets.Data);
				return BadRequest(assets.Message);
			}
			catch (Exception ex)
			{
				return StatusCode(500, ex.Message);
			}
		}

		[HttpGet("pregenerate/java")]
		[SwaggerResponse(200, Description = "Assets for all supported Java versions have been pre-generated")]
		[Authorize("write:pregenerate-assets")]
		public async Task<IActionResult> PregenerateJavaAssets()
		{
			try
			{
				return Ok(await _toolsLogic.PregenerateJavaAssets());
			}
			catch (Exception ex)
			{
				return StatusCode(500, ex.Message);
			}
		}

		[HttpGet("pregenerate/bedrock")]
		[SwaggerResponse(200, Description = "Assets for all supported Bedrock versions have been pre-generated")]
		[Authorize("write:pregenerate-assets")]
		public async Task<IActionResult> PregenerateBedrockAssets()
		{
			try
			{
				return Ok(await _toolsLogic.PregenerateBedrockAssets());
			}
			catch (Exception ex)
			{
				return StatusCode(500, ex.Message);
			}
		}

		[HttpGet("purge")]
		[SwaggerResponse(200, Description = "Queued asset purging")]
		[Authorize("write:purge-assets")]
		public IActionResult PurgeAssets()
		{
			try
			{
				_ = Task.Run(() => _toolsLogic.PurgeAssets());
				return Ok();
			}
			catch (Exception ex)
			{
				return StatusCode(500, ex.Message);
			}
		}
	}
}
