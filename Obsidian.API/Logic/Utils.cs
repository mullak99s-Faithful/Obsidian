using System.IO.Compression;

namespace Obsidian.API.Logic
{
	public static class Utils
	{
		public static byte[] WriteEntryToByteArray(ZipArchiveEntry entry)
		{
			using var stream = new MemoryStream();
			using (var entryStream = entry.Open())
			{
				entryStream.CopyTo(stream);
			}
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
				archive.CreateEntryFromFile(filePath, entryPrefix + Path.GetFileName(filePath));

			foreach (string subdirectoryPath in Directory.GetDirectories(directoryPath))
			{
				string subdirectoryName = Path.GetFileName(subdirectoryPath);
				string entryName = entryPrefix + subdirectoryName + "/";

				archive.CreateEntry(entryName);
				AddDirectoryToZip(subdirectoryPath, archive, entryName);
			}
		}
	}
}
