using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Obsidian.API.Logic;
using Obsidian.API.Repository;
using Obsidian.SDK.Enums;
using Obsidian.SDK.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace Obsidian.API.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[SwaggerResponse(401, "You are not authorized to access this")]
	[SwaggerResponse(500, "An unexpected error occurred")]
	public class PackController : ControllerBase
	{
		private readonly IPackRepository _packRepository;
		private readonly ITextureMapRepository _textureMapRepository;
		private readonly IModelMapRepository _modelMapRepository;
		private readonly IBlockStateMapRepository _blockStateMapRepository;
		private readonly IPackPngLogic _packPngLogic;
		private readonly IMiscAssetLogic _miscAssetLogic;
		private readonly IPackLogic _packLogic;
		private readonly ILogger<PackController> _logger;

		public PackController(IPackRepository packRepository, ITextureMapRepository textureMapRepository, IModelMapRepository modelMapRepository, IBlockStateMapRepository blockStateMapRepository, IPackPngLogic packPngLogic, IMiscAssetLogic miscAssetLogic, IPackLogic packLogic, ILogger<PackController> logger)
		{
			_packRepository = packRepository;
			_textureMapRepository = textureMapRepository;
			_modelMapRepository = modelMapRepository;
			_blockStateMapRepository = blockStateMapRepository;
			_packPngLogic = packPngLogic;
			_miscAssetLogic = miscAssetLogic;
			_packLogic = packLogic;
			_logger = logger;
		}

		[HttpGet("GetAll")]
		[ProducesResponseType(typeof(IEnumerable<Pack>), 200)]
		[SwaggerResponse(404, "No packs exist")]
		public async Task<IActionResult> GetPacks()
		{
			IEnumerable<Pack> packs = await _packRepository.GetAllPacks();
			if (!packs.Any())
				return NotFound();
			return Ok(packs);
		}

		[HttpGet("GetFromId/{id}")]
		[ProducesResponseType(typeof(Pack), 200)]
		[SwaggerResponse(404, "Pack does not exist")]
		public async Task<IActionResult> GetPack([FromRoute] Guid id)
		{
			Pack? pack = await _packRepository.GetPackById(id);
			if (pack == null)
				return NotFound();
			return Ok(pack);
		}

		[HttpGet("GetFromName/{name}")]
		[ProducesResponseType(typeof(Pack), 200)]
		[SwaggerResponse(404, "Pack does not exist")]
		public async Task<IActionResult> GetPack([FromRoute] string name)
		{
			Pack? pack = await _packRepository.GetPackByName(name);
			if (pack == null)
				return NotFound();
			return Ok(pack);
		}

		[HttpPost("Add")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:add-pack")]
		public async Task<IActionResult> AddPack(string name, string description, Guid textureMappingId, Guid? modelMappingId, Guid? blockStateMappingId)
		{
			if (await _textureMapRepository.GetTextureMappingById(textureMappingId) == null)
				return NotFound("Invalid texture map!");

			if (modelMappingId != null && await _modelMapRepository.GetModelMappingById(modelMappingId.Value) == null)
				return NotFound("Invalid model map!");

			if (blockStateMappingId != null && await _blockStateMapRepository.GetBlockStateMappingById(blockStateMappingId.Value) == null)
				return NotFound("Invalid model map!");

			bool success = await _packLogic.AddPack(new Pack(name, description, textureMappingId, modelMappingId, blockStateMappingId));
			if (!success)
				return BadRequest();
			return Ok();
		}

		[HttpPost("Edit/{id}")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:edit-pack")]
		public async Task<IActionResult> EditPack([FromRoute] Guid id, string? name, string? description, Guid? textureMappingId, Guid? modelMappingId, Guid? blockStateMappingId, bool? emissives, string? emissiveSuffix)
		{
			if (textureMappingId != null)
			{
				if (await _textureMapRepository.GetTextureMappingById(textureMappingId.Value) == null)
					return NotFound("Invalid texture map!");
			}

			if (modelMappingId != null)
			{
				if (await _modelMapRepository.GetModelMappingById(modelMappingId.Value) == null)
					return NotFound("Invalid model map!");
			}

			if (blockStateMappingId != null)
			{
				if (await _blockStateMapRepository.GetBlockStateMappingById(blockStateMappingId.Value) == null)
					return NotFound("Invalid block state map!");
			}

			bool success = await _packRepository.UpdatePackById(id, name, textureMappingId, modelMappingId, blockStateMappingId, description, null, emissives, emissiveSuffix);
			if (!success)
				return BadRequest();
			return Ok();
		}

		[HttpPost("Delete/{id}")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:delete-pack")]
		public async Task<IActionResult> DeletePack([FromRoute] Guid id)
		{
			bool success = false;
			Pack? pack = await _packRepository.GetPackById(id);
			if (pack != null)
				success = await _packLogic.DeletePack(pack);

			if (!success)
				return BadRequest();
			return Ok();
		}

		[HttpPost("PackPng/Add")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:edit-pack")]
		public async Task<IActionResult> AddPackPNG(Guid packId, IFormFile packPng, bool overwrite = true)
		{
			bool success = await _packPngLogic.UploadPackPng(packId, packPng, overwrite);
			if (!success)
				return BadRequest();
			return Ok();
		}

		[HttpPost("PackPng/Delete")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:edit-pack")]
		public async Task<IActionResult> DeletePackPNG(Guid packId)
		{
			bool success = await _packPngLogic.DeletePackPng(packId);
			if (!success)
				return BadRequest();
			return Ok();
		}

		[HttpPost("Misc/Add")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:edit-pack")]
		public async Task<IActionResult> AddMiscAsset(Guid packId, MinecraftVersion minVersion, MinecraftVersion maxVersion, IFormFile miscAsset, bool overwrite = true)
		{
			Pack? pack = await _packRepository.GetPackById(packId);
			if (pack == null)
				return BadRequest("Invalid pack id!");

			byte[] bytes = await Utils.GetBytesFromFormFileAsync(miscAsset);

			if (bytes.Length == 0 || !Utils.IsZipFile(bytes))
				return BadRequest("Invalid misc asset!");

			await _miscAssetLogic.AddMiscAsset(pack, minVersion, maxVersion, bytes, overwrite);
			return Ok();
		}

		[HttpPost("Misc/Delete")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:edit-pack")]
		public async Task<IActionResult> DeleteMiscAsset(Guid packId, Guid assetId)
		{
			Pack? pack = await _packRepository.GetPackById(packId);
			if (pack == null)
				return BadRequest("Invalid pack id!");

			await _miscAssetLogic.DeleteMiscAsset(pack, assetId);
			return Ok();
		}

		[HttpGet("Triggers/PackCheck")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:generate-packs")]
		public IActionResult DoPackCheck(Guid packId, bool fullCheck = false)
		{
			Task.Run(() => _ = _packLogic.TriggerPackCheck(packId, fullCheck)); // Run in the background
			return Ok();
		}
	}
}