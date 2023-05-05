using System.Text.Json;
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
	public class MappingController : ControllerBase
	{
		private readonly ITextureMapRepository _textureMapRepository;
		private readonly ILogger<MappingController> _logger;

		public MappingController(ITextureMapRepository textureMapRepository, ILogger<MappingController> logger)
		{
			_textureMapRepository = textureMapRepository;
			_logger = logger;
		}

		[HttpGet("TextureMap/GetAll")]
		[ProducesResponseType(typeof(IEnumerable<TextureMapping>), 200)]
		[SwaggerResponse(404, "No texture mappings exist")]
		public async Task<IActionResult> GetTextureMappings()
		{
			IEnumerable<TextureMapping> maps = await _textureMapRepository.GetAllTextureMappings();
			if (!maps.Any())
				return NotFound();
			return Ok(maps);
		}

		[HttpGet("TextureMap/GetAllIds")]
		[ProducesResponseType(typeof(Dictionary<Guid, string>), 200)]
		[SwaggerResponse(404, "No texture mappings exist")]
		public async Task<IActionResult> GetTextureMappingIds()
		{
			Dictionary<Guid, string> maps = await _textureMapRepository.GetAllTextureMappingIds();
			if (!maps.Any())
				return NotFound();
			return Ok(maps);
		}

		[HttpGet("TextureMap/GetName/{id}")]
		[ProducesResponseType(typeof(string), 200)]
		[SwaggerResponse(404, "No texture mappings exist")]
		public async Task<IActionResult> GetTextureMappingName([FromRoute] Guid id)
		{
			if (string.IsNullOrWhiteSpace(id.ToString()))
				return BadRequest("No id provided");

			string? mapName = await _textureMapRepository.GetTextureMappingNameById(id);
			if (string.IsNullOrWhiteSpace(mapName))
				return NotFound();
			return Ok(mapName);
		}

		[HttpGet("TextureMap/Get/{id}")]
		[ProducesResponseType(typeof(TextureMapping), 200)]
		[SwaggerResponse(404, "Texture mapping does not exist")]
		public async Task<IActionResult> GetTextureMapping([FromRoute] Guid id)
		{
			TextureMapping? map = await _textureMapRepository.GetTextureMappingById(id);
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

			using var streamReader = new StreamReader(file.OpenReadStream());
			string json = await streamReader.ReadToEndAsync();
			List<Asset>? map;
			try
			{
				map = JsonSerializer.Deserialize<List<Asset>>(json);
			}
			catch (Exception)
			{
				// Something broke with the map
				map = null;
			}

			if (map == null)
				return BadRequest("Invalid map");

			await _textureMapRepository.AddTextureMap(new TextureMapping()
			{
				Id = Guid.NewGuid(),
				Name = name,
				Assets = map
			});

			return Ok();
		}

		[HttpPost("TextureMap/Rename/{mapGuid}")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:rename-mapping")]
		public async Task<IActionResult> RenameTextureMapping([FromRoute] Guid mapGuid, string name)
		{
			if (string.IsNullOrEmpty(mapGuid.ToString()))
				return BadRequest("Please provide an id");
			if (string.IsNullOrEmpty(name))
				return BadRequest("Please provide a name");

			return await _textureMapRepository.UpdateNameById(mapGuid, name) ? Ok() : BadRequest();
		}

		[HttpPost("TextureMap/Delete/{mapGuid}")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:delete-mapping")]
		public async Task<IActionResult> DeleteTextureMapping([FromRoute] Guid mapGuid)
		{
			if (string.IsNullOrEmpty(mapGuid.ToString()))
				return BadRequest("Please provide an id");

			return await _textureMapRepository.DeleteById(mapGuid) ? Ok() : BadRequest();
		}
	}
}
