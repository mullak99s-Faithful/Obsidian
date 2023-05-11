using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Obsidian.SDK.Models.Mappings;
using Swashbuckle.AspNetCore.Annotations;

namespace Obsidian.API.Controllers
{
    public partial class MappingController
	{
		[HttpPost("BlockStateMap/Add/Blank/{name}")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:add-mapping")]
		public async Task<IActionResult> AddBlockStateMapping([FromRoute] string name)
		{
			await _blockStateMapRepository.AddBlockStateMap(new BlockStateMapping(name));
			return Ok();
		}

		[HttpGet("BlockStateMap/GetAll")]
		[ProducesResponseType(typeof(IEnumerable<TextureMapping>), 200)]
		[SwaggerResponse(404, "No texture mappings exist")]
		public async Task<IActionResult> GetBlockStateMappings()
		{
			IEnumerable<BlockStateMapping> maps = await _blockStateMapRepository.GetAllBlockStateMappings();
			if (!maps.Any())
				return NotFound();
			return Ok(maps);
		}

		[HttpGet("BlockStateMap/GetAllIds")]
		[ProducesResponseType(typeof(Dictionary<Guid, string>), 200)]
		[SwaggerResponse(404, "No texture mappings exist")]
		public async Task<IActionResult> GetBlockStateIds()
		{
			Dictionary<Guid, string> maps = await _blockStateMapRepository.GetAllBlockStateMappingIds();
			if (!maps.Any())
				return NotFound();
			return Ok(maps);
		}

		[HttpGet("BlockStateMap/GetName/{id}")]
		[ProducesResponseType(typeof(string), 200)]
		[SwaggerResponse(404, "No block state mappings exist")]
		public async Task<IActionResult> GetBlockStateMappingName([FromRoute] Guid id)
		{
			if (string.IsNullOrWhiteSpace(id.ToString()))
				return BadRequest("No id provided");

			string mapName = await _blockStateMapRepository.GetBlockStateMappingNameById(id);
			if (string.IsNullOrWhiteSpace(mapName))
				return NotFound();
			return Ok(mapName);
		}

		[HttpGet("BlockStateMap/Get/{id}")]
		[ProducesResponseType(typeof(TextureMapping), 200)]
		[SwaggerResponse(404, "Block State mapping does not exist")]
		public async Task<IActionResult> GetBlockStateMapping([FromRoute] Guid id)
		{
			BlockStateMapping? map = await _blockStateMapRepository.GetBlockStateMappingById(id);
			if (map == null)
				return NotFound();
			return Ok(map);
		}

		[HttpPost("BlockStateMap/Rename/{mapGuid}")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:rename-mapping")]
		public async Task<IActionResult> RenameBlockStateMapping([FromRoute] Guid mapGuid, string name)
		{
			if (string.IsNullOrEmpty(mapGuid.ToString()))
				return BadRequest("Please provide an id");
			if (string.IsNullOrEmpty(name))
				return BadRequest("Please provide a name");

			return await _blockStateMapRepository.UpdateNameById(mapGuid, name) ? Ok() : BadRequest();
		}

		[HttpPost("BlockStateMap/Delete/{mapGuid}")]
		[ProducesResponseType(typeof(IActionResult), 200)]
		[Authorize("write:delete-mapping")]
		public async Task<IActionResult> DeleteBlockStateMapping([FromRoute] Guid mapGuid)
		{
			if (string.IsNullOrEmpty(mapGuid.ToString()))
				return BadRequest("Please provide an id");

			return await _blockStateMapRepository.DeleteById(mapGuid) ? Ok() : BadRequest();
		}
	}
}
