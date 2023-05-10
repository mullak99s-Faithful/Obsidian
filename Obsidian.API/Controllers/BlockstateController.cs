using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Obsidian.API.Repository;
using Swashbuckle.AspNetCore.Annotations;
using System.IO.Compression;

namespace Obsidian.API.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[SwaggerResponse(401, "You are not authorized to access this")]
	[SwaggerResponse(500, "An unexpected error occurred")]
	public class BlockstateController : ControllerBase
	{
		private readonly IBlockstateBucket _blockstateBucket;
		private readonly IPackRepository _packRepository;
		private readonly ILogger<BlockstateController> _logger;

		public BlockstateController(IBlockstateBucket blockstateBucket, IPackRepository packRepository, ILogger<BlockstateController> logger)
		{
			_blockstateBucket = blockstateBucket;
			_packRepository = packRepository;
			_logger = logger;
		}

		[HttpPost("Upload")]
		[Consumes("multipart/form-data")]
		[RequestFormLimits(MultipartBodyLengthLimit = 209715200)]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:upload-texture")]
		public async Task<IActionResult> UploadBlockstate([FromQuery] string packIds, IFormFile blockState, bool overwrite = false)
		{
			var packIdList = packIds.Split(',').Select(Guid.Parse).ToList();

			if (blockState.Length == 0)
				return BadRequest("Please select a file");
			if (packIdList.Count == 0)
				return BadRequest("Please provide pack ids");

			List<bool> success = new();
			foreach (Guid packId in packIdList)
			{
				if (await _packRepository.GetPackById(packId) == null)
					continue;

				using var ms = new MemoryStream();
				await blockState.CopyToAsync(ms);
				byte[] blockStateBytes = ms.ToArray();

				if (await _blockstateBucket.UploadBlockstate(packId, blockState.FileName, blockStateBytes, overwrite))
					success.Add(true);
			}
			return success.Any(x => x) ? Ok() : BadRequest("Invalid");
		}

		[HttpPost("Upload/Multiple")]
		[Consumes("multipart/form-data")]
		[RequestFormLimits(MultipartBodyLengthLimit = 209715200)]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:upload-texture")]
		public async Task<IActionResult> UploadBlockstates([FromQuery] string packIds, IFormFile zipFile, bool overwrite = false)
		{
			var packIdList = packIds.Split(',').Select(Guid.Parse).ToList();

			if (zipFile.Length == 0)
				return BadRequest("Please select a file");
			if (packIdList.Count == 0)
				return BadRequest("Please provide pack ids");

			List<bool> success = new();
			foreach (Guid packId in packIdList)
			{
				if (await _packRepository.GetPackById(packId) == null)
					continue;

				using var zipArchive = new ZipArchive(zipFile.OpenReadStream(), ZipArchiveMode.Read);
				foreach (var entry in zipArchive.Entries)
				{
					if (entry.Name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
					{
						await using var stream = entry.Open();
						using var memoryStream = new MemoryStream();
						await stream.CopyToAsync(memoryStream);
						var jsonBytes = memoryStream.ToArray();

						if (await _blockstateBucket.UploadBlockstate(packId, entry.Name, jsonBytes, overwrite))
							success.Add(true);
					}
				}
			}
			return success.Any(x => x) ? Ok() : BadRequest("Invalid");
		}

		[HttpPost("Delete/{name}")]
		[Consumes("multipart/form-data")]
		[RequestFormLimits(MultipartBodyLengthLimit = 209715200)]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:upload-texture")]
		public async Task<IActionResult> DeleteBlockstate([FromRoute] string name, [FromQuery] string packIds)
		{
			var packIdList = packIds.Split(',').Select(Guid.Parse).ToList();

			if (string.IsNullOrWhiteSpace(name))
				return BadRequest("Please specify a name");
			if (packIdList.Count == 0)
				return BadRequest("Please provide pack ids");

			List<bool> success = new();
			foreach (Guid packId in packIdList)
			{
				if (await _packRepository.GetPackById(packId) == null)
					continue;

				if (await _blockstateBucket.DeleteBlockstate(packId, name))
					success.Add(true);
			}
			return success.Any(x => x) ? Ok() : BadRequest("Invalid");
		}
	}
}
