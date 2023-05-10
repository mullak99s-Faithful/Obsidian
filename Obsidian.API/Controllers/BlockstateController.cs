using Microsoft.AspNetCore.Mvc;
using Obsidian.API.Repository;
using Swashbuckle.AspNetCore.Annotations;

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
	}
}
