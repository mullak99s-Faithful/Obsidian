using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Obsidian.API.Logic;
using Obsidian.SDK.Enums;
using Swashbuckle.AspNetCore.Annotations;

namespace Obsidian.API.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[SwaggerResponse(401, "You are not authorized to access this")]
	[SwaggerResponse(500, "An unexpected error occurred")]
	public class TextureController : ControllerBase
	{
		private readonly ITextureLogic _logic;
		private readonly ILogger<MappingController> _logger;

		public TextureController(ITextureLogic textureLogic, ILogger<MappingController> logger)
		{
			_logic = textureLogic;
			_logger = logger;
		}

		[HttpPost("Texture/Upload/ByName/{name}")]
		[Consumes("multipart/form-data")]
		[RequestFormLimits(MultipartBodyLengthLimit = 209715200)]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:upload-texture")]
		public async Task<IActionResult> UploadTexture([FromRoute] string name, [FromQuery] string packIds, IFormFile textureFile, IFormFile? mcMetaFile)
		{
			var packIdList = packIds.Split(',').Select(Guid.Parse).ToList();

			if (textureFile.Length == 0)
				return BadRequest("Please select a file");
			if (string.IsNullOrEmpty(name))
				return BadRequest("Please provide a name");
			if (packIdList.Count == 0)
				return BadRequest("Please provide pack ids");

			return await _logic.AddTexture(name, packIdList, textureFile, mcMetaFile) ? Ok() : BadRequest("Invalid");
		}

		[HttpPost("Texture/Upload/ById/{assetId}")]
		[Consumes("multipart/form-data")]
		[RequestFormLimits(MultipartBodyLengthLimit = 209715200)]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:upload-texture")]
		public async Task<IActionResult> UploadTexture([FromRoute] Guid assetId, [FromQuery] string packIds, IFormFile textureFile, IFormFile? mcMetaFile)
		{
			var packIdList = packIds.Split(',').Select(Guid.Parse).ToList();

			if (textureFile.Length == 0)
				return BadRequest("Please select a file");
			if (string.IsNullOrEmpty(assetId.ToString()))
				return BadRequest("Please provide an id");
			if (packIdList.Count == 0)
				return BadRequest("Please provide pack ids");

			return await _logic.AddTexture(assetId, packIdList, textureFile, mcMetaFile) ? Ok() : BadRequest("Invalid");
		}

		[HttpPost("Texture/ImportPack/{version}")]
		[Consumes("multipart/form-data")]
		[RequestFormLimits(MultipartBodyLengthLimit = 209715200)]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:import-pack")]
		public IActionResult ImportPack([FromRoute] MinecraftVersion version, [FromQuery] string packIds, [FromQuery] bool? overwrite, IFormFile pack)
		{
			var packIdList = packIds.Split(',').Select(Guid.Parse).ToList();
			overwrite ??= false;

			if (pack.Length == 0)
				return BadRequest("Please select a file");
			if (packIdList.Count == 0)
				return BadRequest("Please provide pack ids");

			return _logic.ImportPack(version, packIdList, pack, overwrite.Value) ? Ok() : BadRequest("Invalid");

		}

		[HttpGet("Texture/GeneratePacks")]
		[Authorize("write:generate-packs")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		public IActionResult GenerateAllPacks([FromQuery] string packIds)
		{
			var packIdList = packIds.Split(',').Select(Guid.Parse).ToList();
			if (packIdList.Count == 0)
				return BadRequest("Please provide pack ids");

			return _logic.GeneratePacks(packIdList) ? Ok() : BadRequest("Invalid");
		}

		[HttpGet("Texture/Search/{packId}")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		public async Task<IActionResult> TextureSearch([FromRoute] Guid packId, [FromQuery] string query)
		{
			return Ok(await _logic.SearchForTextures(packId, query));
		}

		[HttpGet("Texture/GetTexture/{packId}/{assetId}")]
		[ProducesResponseType(typeof(FileContentResult), 200)]
		public async Task<IActionResult> GetTexture([FromRoute] Guid packId, [FromRoute] Guid assetId)
		{
			(string FileName, byte[] Data) texture = await _logic.GetTexture(packId, assetId);
			if (texture.Data.Length == 0)
				return NotFound("Texture not found");

			FileContentResult fileContentResult = new(texture.Data, "image/png")
			{
				FileDownloadName = texture.FileName
			};
			return fileContentResult;
		}
	}
}
