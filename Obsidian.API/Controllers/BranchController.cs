﻿using Microsoft.AspNetCore.Authorization;
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
	public class BranchController : ControllerBase
	{
		private readonly IPackLogic _logic;
		private readonly ILogger<BranchController> _logger;

		public BranchController(IPackLogic packLogic, ILogger<BranchController> logger)
		{
			_logic = packLogic;
			_logger = logger;
		}

		[HttpGet("GetForPackId/{id}")]
		[ProducesResponseType(typeof(List<PackBranch>), 200)]
		[SwaggerResponse(404, "Pack does not exist")]
		public IActionResult GetBranchesForPackId([FromRoute] Guid id)
		{
			Pack? pack = _logic.GetPack(id);

			if (pack == null)
				return NotFound("Pack not found");

			List<PackBranch> branches = pack.Branches;
			return Ok(branches);
		}

		[HttpGet("GetForPackName/{name}")]
		[ProducesResponseType(typeof(List<PackBranch>), 200)]
		[SwaggerResponse(404, "Pack does not exist")]
		public IActionResult GetBranchesForPackId([FromRoute] string name)
		{
			Pack? pack = _logic.GetPack(name);

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
			return await _logic.AddBranch(id, branchName, version) ? Ok() : BadRequest();
		}

		[HttpPost("Edit/{branchId}")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:edit-branch")]
		public async Task<IActionResult> DeleteBranch([FromRoute] Guid branchId, string? branchName, MinecraftVersion? version)
		{
			return await _logic.EditBranch(branchId, branchName, version) ? Ok() : BadRequest();
		}

		[HttpPost("Delete/{branchId}")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:delete-branch")]
		public async Task<IActionResult> DeleteBranch([FromRoute] Guid branchId)
		{
			return await _logic.DeleteBranch(branchId) ? Ok() : BadRequest();
		}
	}
}
