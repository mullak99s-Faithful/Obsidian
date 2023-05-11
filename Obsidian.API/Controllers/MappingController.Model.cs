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
		[HttpPost("ModelMap/Add/{name}")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:add-mapping")]
		public async Task<IActionResult> AddModelMapping([FromRoute] string name, IFormFile file)
		{
			if (file.Length == 0)
				return BadRequest("Please select a file");
			if (string.IsNullOrEmpty(name))
				return BadRequest("Please provide a name");

			using var streamReader = new StreamReader(file.OpenReadStream());
			string json = await streamReader.ReadToEndAsync();
			List<ModelAsset>? map;
			try
			{
				map = JsonConvert.DeserializeObject<List<ModelAsset>>(json);
			}
			catch (Exception)
			{
				// Something broke with the map
				map = null;
			}

			if (map == null)
				return BadRequest("Invalid map");

			await _modelMapRepository.AddModelMap(new ModelMapping()
			{
				Id = Guid.NewGuid(),
				Name = name,
				Models = map
			});

			return Ok();
		}

		[HttpPost("ModelMap/Add/Blank/{name}")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:add-mapping")]
		public async Task<IActionResult> AddBlankModelMapping([FromRoute] string name)
		{
			if (string.IsNullOrEmpty(name))
				return BadRequest("Please provide a name");

			await _modelMapRepository.AddModelMap(new ModelMapping()
			{
				Id = Guid.NewGuid(),
				Name = name,
				Models = new List<ModelAsset>()
			});

			return Ok();
		}

		[HttpGet("ModelMap/GetAll")]
		[ProducesResponseType(typeof(IEnumerable<ModelMapping>), 200)]
		[SwaggerResponse(404, "No model mappings exist")]
		public async Task<IActionResult> GetModelMappings()
		{
			IEnumerable<ModelMapping> maps = await _modelMapRepository.GetAllModelMappings();
			if (!maps.Any())
				return NotFound();
			return Ok(maps);
		}

		[HttpGet("ModelMap/GetAllIds")]
		[ProducesResponseType(typeof(Dictionary<Guid, string>), 200)]
		[SwaggerResponse(404, "No model mappings exist")]
		public async Task<IActionResult> GetModelMappingIds()
		{
			Dictionary<Guid, string> maps = await _modelMapRepository.GetAllModelMappingIds();
			if (!maps.Any())
				return NotFound();
			return Ok(maps);
		}

		[HttpGet("ModelMap/GetName/{id}")]
		[ProducesResponseType(typeof(string), 200)]
		[SwaggerResponse(404, "No model mappings exist")]
		public async Task<IActionResult> GetModelMappingName([FromRoute] Guid id)
		{
			if (string.IsNullOrWhiteSpace(id.ToString()))
				return BadRequest("No id provided");

			string mapName = await _modelMapRepository.GetModelMappingNameById(id);
			if (string.IsNullOrWhiteSpace(mapName))
				return NotFound();
			return Ok(mapName);
		}

		[HttpGet("ModelMap/Get/{id}")]
		[ProducesResponseType(typeof(ModelMapping), 200)]
		[SwaggerResponse(404, "Model mapping does not exist")]
		public async Task<IActionResult> GetModelMapping([FromRoute] Guid id)
		{
			ModelMapping? map = await _modelMapRepository.GetModelMappingById(id);
			if (map == null)
				return NotFound();
			return Ok(map);
		}
	}
}
