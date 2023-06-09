﻿using Microsoft.AspNetCore.Mvc;
using Obsidian.API.Repository;
using Swashbuckle.AspNetCore.Annotations;

namespace Obsidian.API.Controllers
{
	[ApiController]
	[ApiVersion("1.0")]
	[Route("api/v{apiVersion:apiVersion}/[controller]")]
	[SwaggerResponse(401, "You are not authorized to access this")]
	[SwaggerResponse(500, "An unexpected error occurred")]
	public partial class MappingController : ControllerBase
	{
		private readonly ITextureMapRepository _textureMapRepository;
		private readonly IModelMapRepository _modelMapRepository;
		private readonly IBlockStateMapRepository _blockStateMapRepository;
		private readonly ILogger<MappingController> _logger;

		public MappingController(ITextureMapRepository textureMapRepository, IModelMapRepository modelMapRepository, IBlockStateMapRepository blockStateMapRepository, ILogger<MappingController> logger)
		{
			_textureMapRepository = textureMapRepository;
			_modelMapRepository = modelMapRepository;
			_blockStateMapRepository = blockStateMapRepository;
			_logger = logger;
		}
	}
}
