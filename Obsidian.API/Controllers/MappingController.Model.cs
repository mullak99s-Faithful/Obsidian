using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Obsidian.SDK.Enums;
using Obsidian.SDK.Models;
using Obsidian.SDK.Models.Assets;
using Obsidian.SDK.Models.Mappings;
using Obsidian.SDK.Models.Minecraft;

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

		[HttpPost("ModelMap/Asset/Import")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		//[Authorize("write:edit-mapping")]
		public async Task<IActionResult> ImportModel(Guid textureMappingId, Guid modelMappingId, string path, string? blockState, IFormFile? file, MinecraftVersion minVersion, MinecraftVersion maxVersion = MinecraftVersion.ALL_FUTURE)
		{
			if (file == null || file.Length == 0)
				return BadRequest("No file specified");

			TextureMapping? textureMapping = await _textureMapRepository.GetTextureMappingById(textureMappingId);
			if (textureMapping == null)
				return BadRequest("Invalid texture map specified");

			ModelMapping? modelMapping = await _modelMapRepository.GetModelMappingById(modelMappingId);
			if (modelMapping == null)
				return BadRequest("Invalid model map specified");

			using var streamReader = new StreamReader(file.OpenReadStream());
			var fileContent = await streamReader.ReadToEndAsync();

			BlockModel? blockModel = JsonConvert.DeserializeObject<BlockModel>(fileContent);
			if (blockModel == null)
				return BadRequest("Invalid model!");

			string fileName = file.FileName;
			List<string> ModelNames = new()
			{
				fileName.ToUpper().Replace(".JSON", "")
			};

			MCVersion version = new()
			{
				MinVersion = minVersion,
				MaxVersion = maxVersion
			};

			// Create Model Asset for imported block model
			// This will automatically convert texture paths to their equivalent GUID
			ModelAsset asset = new(blockModel, version, ModelNames, path, fileName, blockState?.Trim(), textureMapping.Assets);

			// Add model mapping
			await _modelMapRepository.AddModel(asset, modelMappingId);

			// Test converting the internal asset into a valid MC model
			// This will automatically convert GUIDs to valid texture paths for the specified version
			return Ok(asset.Serialize(textureMapping.Assets, MinecraftVersion.MC1194));
		}
	}
}
