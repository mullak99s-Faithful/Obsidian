using Obsidian.API.Repository;
using Obsidian.SDK.Enums;
using Obsidian.SDK.Models;
using Pack = Obsidian.SDK.Models.Pack;

namespace Obsidian.API.Logic
{
	public interface ITextureLogic
	{
		public Task<bool> AddTexture(string textureName, List<Guid> packIds, IFormFile textureFile, IFormFile? mcMetaFile);
		public Task<bool> AddTexture(Guid assetId, List<Guid> packIds, IFormFile textureFile, IFormFile? mcMetaFile);
		public bool ImportPack(MinecraftVersion version, List<Guid> packIds, IFormFile packFile, bool overwrite);
		public bool GeneratePacks(List<Guid> packIds);
		public Task<List<Asset>> SearchForTextures(Guid packId, string searchQuery);
		public Task<(string, byte[])> GetTexture(Guid packId, Guid assetId);
	}

	public class TextureLogic : ITextureLogic
	{
		private readonly ITextureMapRepository _textureMapRepository;
		private readonly IPackRepository _packRepository;
		private readonly ITextureBucket _textureBucket;

		public TextureLogic(ITextureMapRepository textureMapRepository, IPackRepository packRepository, ITextureBucket textureBucket)
		{
			_textureMapRepository = textureMapRepository;
			_packRepository = packRepository;
			_textureBucket = textureBucket;
		}

		private async Task Upload(Pack pack, Asset asset, IFormFile textureFile, IFormFile? mcMetaFile)
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
				Asset? texture = mapping?.Assets.First(x => x.Names.Contains(textureName.ToUpper()));
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
				Asset? texture = mapping?.Assets.First(x => x.Id == assetId);
				if (texture == null)
					continue;

				await Upload(pack, texture, textureFile, mcMetaFile);
			}
			return true;
		}

		public bool ImportPack(MinecraftVersion version, List<Guid> packIds, IFormFile packFile, bool overwrite)
		{
			throw new NotImplementedException();
		}

		public bool GeneratePacks(List<Guid> packIds)
		{
			throw new NotImplementedException();
		}

		public async Task<List<Asset>> SearchForTextures(Guid packId, string searchQuery)
		{
			Pack? pack = await _packRepository.GetPackById(packId);
			if (pack == null)
				return new List<Asset>();

			TextureMapping? map = await _textureMapRepository.GetTextureMappingById(pack.TextureMappingsId);
			if (map == null)
				return new List<Asset>();

			Asset? exactMatch = map.Assets.Find(x => x.Names.Any(y => string.Equals(y, searchQuery.ToUpper().Trim(), StringComparison.Ordinal)));

			return exactMatch != null
				? new List<Asset> { exactMatch }
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
						Asset? asset = map.Assets.Find(x => x.Id == assetId);
						texName = Path.GetFileName(asset?.TexturePaths.Last().Path) ?? $"{assetId}.png";
					}
				}
			}
			return (texName, texture);
		}
	}
}
