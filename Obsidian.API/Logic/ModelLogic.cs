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

		public async Task<bool> AddModel(string fileName, string name, List<Guid> packIds, BlockModel model, string path, MinecraftVersion minVersion, MinecraftVersion? maxVersion, bool overwrite = false,  bool overwriteVersion = false)
		{
			List<Pack?> packs = new();
			foreach (var packId in packIds)
				packs.Add(await _packRepository.GetPackById(packId));

			foreach (var pack in packs)
			{
				if (pack?.ModelMappingsId == null || pack?.TextureMappingsId == null)
					continue;

				var texMappingTask = _textureMapRepository.GetTextureMappingById(pack.TextureMappingsId);
				var modelMappingTask = _modelMapRepository.GetModelMappingById(pack.ModelMappingsId.Value);
				await Task.WhenAll(texMappingTask, modelMappingTask);

				TextureMapping? texMapping = texMappingTask.Result;
				ModelMapping? mapping = modelMappingTask.Result;
				if (texMapping == null || mapping == null)
					continue;

				ModelAsset newModel = new(model, new MCVersion(minVersion, maxVersion), name, path, fileName, texMapping.Assets);

				ModelAsset? existingModel = await _modelMapRepository.GetExistingModel(newModel, mapping.Id);

				List<Task> modelTasks = new();

				if (existingModel != null)
				{
					if (overwrite)
						modelTasks.Add(_modelMapRepository.DeleteModel(existingModel.Id, mapping.Id));
					else return false;

					if (!overwriteVersion)
						newModel.MCVersion = existingModel.MCVersion;
				}

				modelTasks.Add(_modelMapRepository.AddModel(newModel, mapping.Id));
				await Task.WhenAll(modelTasks);
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

		public async Task<bool> DeleteAllModels(Guid mappingId)
			=> await _modelMapRepository.ClearModels(mappingId);
	}

	public interface IModelLogic
	{
		Task<bool> AddModel(string fileName, string name, List<Guid> packIds, BlockModel model, string path, MinecraftVersion minVersion, MinecraftVersion? maxVersion, bool overwrite = false, bool overwriteVersion = false);
		Task<List<ModelAsset>> SearchForModels(Guid packId, string searchQuery);
		Task<ModelAsset?> GetModel(Guid packId, string name, MinecraftVersion version);
		Task<bool> DeleteAllModels(Guid mappingId);
	}
}
