using Obsidian.API.Repository;
using Obsidian.SDK.Models;

namespace Obsidian.API.Logic
{
	public class PackPngLogic : IPackPngLogic
	{
		private readonly IPackRepository _packRepository;
		private readonly IPackPngBucket _packPngBucket;

		public PackPngLogic(IPackRepository packRepository, IPackPngBucket packPngBucket)
		{
			_packRepository = packRepository;
			_packPngBucket = packPngBucket;
		}

		public async Task<bool> UploadPackPng(Guid packId, IFormFile packPng, bool overwrite)
		{
			if (packPng is not { Length: > 0 })
				return false;

			Pack? pack = await _packRepository.GetPackById(packId);
			if (pack == null)
				return false;

			using var ms = new MemoryStream();
			await packPng.CopyToAsync(ms);
			byte[] textureBytes = ms.ToArray();
			return await _packPngBucket.UploadPackPng(pack.Id, textureBytes, overwrite);
		}

		public async Task<byte[]?> DownloadPackPng(Guid packId)
			=> await _packPngBucket.DownloadPackPng(packId);

		public async Task<bool> DeletePackPng(Guid packId)
			=> await _packPngBucket.DeletePackPng(packId);
	}

	public interface IPackPngLogic
	{
		Task<bool> UploadPackPng(Guid packId, IFormFile packPng, bool overwrite);
		Task<byte[]?> DownloadPackPng(Guid packId);
		Task<bool> DeletePackPng(Guid packId);
	}
}
