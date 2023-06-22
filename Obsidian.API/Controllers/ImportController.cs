using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Obsidian.API.Extensions;
using Obsidian.API.Logic;
using Obsidian.API.Repository;
using Obsidian.SDK.Enums;
using Obsidian.SDK.Models;
using Obsidian.SDK.Models.Assets;
using Obsidian.SDK.Models.Import;
using Obsidian.SDK.Models.Mappings;
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
		private readonly ITextureLogic _textureLogic;
		private readonly IPackRepository _packRepository;
		private readonly ITextureMapRepository _textureMapRepository;
		private readonly ILogger<ImportController> _logger;

		public ImportController(IPackLogic packLogic, ITextureLogic textureLogic, IPackRepository packRepository, ITextureMapRepository textureMapRepository, ILogger<ImportController> logger)
		{
			_packLogic = packLogic;
			_textureLogic = textureLogic;
			_packRepository = packRepository;
			_textureMapRepository = textureMapRepository;
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

		[HttpPost("Credits/{packId}")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:import-pack")]
		public async Task<IActionResult> ImportCredits([FromRoute] Guid packId, List<Credits> credits)
		{
			if (credits.Count == 0)
				return BadRequest("Please provide credits");

			Pack? pack = await _packRepository.GetPackById(packId);
			if (pack == null)
				return NotFound("Pack does not exist");

			TextureMapping? mapping = await _textureMapRepository.GetTextureMappingById(pack.TextureMappingsId);
			if (mapping == null)
				return NotFound("Texture mapping does not exist");

			List<Task> tasks = new();
			foreach (Credits credit in credits)
			{
				TextureAsset? asset = mapping.Assets.Find(x => x.TexturePaths.Any(y => y.Path == credit.TexturePath));
				if (asset == null)
				{
					Console.WriteLine($"Null asset for credit: {credit.TexturePath}");
					continue;
				}

				tasks.Add(_textureLogic.AddCredit(packId, asset.Id, credit.Credit));
			}
			await tasks.WhenAllThrottledAsync(10);
			return Ok();
		}
	}
}
