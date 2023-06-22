using Obsidian.API.Repository;
using Obsidian.SDK.Models;
using Obsidian.SDK.Models.Assets;
using Obsidian.SDK.Models.Mappings;
using Obsidian.SDK.Models.Minecraft;
using Pack = Obsidian.SDK.Models.Pack;

namespace Obsidian.API.Logic
{
	public class TextureLogic : ITextureLogic
	{
		private readonly ITextureMapRepository _textureMapRepository;
		private readonly IPackRepository _packRepository;
		private readonly ITextureMetadataRepository _textureMetadata;
		private readonly ITextureBucket _textureBucket;
		private readonly IContinuousPackLogic _continuousPackLogic;

		public TextureLogic(ITextureMapRepository textureMapRepository, IPackRepository packRepository, ITextureMetadataRepository textureMetadata, ITextureBucket textureBucket, IContinuousPackLogic continuousPackLogic)
		{
			_textureMapRepository = textureMapRepository;
			_packRepository = packRepository;
			_textureMetadata = textureMetadata;
			_textureBucket = textureBucket;
			_continuousPackLogic = continuousPackLogic;
		}

		private async Task Upload(Pack pack, TextureAsset asset, IFormFile textureFile, IFormFile? mcMetaFile, string? credits = null, bool overwrite = false)
		{
			List<Task> uploadTasks = new();
			if (textureFile is { Length: > 0 })
			{
				using var ms = new MemoryStream();
				await textureFile.CopyToAsync(ms);
				byte[] textureBytes = ms.ToArray();
				uploadTasks.Add(_textureBucket.UploadTexture(pack.Id, asset.Id, textureBytes, overwrite));
			}
			if (mcMetaFile is { Length: > 0 })
			{
				using var ms = new MemoryStream();
				await textureFile.CopyToAsync(ms);
				byte[] mcMetaBytes = ms.ToArray();
				uploadTasks.Add(_textureBucket.UploadMCMeta(pack.Id, asset.Id, mcMetaBytes, overwrite));
			}
			await Task.WhenAll(uploadTasks);

			List<Task> postUploadTasks = new() { _continuousPackLogic.AddTexture(pack, asset), _continuousPackLogic.AddMetadata(pack) };

			if (credits != null)
				postUploadTasks.Add(AddCredit(pack, asset.Id, credits));

			await Task.WhenAll(postUploadTasks);
		}

		public async Task<bool> AddCredit(Pack pack, Guid assetId, string credits)
		{
			TextureMetadata metadata = await _textureMetadata.GetMetadata(pack.TextureMappingsId, assetId) ?? new()
			{
				Credit = credits
			};

			TextureMapping? mapping = await _textureMapRepository.GetTextureMappingById(pack.TextureMappingsId);
			TextureAsset? asset = mapping?.Assets.First(x => x.Id == assetId);
			if (asset == null)
				return false;

			return await _textureMetadata.AddOrUpdate(pack.Id, assetId, metadata);
		}

		public async Task<bool> AddCredit(Guid packId, Guid assetId, string credits)
		{
			Pack? pack = await _packRepository.GetPackById(packId);
			if (pack == null)
				return false;

			return await AddCredit(pack, assetId, credits);
		}

		public async Task<bool> AddTexture(string textureName, List<Guid> packIds, IFormFile textureFile, IFormFile? mcMetaFile, string? credits = null, bool overwrite = false)
		{
			List<Pack?> packs = new();
			foreach (var packId in packIds)
				packs.Add(await _packRepository.GetPackById(packId));

			foreach (var pack in packs)
			{
				if (pack == null)
					continue;

				TextureMapping? mapping = await _textureMapRepository.GetTextureMappingById(pack.TextureMappingsId);
				TextureAsset? texture = mapping?.Assets.First(x => x.Names.Contains(textureName.ToUpper()));
				if (texture == null)
					continue;

				await Upload(pack, texture, textureFile, mcMetaFile);
			}
			return true;
		}

		public async Task<bool> AddTexture(Guid assetId, List<Guid> packIds, IFormFile textureFile, IFormFile? mcMetaFile, string? credits = null, bool overwrite = false)
		{
			List<Pack?> packs = new();
			foreach (var packId in packIds)
				packs.Add(await _packRepository.GetPackById(packId));

			foreach (var pack in packs)
			{
				if (pack == null)
					continue;

				TextureMapping? mapping = await _textureMapRepository.GetTextureMappingById(pack.TextureMappingsId);
				TextureAsset? texture = mapping?.Assets.First(x => x.Id == assetId);
				if (texture == null)
					continue;

				await Upload(pack, texture, textureFile, mcMetaFile);
				NotifyCreditChanged(pack).ConfigureAwait(false).GetAwaiter();
			}
			return true;
		}

		public async Task<List<TextureAsset>> SearchForTextures(Guid packId, string searchQuery)
		{
			Pack? pack = await _packRepository.GetPackById(packId);
			if (pack == null)
				return new List<TextureAsset>();

			TextureMapping? map = await _textureMapRepository.GetTextureMappingById(pack.TextureMappingsId);
			if (map == null)
				return new List<TextureAsset>();

			TextureAsset? exactMatch = map.Assets.Find(x => x.Names.Any(y => string.Equals(y, searchQuery.ToUpper().Trim(), StringComparison.Ordinal)));

			return exactMatch != null
				? new List<TextureAsset> { exactMatch }
				: map.Assets.FindAll(x => x.TexturePaths.Any(y => y.Path.Contains($"\\{searchQuery}")));
		}

		public async Task<(string, byte[])> GetTexture(Guid packId, Guid assetId)
		{
			string texName = $"{assetId}.png";
			byte[] texture = await _textureBucket.DownloadTexture(packId, assetId) ?? Array.Empty<byte>();

			if (texture.Length > 0)
			{
				Pack? pack = await _packRepository.GetPackById(packId);
				if (pack != null)
				{
					TextureMapping? map = await _textureMapRepository.GetTextureMappingById(pack.TextureMappingsId);
					if (map != null)
					{
						TextureAsset? asset = map.Assets.Find(x => x.Id == assetId);
						texName = Path.GetFileName(asset?.TexturePaths.Last().Path) ?? $"{assetId}.png";
					}
				}
			}
			return (texName, texture);
		}

		public async Task<bool> DeleteAllTextures(Guid mappingId)
		{
			TextureMapping? map = await _textureMapRepository.GetTextureMappingById(mappingId);
			if (map == null)
				return false;

			List<Pack> packs = (await _packRepository.GetAllPacks()).Where(x => x.TextureMappingsId == mappingId).ToList();
			if (packs.Count == 0)
				return false;

			foreach (var asset in map.Assets)
			{
				foreach (var pack in packs)
				{
					List<Task> texTasks = new() { _textureBucket.DeleteTexture(pack.Id, asset.Id) };
					if (await _textureBucket.DoesMCMetaExist(pack.Id, asset.Id))
						texTasks.Add(_textureBucket.DeleteMCMeta(pack.Id, asset.Id));

					await Task.WhenAll(texTasks); // 1-2 Tasks. Can be done without overloading connections
				}
			}
			return await _textureMapRepository.ClearAssets(mappingId);
		}

		public async Task<Dictionary<Guid, string>> GetTextureCredit(Guid packId, Guid assetId)
		{
			var metadata = await _textureMetadata.GetMetadata(packId, assetId);

			return new Dictionary<Guid, string>
			{
				{ assetId, metadata?.Credit ?? "N/A" }
			};
		}

		public async Task<Dictionary<Guid, string>> SearchForTextureCredit(Guid packId, string searchQuery)
		{
			List<TextureAsset> assets = await SearchForTextures(packId, searchQuery);

			Dictionary<Guid, string> credits = new();

			foreach (var asset in assets)
			{
				var metadata = await _textureMetadata.GetMetadata(packId, asset.Id);
				credits.Add(asset.Id, metadata?.Credit ?? "N/A");
			}
			return credits;
		}

		public async Task<bool> DeleteAllCredits(Guid? mapId = null)
			=> await _textureMetadata.DeleteAllMetadata(mapId);

		public async Task NotifyCreditChanged(Pack pack, PackBranch? branch = null, List<TextureAsset>? textureAssets = null)
			=> await _continuousPackLogic.AddMetadata(pack, branch, textureAssets);
	}

	public interface ITextureLogic
	{
		Task<bool> AddTexture(string textureName, List<Guid> packIds, IFormFile textureFile, IFormFile? mcMetaFile, string? credits = null, bool overwrite = false);
		Task<bool> AddTexture(Guid assetId, List<Guid> packIds, IFormFile textureFile, IFormFile? mcMetaFile, string? credits = null, bool overwrite = false);
		Task<bool> AddCredit(Pack pack, Guid assetId, string credits);
		Task<bool> AddCredit(Guid packId, Guid assetId, string credits);
		Task<List<TextureAsset>> SearchForTextures(Guid packId, string searchQuery);
		Task<(string, byte[])> GetTexture(Guid packId, Guid assetId);
		Task<Dictionary<Guid, string>> GetTextureCredit(Guid packId, Guid assetId);
		Task<Dictionary<Guid, string>> SearchForTextureCredit(Guid packId, string searchQuery);
		Task<bool> DeleteAllTextures(Guid mappingId);
		Task NotifyCreditChanged(Pack pack, PackBranch? branch = null, List<TextureAsset>? textureAssets = null);
	}
}
