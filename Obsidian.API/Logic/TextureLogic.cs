using Obsidian.API.Repository;
using Obsidian.SDK.Models.Assets;
using Obsidian.SDK.Models.Mappings;
using Pack = Obsidian.SDK.Models.Pack;

namespace Obsidian.API.Logic
{
	public class TextureLogic : ITextureLogic
	{
		private readonly ITextureMapRepository _textureMapRepository;
		private readonly IPackRepository _packRepository;
		private readonly ITextureBucket _textureBucket;
		private readonly IPackPngBucket _packPngBucket;

		public TextureLogic(ITextureMapRepository textureMapRepository, IPackRepository packRepository, ITextureBucket textureBucket, IPackPngBucket packPngBucket)
		{
			_textureMapRepository = textureMapRepository;
			_packRepository = packRepository;
			_textureBucket = textureBucket;
			_packPngBucket = packPngBucket;
		}

		private async Task Upload(Pack pack, TextureAsset asset, IFormFile textureFile, IFormFile? mcMetaFile)
		{
			if (textureFile is { Length: > 0 })
			{
				using var ms = new MemoryStream();
				await textureFile.CopyToAsync(ms);
				byte[] textureBytes = ms.ToArray();
				await _textureBucket.UploadTexture(pack.Id, asset.Id, textureBytes);
			}

			if (mcMetaFile is { Length: > 0 })
			{
				using var ms = new MemoryStream();
				await textureFile.CopyToAsync(ms);
				byte[] mcMetaBytes = ms.ToArray();
				await _textureBucket.UploadMCMeta(pack.Id, asset.Id, mcMetaBytes);
			}
		}

		public async Task<bool> AddTexture(string textureName, List<Guid> packIds, IFormFile textureFile, IFormFile? mcMetaFile)
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

				await Upload(pack, texture,  textureFile, mcMetaFile);
			}
			return true;
		}

		public async Task<bool> AddTexture(Guid assetId, List<Guid> packIds, IFormFile textureFile, IFormFile? mcMetaFile)
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
	}

	public interface ITextureLogic
	{
		Task<bool> AddTexture(string textureName, List<Guid> packIds, IFormFile textureFile, IFormFile? mcMetaFile);
		Task<bool> AddTexture(Guid assetId, List<Guid> packIds, IFormFile textureFile, IFormFile? mcMetaFile);
		Task<List<TextureAsset>> SearchForTextures(Guid packId, string searchQuery);
		Task<(string, byte[])> GetTexture(Guid packId, Guid assetId);
		Task<bool> DeleteAllTextures(Guid mappingId);
	}
}
