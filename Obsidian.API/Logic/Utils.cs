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
			try
			{
				Parallel.ForEach(Directory.GetFiles(path), File.Delete);
				Parallel.ForEach(Directory.GetDirectories(path), FastDeleteAll);
				Directory.Delete(path, false);
			}
			catch (Exception)
			{
				// Ignore exceptions
			}
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

		public static async Task<byte[]> GetBytesFromFormFileAsync(IFormFile formFile)
		{
			using var memoryStream = new MemoryStream();
			await formFile.CopyToAsync(memoryStream);
			return memoryStream.ToArray();
		}

		public static bool IsZipFile(byte[] fileBytes)
		{
			if (fileBytes.Length < 4)
				return false;

			return fileBytes[0] == 0x50 && fileBytes[1] == 0x4B && fileBytes[2] == 0x03 && fileBytes[3] == 0x04;
		}

		// TODO: Needs redoing, doesn't work correctly atm
		public static async Task ProcessTasksInBatches(List<Task> tasks, int batchSize = 50)
		{
			int totalTasks = tasks.Count;
			int processedTasks = 0;

			while (processedTasks < totalTasks)
			{
				Task[] batch = tasks.Skip(processedTasks).Take(batchSize).ToArray();
				await Task.WhenAll(batch);

				processedTasks += batch.Length;
			}
		}
	}
}
