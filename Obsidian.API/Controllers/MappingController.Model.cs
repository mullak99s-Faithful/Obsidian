using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Obsidian.SDK.Enums;
using Obsidian.SDK.Models.Assets;
using Obsidian.SDK.Models.Mappings;
using Obsidian.SDK.Models.Minecraft;
using Swashbuckle.AspNetCore.Annotations;

namespace Obsidian.API.Controllers
{
    public partial class MappingController
	{
		[HttpPost("ModelMap/Individual/Import/{textureMappingId}")]
		//[ProducesResponseType(typeof(IEnumerable<TextureMapping>), 200)]
		[SwaggerResponse(404, "No texture mappings exist")]
		public async Task<IActionResult> ImportModel([FromRoute] Guid textureMappingId, string path, IFormFile? file)
		{
			if (file == null || file.Length == 0)
				return BadRequest("No file specified");

			TextureMapping? textureMapping = await _textureMapRepository.GetTextureMappingById(textureMappingId);
			if (textureMapping == null)
				return BadRequest("Invalid texture map specified");

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

			// Create Model Asset for imported block model
			// This will automatically convert texture paths to their equivalent GUID
			ModelAsset asset = new(blockModel, ModelNames, path, fileName, textureMapping.Assets);

			// Test converting the internal asset into a valid MC model
			// This will automatically convert GUIDs to valid texture paths for the specified version
			return Ok(asset.Serialize(textureMapping.Assets, MinecraftVersion.MC1194));
		}
	}
}
