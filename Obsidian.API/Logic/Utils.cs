namespace Obsidian.API.Logic
{
	public static class Utils
	{
		public static async Task CopyFile(IFormFile file, string destinationPath)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(destinationPath) ?? string.Empty);
			await using var stream = file.OpenReadStream();
			await using var fileStream = new FileStream(destinationPath, FileMode.Create);
			await stream.CopyToAsync(fileStream);
		}

		public static void CopyFile(string sourceFilePath, string destinationFilePath, bool overwrite = false)
		{
			if (!File.Exists(sourceFilePath)) return;

			string? dir = Path.GetDirectoryName(destinationFilePath);
			if (string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir)) return;

			Directory.CreateDirectory(dir);
			File.Copy(sourceFilePath, destinationFilePath, overwrite);
		}
	}
}
