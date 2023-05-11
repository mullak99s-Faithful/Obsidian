using Obsidian.API.Repository;
using Obsidian.SDK.Enums;
using Obsidian.SDK.Models;
using Obsidian.SDK.Models.Mappings;
using Obsidian.SDK.Models.Assets;
using Obsidian.SDK.Models.Minecraft;
using Pack = Obsidian.SDK.Models.Pack;

namespace Obsidian.API.Logic
{
	public class ModelLogic : IModelLogic
	{
		private readonly IModelMapRepository _modelMapRepository;
		private readonly ITextureMapRepository _textureMapRepository;
		private readonly IPackRepository _packRepository;

		public ModelLogic(IModelMapRepository modelMapRepository, ITextureMapRepository textureMapRepository, IPackRepository packRepository)
		{
			_modelMapRepository = modelMapRepository;
			_textureMapRepository = textureMapRepository;
			_packRepository = packRepository;
		}

		public async Task<bool> AddModel(string fileName, string name, List<Guid> packIds, BlockModel model, string path, MinecraftVersion minVersion, MinecraftVersion maxVersion)
		{
			List<Pack?> packs = new();
			foreach (var packId in packIds)
				packs.Add(await _packRepository.GetPackById(packId));

			foreach (var pack in packs)
			{
				if (pack?.ModelMappingsId == null || pack?.TextureMappingsId == null)
					continue;

				TextureMapping? texMapping = await _textureMapRepository.GetTextureMappingById(pack.TextureMappingsId);
				if (texMapping == null)
					continue;

				ModelMapping? mapping = await _modelMapRepository.GetModelMappingById(pack.ModelMappingsId.Value);
				if (mapping == null)
					continue;

				ModelAsset newModel = new(model, new MCVersion(minVersion, maxVersion), name, path, fileName, texMapping.Assets);

				bool doesExist = await _modelMapRepository.DoesModelExist(newModel, mapping.Id);
				if (doesExist)
					return false;

				await _modelMapRepository.AddModel(newModel, mapping.Id);
			}
			return true;
		}

		public async Task<List<ModelAsset>> SearchForModels(Guid packId, string searchQuery)
		{
			Pack? pack = await _packRepository.GetPackById(packId);
			if (pack?.ModelMappingsId == null)
				return new List<ModelAsset>();

			ModelMapping? map = await _modelMapRepository.GetModelMappingById(pack.ModelMappingsId.Value);
			return map == null ? new List<ModelAsset>() : ModelSearch(map, searchQuery);
		}

		private List<ModelAsset> ModelSearch(ModelMapping map, string searchQuery)
		{
			ModelAsset? exactNameMatch = map.Models.Find(x => string.Equals(x.Name, searchQuery.ToLower().Trim(), StringComparison.Ordinal));
			List<ModelAsset> nameMatch = exactNameMatch != null
				? new List<ModelAsset> { exactNameMatch }
				: map.Models.FindAll(x => x.Name.Contains($"\\{searchQuery}"));

			if (nameMatch.Count > 0)
				return nameMatch;

			ModelAsset? exactFileNameMatch = map.Models.Find(x => string.Equals(x.FileName, searchQuery.ToLower().Trim(), StringComparison.Ordinal));
			List<ModelAsset> fileNameMatch = exactFileNameMatch != null
				? new List<ModelAsset> { exactFileNameMatch }
				: map.Models.FindAll(x => x.FileName.Contains($"\\{searchQuery}"));

			return fileNameMatch;
		}

		public async Task<ModelAsset?> GetModel(Guid packId, string name, MinecraftVersion version)
		{
			Pack? pack = await _packRepository.GetPackById(packId);
			if (pack?.ModelMappingsId == null)
				return null;

			ModelMapping? map = await _modelMapRepository.GetModelMappingById(pack.ModelMappingsId.Value);
			ModelAsset? model = map?.Models.FirstOrDefault(x => x.FileName == name.ToUpper() && x.MCVersion.IsMatchingVersion(version));
			return model;
		}
	}

	public interface IModelLogic
	{
		Task<bool> AddModel(string fileName, string name, List<Guid> packIds, BlockModel model, string path, MinecraftVersion minVersion, MinecraftVersion maxVersion);
		Task<List<ModelAsset>> SearchForModels(Guid packId, string searchQuery);
		Task<ModelAsset?> GetModel(Guid packId, string name, MinecraftVersion version);
	}
}
