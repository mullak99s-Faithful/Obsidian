using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Obsidian.API.Repository;
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
		private readonly ILogger<PackController> _logger;

		public PackController(IPackRepository packRepository, ITextureMapRepository textureMapRepository, ILogger<PackController> logger)
		{
			_packRepository = packRepository;
			_textureMapRepository = textureMapRepository;
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
		public async Task<IActionResult> AddPack(string name, string description, Guid textureMappings)
		{
			if (await _textureMapRepository.GetTextureMappingById(textureMappings) == null)
				return NotFound("Invalid texture map!");

			bool success = await _packRepository.AddPack(new Pack(name, description, textureMappings));
			if (!success)
				return BadRequest();
			return Ok();
		}

		[HttpPost("Edit/{id}")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:edit-pack")]
		public async Task<IActionResult> EditPack([FromRoute] Guid id, string? name, string? description, Guid? textureMappings)
		{
			if (textureMappings != null)
			{
				if (await _textureMapRepository.GetTextureMappingById(textureMappings.Value) == null)
					return NotFound("Invalid texture map!");
			}

			bool success = await _packRepository.UpdatePackById(id, name, textureMappings, description);
			if (!success)
				return BadRequest();
			return Ok();
		}

		[HttpPost("Delete/{id}")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:delete-pack")]
		public async Task<IActionResult> DeletePack([FromRoute] Guid id)
		{
			bool success = await _packRepository.DeleteById(id);
			if (!success)
				return BadRequest();
			return Ok();
		}

		[HttpPost("Edit/PackPng")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:edit-pack")]
		public async Task<IActionResult> AddPackPNG(Guid id, IFormFile packPng)
		{
			bool success = false; //await _packLogic.AddPackPng(id, packPng);
			if (!success)
				return BadRequest();
			return Ok();
		}
	}
}