﻿using Microsoft.AspNetCore.Authorization;
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
	public class MappingController : ControllerBase
	{
		private readonly IMappingLogic _logic;
		private readonly ILogger<MappingController> _logger;

		public MappingController(IMappingLogic mappingLogic, ILogger<MappingController> logger)
		{
			_logic = mappingLogic;
			_logger = logger;
		}

		[HttpGet("TextureMap/GetAll")]
		[ProducesResponseType(typeof(IEnumerable<TextureMapping>), 200)]
		[SwaggerResponse(404, "No texture mappings exist")]
		public IActionResult GetTextureMappings()
		{
			IEnumerable<TextureMapping> maps = _logic.GetTextureMappings();
			if (!maps.Any())
				return NotFound();
			return Ok(maps);
		}

		[HttpGet("TextureMap/Get/{id}")]
		[ProducesResponseType(typeof(TextureMapping), 200)]
		[SwaggerResponse(404, "Texture mapping does not exist")]
		public IActionResult GetTextureMapping([FromRoute] Guid id)
		{
			TextureMapping? map = _logic.GetTextureMapping(id);
			if (map == null)
				return NotFound();
			return Ok(map);
		}

		[HttpPost("TextureMap/Add/{name}")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:add-mapping")]
		public async Task<IActionResult> AddTextureMapping([FromRoute] string name, IFormFile file)
		{
			if (file.Length == 0)
				return BadRequest("Please select a file");
			if (string.IsNullOrEmpty(name))
				return BadRequest("Please provide a name");

			return await _logic.AddTextureMapping(name, file) ? Ok() : BadRequest("Invalid map");
		}
	}
}
