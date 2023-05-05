using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Obsidian.API.Repository;
using Obsidian.SDK.Enums;
using Obsidian.SDK.Models;
using Swashbuckle.AspNetCore.Annotations;

namespace Obsidian.API.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	[SwaggerResponse(401, "You are not authorized to access this")]
	[SwaggerResponse(500, "An unexpected error occurred")]
	public class BranchController : ControllerBase
	{
		private readonly IPackRepository _packRepository;
		private readonly ILogger<BranchController> _logger;

		public BranchController(IPackRepository packRepository, ILogger<BranchController> logger)
		{
			_packRepository = packRepository;
			_logger = logger;
		}

		[HttpGet("GetForPackId/{id}")]
		[ProducesResponseType(typeof(List<PackBranch>), 200)]
		[SwaggerResponse(404, "Pack does not exist")]
		public async Task<IActionResult> GetBranchesForPackId([FromRoute] Guid id)
		{
			Pack? pack = await _packRepository.GetPackById(id);

			if (pack == null)
				return NotFound("Pack not found");

			List<PackBranch> branches = pack.Branches;
			return Ok(branches);
		}

		[HttpGet("GetForPackName/{name}")]
		[ProducesResponseType(typeof(List<PackBranch>), 200)]
		[SwaggerResponse(404, "Pack does not exist")]
		public async Task<IActionResult> GetBranchesForPackName([FromRoute] string name)
		{
			Pack? pack = await _packRepository.GetPackByName(name);

			if (pack == null)
				return NotFound("Pack not found");

			List<PackBranch> branches = pack.Branches;
			return Ok(branches);
		}

		[HttpPost("Add/{id}")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:add-branch")]
		public async Task<IActionResult> AddBranch([FromRoute] Guid id, string branchName, MinecraftVersion version)
		{
			return await _packRepository.AddBranch(id, new PackBranch(branchName, version)) ? Ok() : BadRequest();
		}

		[HttpPost("Delete/{packId}/{branchId}")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:delete-branch")]
		public async Task<IActionResult> DeleteBranch([FromRoute] Guid packId, [FromRoute] Guid branchId)
		{
			return await _packRepository.DeleteBranch(packId, branchId) ? Ok() : BadRequest();
		}
	}
}
