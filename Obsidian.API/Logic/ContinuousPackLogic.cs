using Obsidian.API.Repository;
using Obsidian.SDK.Models;
using Obsidian.SDK.Models.Assets;
using System.Security.Cryptography;

namespace Obsidian.API.Logic
{
	public class ContinuousPackLogic : IContinuousPackLogic
	{
		private readonly string _rootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Continuous");

		private readonly ITextureBucket _textureBucket;

		public ContinuousPackLogic(ITextureBucket textureBucket)
		{
			_textureBucket = textureBucket;
		}

		public void AddPack(Pack pack)
		{
			string packPath = GetPackPath(pack);
			if (!Directory.Exists(packPath))
			{
				Console.WriteLine($"[ContinuousPackLogic] Created pack {pack.Name} ({pack.Id})");
				Directory.CreateDirectory(packPath);

				#if DEBUG
				File.Create(Path.Combine(_rootPath, $"{pack.Name} - {pack.Id}")).Dispose();
				#endif
			}
		}

		public void DeletePack(Pack pack)
		{
			string packPath = GetPackPath(pack);
			if (Directory.Exists(packPath))
			{
				Console.WriteLine($"[ContinuousPackLogic] Deleted pack {pack.Name} ({pack.Id})");
				Task.Run(() => Utils.FastDeleteAll(packPath));

				#if DEBUG
				if (File.Exists(Path.Combine(_rootPath, $"{pack.Name} - {pack.Id}")))
					File.Delete(Path.Combine(_rootPath, $"{pack.Name} - {pack.Id}"));
				#endif
			}
		}

		public void AddBranch(Pack pack, PackBranch branch)
		{
			string branchPath = GetBranchPath(pack, branch);
			if (!Directory.Exists(branchPath))
			{
				Console.WriteLine($"[ContinuousPackLogic] Created branch {branch.Name} ({branch.Id}) for {pack.Name} ({pack.Id})");
				Directory.CreateDirectory(branchPath);

				#if DEBUG
				File.Create(Path.Combine(GetPackPath(pack), $"{branch.Name} - {branch.Id}")).Dispose();
				#endif
			}
		}

		public void DeleteBranch(Pack pack, PackBranch branch)
		{
			string branchPath = GetBranchPath(pack, branch);
			if (!Directory.Exists(branchPath))
			{
				Console.WriteLine($"[ContinuousPackLogic] Deleted branch {branch.Name} ({branch.Id}) for {pack.Name} ({pack.Id})");
				Task.Run(() => Utils.FastDeleteAll(branchPath));

				#if DEBUG
				if (File.Exists(Path.Combine(GetPackPath(pack), $"{branch.Name} - {branch.Id}")))
					File.Delete(Path.Combine(GetPackPath(pack), $"{branch.Name} - {branch.Id}"));
				#endif
			}
		}

		public async Task AddTexture(Pack pack, TextureAsset texture)
		{
			List<PackBranch> branches = pack.Branches;
			byte[]? textureBytes = await _textureBucket.DownloadTexture(pack.Id, texture.Id);
			byte[]? mcMetaBytes = null;
			if (textureBytes == null)
				return;

			List<string> texDestinationPaths = new();
			List<string> mcMetaDestinationPaths = new();

			foreach (PackBranch branch in branches)
			{
				string branchPath = GetBranchPath(pack, branch);
				foreach (var tex in texture.TexturePaths.Where(x => x.MCVersion.IsMatchingVersion(branch.Version)))
				{
					texDestinationPaths.Add(Path.Combine(branchPath, tex.Path));
					if (tex.MCMeta)
					{
						mcMetaBytes ??= await _textureBucket.DownloadMCMeta(pack.Id, texture.Id);
						mcMetaDestinationPaths.Add(Path.Combine(branchPath, tex.MCMetaPath));
					}
				}
			}

			List<Task> ioTasks = new();
			foreach (string destinationPath in texDestinationPaths)
			{
				string? dir = Path.GetDirectoryName(destinationPath);
				if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
					Directory.CreateDirectory(dir);

				ioTasks.Add(WriteChangedFileBytes(destinationPath, textureBytes));
			}

			if (mcMetaBytes != null && mcMetaDestinationPaths.Count > 0)
			{
				foreach (string destinationPath in mcMetaDestinationPaths)
				{
					string? dir = Path.GetDirectoryName(destinationPath);
					if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
						Directory.CreateDirectory(dir);

					ioTasks.Add(WriteChangedFileBytes(destinationPath, mcMetaBytes));
				}
			}
			await Task.WhenAll(ioTasks);
		}

		private async Task WriteChangedFileBytes(string path, byte[] bytes)
		{
			if (File.Exists(path))
			{
				try
				{
					using SHA1 sha1 = SHA1.Create();
					await using var fileStream = File.OpenRead(path);
					byte[] existingFileHash = await sha1.ComputeHashAsync(fileStream);
					byte[] newFileHash = sha1.ComputeHash(bytes);

					// Don't replace files if they are the same
					if (existingFileHash.SequenceEqual(newFileHash))
						return;
				}
				catch (Exception e)
				{
					Console.WriteLine($"[ContinuousPackLogic] Error: {e}");
				}
			}
			Console.WriteLine($"[ContinuousPackLogic] Added texture: {path}");
			await File.WriteAllBytesAsync(path, bytes);
		}

		private string GetPackPath(Pack pack)
			=> Path.Combine(_rootPath, pack.Id.ToString());

		private string GetBranchPath(Pack pack, PackBranch branch)
			=> Path.Combine(GetPackPath(pack), branch.Id.ToString());
	}

	public interface IContinuousPackLogic
	{
		void AddPack(Pack pack);
		void DeletePack(Pack pack);
		void AddBranch(Pack pack, PackBranch branch);
		void DeleteBranch(Pack pack, PackBranch branch);
		Task AddTexture(Pack pack, TextureAsset texture);
	}
}
