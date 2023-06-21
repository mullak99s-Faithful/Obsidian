using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Obsidian.API.Logic;
using Obsidian.SDK.Enums;
using Swashbuckle.AspNetCore.Annotations;

namespace Obsidian.API.Controllers
{
	[ApiController]
	[ApiVersion("1.0")]
	[Route("api/v{apiVersion:apiVersion}/[controller]")]
	[SwaggerResponse(401, "You are not authorized to access this")]
	[SwaggerResponse(500, "An unexpected error occurred")]
	public class ImportController : Controller
	{
		private readonly IPackLogic _packLogic;
		private readonly ILogger<ImportController> _logger;

		public ImportController(IPackLogic packLogic, ILogger<ImportController> logger)
		{
			_packLogic = packLogic;
			_logger = logger;
		}

		[HttpPost("Pack/{version}")]
		[Consumes("multipart/form-data")]
		[RequestFormLimits(MultipartBodyLengthLimit = 209715200)]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:import-pack")]
		public async Task<IActionResult> ImportPack([FromRoute] MinecraftVersion version, [FromQuery] string packIds, [FromQuery] bool? overwrite, IFormFile pack)
		{
			var packIdList = packIds.Split(',').Select(Guid.Parse).ToList();
			overwrite ??= false;

			if (pack.Length == 0)
				return BadRequest("Please select a file");
			if (packIdList.Count == 0)
				return BadRequest("Please provide pack ids");

			await _packLogic.ImportPack(version, packIdList, pack, overwrite.Value);
			return Ok();
		}
	}
}
