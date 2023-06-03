using Obsidian.SDK.Enums;
using System.IO.Compression;

namespace Obsidian.SDK.Models.Assets
{
	public class MiscAsset
	{
		public Guid Id { get; set; }
		public MCVersion MCVersion { get; set; }
		public byte[]? ZipBytes { get; set; }

		public MiscAsset(MinecraftVersion minVersion, MinecraftVersion maxVersion)
		{
			Id = Guid.NewGuid();
			MCVersion = new MCVersion(minVersion, maxVersion);
		}

		public MiscAsset(MinecraftVersion minVersion, MinecraftVersion maxVersion, string path) : this(minVersion, maxVersion)
			=> Read(path);

		public MiscAsset(MinecraftVersion minVersion, MinecraftVersion maxVersion, byte[] zipBytes) : this(minVersion, maxVersion)
			=> ZipBytes = zipBytes;

		public MiscAsset() {}

		public async void Write(string path)
		{
			if (ZipBytes != null)
				await File.WriteAllBytesAsync(path, ZipBytes);
		}

		public async void Read(string path)
			=> ZipBytes = await File.ReadAllBytesAsync(path);

		public async Task Extract(string extractPath, bool overwrite = true)
		{
			if (ZipBytes == null) return;

			using var zipStream = new MemoryStream(ZipBytes);
			using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
			foreach (var entry in archive.Entries)
			{
				string entryPath = Path.Combine(extractPath, entry.FullName);
				entryPath = Path.GetFullPath(entryPath);

				if (Path.GetFileName(entryPath) == string.Empty)
					Directory.CreateDirectory(entryPath);
				else
					await Task.Run(() => entry.ExtractToFile(entryPath, overwrite));
			}
		}
	}
}
