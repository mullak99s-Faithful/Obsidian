using System.IO.Compression;

namespace Obsidian.API.Logic
{
	public static class Utils
	{
		private const CompressionLevel COMPRESSION_LEVEL = CompressionLevel.Optimal;

		public static byte[] WriteEntryToByteArray(ZipArchiveEntry entry)
		{
			using var stream = new MemoryStream();
			using (var entryStream = entry.Open())
				entryStream.CopyTo(stream);
			return stream.ToArray();
		}

		public static void FastDeleteAll(string path)
		{
			Parallel.ForEach(Directory.GetFiles(path), File.Delete);
			Parallel.ForEach(Directory.GetDirectories(path), FastDeleteAll);
			Directory.Delete(path, false);
		}

		public static void CreateZipArchive(string directoryPath, string archivePath)
		{
			ZipArchive archive = ZipFile.Open(archivePath, ZipArchiveMode.Create);
			AddDirectoryToZip(directoryPath, archive, "");
			archive.Dispose();
		}

		private static void AddDirectoryToZip(string directoryPath, ZipArchive archive, string entryPrefix)
		{
			foreach (string filePath in Directory.GetFiles(directoryPath))
				archive.CreateEntryFromFile(filePath, entryPrefix + Path.GetFileName(filePath), COMPRESSION_LEVEL);

			foreach (string subDirectoryPath in Directory.GetDirectories(directoryPath))
			{
				string subDirectoryName = Path.GetFileName(subDirectoryPath);
				string entryName = entryPrefix + subDirectoryName + "/";

				archive.CreateEntry(entryName, COMPRESSION_LEVEL);
				AddDirectoryToZip(subDirectoryPath, archive, entryName);
			}
		}
	}
}
