using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Obsidian.API.Logic;
using Swashbuckle.AspNetCore.Annotations;

namespace Obsidian.API.Controllers
{
	[ApiController]
	[ApiVersion("1.0")]
	[Route("api/v{apiVersion:apiVersion}/[controller]")]
	[SwaggerResponse(401, "You are not authorized to access this")]
	[SwaggerResponse(500, "An unexpected error occurred")]
	public class CreditsController : ControllerBase
	{
		private readonly ITextureLogic _logic;
		private readonly ILogger<MappingController> _logger;

		public CreditsController(ITextureLogic textureLogic, ILogger<MappingController> logger)
		{
			_logic = textureLogic;
			_logger = logger;
		}

		[HttpGet("Get/{packId}/{assetId}")]
		[ProducesResponseType(typeof(FileContentResult), 200)]
		public async Task<IActionResult> GetTextureCredit([FromRoute] Guid packId, [FromRoute] Guid assetId)
		{
			return Ok(await _logic.GetTextureCredit(packId, assetId));
		}

		[HttpGet("Search/{packId}")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		public async Task<IActionResult> CreditSearch([FromRoute] Guid packId, [FromQuery] string query)
		{
			return Ok(await _logic.SearchForTextureCredit(packId, query));
		}

		[HttpGet("Add/{packId}/{assetId}")]
		[Authorize("write:upload-texture")]
		[ProducesResponseType(typeof(FileContentResult), 200)]
		public async Task<IActionResult> AddTextureCredit([FromRoute] Guid packId, [FromRoute] Guid assetId, string credits)
		{
			return await _logic.AddCredit(packId, assetId, credits) ? Ok() : BadRequest("Invalid");
		}

		[HttpPost("Clear/{textureMappingId}")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:delete-texture")]
		public async Task<IActionResult> ClearAllCredits([FromRoute] Guid textureMappingId)
		{
			if (string.IsNullOrWhiteSpace(textureMappingId.ToString()))
				return BadRequest("Please provide an id");

			bool success = await _logic.DeleteAllTextures(textureMappingId);
			return success ? Ok() : BadRequest("Invalid");
		}
	}
}
