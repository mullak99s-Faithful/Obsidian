using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Obsidian.API.Logic;
using Obsidian.SDK.Enums;
using Obsidian.SDK.Models.Assets;
using Obsidian.SDK.Models.Minecraft;
using Swashbuckle.AspNetCore.Annotations;
using System.IO.Compression;

namespace Obsidian.API.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[SwaggerResponse(401, "You are not authorized to access this")]
	[SwaggerResponse(500, "An unexpected error occurred")]
	public class ModelController : ControllerBase
	{
		private readonly IModelLogic _logic;
		private readonly ILogger<BlockStateController> _logger;

		public ModelController(IModelLogic logic, ILogger<BlockStateController> logger)
		{
			_logic = logic;
			_logger = logger;
		}

		[HttpPost("Upload/ByName/{modelName}")]
		[Consumes("multipart/form-data")]
		[RequestFormLimits(MultipartBodyLengthLimit = 209715200)]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:upload-model")]
		public async Task<IActionResult> UploadModel([FromRoute] string modelName, [FromQuery] string packIds, string path, IFormFile modelFile, MinecraftVersion minVersion, MinecraftVersion? maxVersion)
		{
			var packIdList = packIds.Split(',').Select(Guid.Parse).ToList();

			if (modelFile.Length == 0)
				return BadRequest("Please select a file");
			if (string.IsNullOrWhiteSpace(modelName))
				return BadRequest("Please provide a name");
			if (packIdList.Count == 0)
				return BadRequest("Please provide pack ids");

			using var streamReader = new StreamReader(modelFile.OpenReadStream());
			var fileContent = await streamReader.ReadToEndAsync();

			BlockModel? blockModel = JsonConvert.DeserializeObject<BlockModel>(fileContent);
			if (blockModel == null)
				return BadRequest("Invalid model!");

			bool success = await _logic.AddModel(modelFile.FileName, modelName, packIdList, blockModel, path, minVersion, maxVersion);
			return success ? Ok() : BadRequest("Invalid");
		}

		[HttpPost("Import/FromZip")]
		[Consumes("multipart/form-data")]
		[RequestFormLimits(MultipartBodyLengthLimit = 209715200)]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:upload-model")]
		public async Task<IActionResult> Import([FromQuery] string packIds, string? nameSuffix, IFormFile zipFile, MinecraftVersion minVersion, MinecraftVersion? maxVersion, bool overwrite = false, bool overwriteVersion = false)
		{
			var packIdList = packIds.Split(',').Select(Guid.Parse).ToList();

			if (zipFile.Length == 0)
				return BadRequest("Please select a file");
			if (packIdList.Count == 0)
				return BadRequest("Please provide pack ids");

			using (var zipArchive = new ZipArchive(zipFile.OpenReadStream()))
			{
				foreach (var entry in zipArchive.Entries)
				{
					if (Path.GetExtension(entry.FullName).Equals(".json", StringComparison.OrdinalIgnoreCase))
					{
						await using var stream = entry.Open();
						string fileName = entry.Name;
						string path = Path.GetDirectoryName(entry.FullName) ?? string.Empty;

						string modelName = $"{fileName.ToLower().Replace(".json", "")}";

						List<string> ignoreDirs = new()
						{
							"block", "item"
						};

						// Include directory name to avoid multiple uses of files like "0.json" causing issues
						string parentDir = path?.Split('\\', '/').LastOrDefault()?.Trim() ?? string.Empty;
						if (!string.IsNullOrWhiteSpace(parentDir) && !ignoreDirs.Contains(parentDir))
							modelName = $"{parentDir}_{modelName}";

						if (!string.IsNullOrWhiteSpace(nameSuffix))
							modelName = $"{modelName}-{nameSuffix}";

						using var streamReader = new StreamReader(entry.Open());
						var fileContent = await streamReader.ReadToEndAsync();

						BlockModel? blockModel = JsonConvert.DeserializeObject<BlockModel>(fileContent);
						if (blockModel == null)
							return BadRequest("Invalid model!");

						Console.WriteLine($"FileName: {fileName}, ModelName: {modelName}, Path: {path}");
						await _logic.AddModel(fileName, modelName, packIdList, blockModel, path, minVersion, maxVersion, overwrite, overwriteVersion);
					}
				}
			}
			return Ok();
		}

		[HttpGet("Search/{packId}")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		public async Task<IActionResult> BlockStateSearch([FromRoute] Guid packId, [FromQuery] string query)
		{
			return Ok(await _logic.SearchForModels(packId, query));
		}

		[HttpGet("Get/{packId}/{modelName}")]
		[ProducesResponseType(typeof(FileContentResult), 200)]
		public async Task<IActionResult> GetBlockState([FromRoute] Guid packId, [FromRoute] string modelName, MinecraftVersion version)
		{
			if (string.IsNullOrEmpty(packId.ToString()))
				return BadRequest("Please provide an id");
			if (!string.IsNullOrEmpty(modelName))
				return BadRequest("Please provide a name");

			ModelAsset? modelAsset = await _logic.GetModel(packId, modelName, version);
			if (modelAsset == null)
				return NotFound("Model not found");

			return Ok(modelAsset.Model);
		}

		[HttpPost("Clear/{modelMappingId}")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:delete-model")]
		public async Task<IActionResult> ClearAllModels([FromRoute] Guid modelMappingId)
		{
			if (string.IsNullOrWhiteSpace(modelMappingId.ToString()))
				return BadRequest("Please provide an id");

			bool success = await _logic.DeleteAllModels(modelMappingId);
			return success ? Ok() : BadRequest("Invalid");
		}
	}
}
