using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Obsidian.API.Logic;
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
		private readonly IPackLogic _packLogic;
		private readonly ILogger<BranchController> _logger;

		public BranchController(IPackRepository packRepository, IPackLogic packLogic, ILogger<BranchController> logger)
		{
			_packRepository = packRepository;
			_packLogic = packLogic;
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
			Pack? pack = await _packRepository.GetPackById(id);
			if (pack == null)
				return NotFound("Pack not found");

			return await _packLogic.AddBranch(pack, new PackBranch(branchName, version)) ? Ok() : BadRequest();
		}

		[HttpPost("Delete/{packId}/{branchId}")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:delete-branch")]
		public async Task<IActionResult> DeleteBranch([FromRoute] Guid id, [FromRoute] Guid branchId)
		{
			Pack? pack = await _packRepository.GetPackById(id);
			if (pack == null)
				return NotFound("Pack not found");

			PackBranch? branch = pack.Branches.FirstOrDefault(b => b.Id == branchId);
			if (branch == null)
				return NotFound("Branch not found");

			return await _packLogic.DeleteBranch(pack, branch) ? Ok() : BadRequest();
		}

		[HttpGet("GetProgress/{packId}/{branchId}")]
		[ProducesResponseType(typeof(PackReport), 200)]
		[SwaggerResponse(404, "Pack does not exist")]
		public async Task<IActionResult> GetProgressForBranch([FromRoute] Guid packId, [FromRoute] Guid branchId)
		{
			Pack? pack = await _packRepository.GetPackById(packId);
			if (pack == null)
				return NotFound("Pack not found");

			PackReport? packReport = pack.Branches.FirstOrDefault(b => b.Id == branchId)?.Report;
			if (packReport == null)
				return NotFound("Branch does not have a report");
			return Ok(packReport);
		}
	}
}
