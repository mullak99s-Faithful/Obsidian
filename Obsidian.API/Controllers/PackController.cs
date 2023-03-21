using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Obsidian.SDK.Models;
using ObsidianAPI.Logic;
using Swashbuckle.AspNetCore.Annotations;

namespace ObsidianAPI.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[SwaggerResponse(401, "You are not authorized to access this")]
	[SwaggerResponse(500, "An unexpected error occurred")]
	public class PackController : ControllerBase
	{
		private readonly IPackLogic _packLogic;
		private readonly ILogger<PackController> _logger;

		public PackController(IPackLogic packLogic, ILogger<PackController> logger)
		{
			_packLogic = packLogic;
			_logger = logger;
		}

		[HttpGet("GetAll")]
		[ProducesResponseType(typeof(IEnumerable<Pack>), 200)]
		[SwaggerResponse(404, "No packs exist")]
		public IActionResult GetPacks()
		{
			IEnumerable<Pack> packs = _packLogic.GetPacks();
			if (!packs.Any())
				return NotFound();
			return Ok(packs);
		}

		[HttpGet("GetFromId/{id}")]
		[ProducesResponseType(typeof(Pack), 200)]
		[SwaggerResponse(404, "Pack does not exist")]
		public IActionResult GetPack([FromRoute] Guid id)
		{
			Pack? pack = _packLogic.GetPack(id);
			if (pack == null)
				return NotFound();
			return Ok(pack);
		}

		[HttpGet("GetFromName/{name}")]
		[ProducesResponseType(typeof(Pack), 200)]
		[SwaggerResponse(404, "Pack does not exist")]
		public IActionResult GetPack([FromRoute] string name)
		{
			Pack? pack = _packLogic.GetPack(name);
			if (pack == null)
				return NotFound();
			return Ok(pack);
		}

		[HttpPost("Add")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:add-pack")]
		public async Task<IActionResult> AddPack(string name, string description, Guid textureMappings)
		{
			bool success = await _packLogic.AddPack(name, description, textureMappings);
			if (!success)
				return BadRequest();
			return Ok();
		}

		[HttpPost("Edit/{id}")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:add-pack")]
		public async Task<IActionResult> AddPack([FromRoute] Guid id, string? name, string? description, Guid? textureMappings)
		{
			bool success = await _packLogic.EditPack(id, name, description, textureMappings);
			if (!success)
				return BadRequest();
			return Ok();
		}

		[HttpPost("Edit/PackPng")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:add-pack")]
		public async Task<IActionResult> AddPackPNG(Guid id, IFormFile packPng)
		{
			bool success = await _packLogic.AddPackPng(id, packPng);
			if (!success)
				return BadRequest();
			return Ok();
		}
	}
}