using System.IO.Compression;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Obsidian.API.Logic;
using Obsidian.SDK.Enums;
using Obsidian.SDK.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace Obsidian.API.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[SwaggerResponse(401, "You are not authorized to access this")]
	[SwaggerResponse(500, "An unexpected error occurred")]
	public class BlockStateController : ControllerBase
	{
		private readonly IBlockStateLogic _logic;
		private readonly ILogger<BlockStateController> _logger;

		public BlockStateController(IBlockStateLogic logic, ILogger<BlockStateController> logger)
		{
			_logic = logic;
			_logger = logger;
		}

		[HttpPost("Upload/ByName/{blockStateName}")]
		[Consumes("multipart/form-data")]
		[RequestFormLimits(MultipartBodyLengthLimit = 209715200)]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:upload-texture")]
		public async Task<IActionResult> UploadBlockState([FromRoute] string blockStateName, [FromQuery] string packIds, IFormFile blockStateFile, MinecraftVersion minVersion, MinecraftVersion maxVersion)
		{
			var packIdList = packIds.Split(',').Select(Guid.Parse).ToList();

			if (blockStateFile.Length == 0)
				return BadRequest("Please select a file");
			if (string.IsNullOrWhiteSpace(blockStateName))
				return BadRequest("Please provide a name");
			if (packIdList.Count == 0)
				return BadRequest("Please provide pack ids");

			using var ms = new MemoryStream();
			await blockStateFile.CopyToAsync(ms);
			byte[] blockStateBytes = ms.ToArray();

			return await _logic.AddBlockState(blockStateName, packIdList, blockStateFile.FileName, blockStateBytes, minVersion, maxVersion) ? Ok() : BadRequest("Invalid");
		}

		[HttpPost("Import/FromZip")]
		[Consumes("multipart/form-data")]
		[RequestFormLimits(MultipartBodyLengthLimit = 209715200)]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:upload-texture")]
		public async Task<IActionResult> Import([FromQuery] string packIds, string? nameSuffix, IFormFile zipFile, MinecraftVersion minVersion, MinecraftVersion maxVersion)
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
						using var memoryStream = new MemoryStream();
						await stream.CopyToAsync(memoryStream);
						var jsonBytes = memoryStream.ToArray();

						string fileName = entry.Name;

						string blockStateName = $"{fileName.ToLower().Replace(".json", "")}";
						if (!string.IsNullOrWhiteSpace(nameSuffix))
							blockStateName = $"{blockStateName}-{nameSuffix}";

						Console.WriteLine($"Added blockstate {fileName}!");
						await _logic.AddBlockState(blockStateName, packIdList, fileName, jsonBytes, minVersion, maxVersion);
					}
				}
			}
			return Ok();
		}

		[HttpGet("Search/{packId}")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		public async Task<IActionResult> BlockStateSearch([FromRoute] Guid packId, [FromQuery] string query)
		{
			return Ok(await _logic.SearchForBlockStates(packId, query));
		}

		[HttpGet("Get/{packId}/{blockStateName}")]
		[ProducesResponseType(typeof(FileContentResult), 200)]
		public async Task<IActionResult> GetBlockState([FromRoute] Guid packId, [FromRoute] string blockStateName, MinecraftVersion version)
		{
			if (string.IsNullOrEmpty(packId.ToString()))
				return BadRequest("Please provide an id");
			if (!string.IsNullOrEmpty(blockStateName))
				return BadRequest("Please provide a name");

			BlockState? blockState = await _logic.GetBlockState(packId, blockStateName, version);
			if (blockState == null)
				return NotFound("Blockstate not found");

			FileContentResult fileContentResult = new(blockState.Data, "application/json")
			{
				FileDownloadName = blockState.FileName
			};
			return fileContentResult;
		}

		[HttpPost("Clear/{blockStateMappingId}")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:upload-texture")]
		public async Task<IActionResult> ClearAllBlockStates([FromRoute] Guid blockStateMappingId)
		{
			if (string.IsNullOrWhiteSpace(blockStateMappingId.ToString()))
				return BadRequest("Please provide an id");

			bool success = await _logic.DeleteAllBlockStates(blockStateMappingId);
			return success ? Ok() : BadRequest("Invalid");
		}
	}
}
