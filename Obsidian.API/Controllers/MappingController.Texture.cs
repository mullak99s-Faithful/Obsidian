using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Obsidian.SDK.Models.Assets;
using Obsidian.SDK.Models.Mappings;
using Swashbuckle.AspNetCore.Annotations;

namespace Obsidian.API.Controllers
{
	public partial class MappingController
	{
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

			string mapName = await _textureMapRepository.GetTextureMappingNameById(id);
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
			List<TextureAsset>? map;
			try
			{
				map = JsonConvert.DeserializeObject<List<TextureAsset>>(json);
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

		[HttpPost("TextureMap/Replace/{id}")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:add-mapping")]
		public async Task<IActionResult> ReplaceTextureMapping([FromRoute] Guid id, IFormFile file)
		{
			if (file.Length == 0)
				return BadRequest("Please select a file");
			if (string.IsNullOrEmpty(id.ToString()))
				return BadRequest("Please provide an id");

			using var streamReader = new StreamReader(file.OpenReadStream());
			string json = await streamReader.ReadToEndAsync();
			List<TextureAsset>? map;
			try
			{
				map = JsonConvert.DeserializeObject<List<TextureAsset>>(json);
			}
			catch (Exception)
			{
				// Something broke with the map
				map = null;
			}

			if (map == null)
				return BadRequest("Invalid map");

			return await _textureMapRepository.ReplaceAssets(id, map) ? Ok() : BadRequest("Invalid");
		}

		[HttpGet("TextureMap/Export/{id}")]
		[ProducesResponseType(typeof(ModelMapping), 200)]
		[SwaggerResponse(404, "Texture mapping does not exist")]
		public async Task<IActionResult> ExportTextureMapping([FromRoute] Guid id)
		{
			TextureMapping? map = await _textureMapRepository.GetTextureMappingById(id);
			if (map == null)
				return NotFound();

			List<TextureAsset> assets = map.Assets;
			if (assets.Count == 0)
				return NotFound("The texture map contains no assets!");

			return Ok(assets);
		}
	}
}
