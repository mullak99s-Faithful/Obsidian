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
			if (File.Exists(sourceFilePath))
			{
				Directory.CreateDirectory(Path.GetDirectoryName(destinationFilePath));
				File.Copy(sourceFilePath, destinationFilePath, overwrite);
			}
		}
	}
}
