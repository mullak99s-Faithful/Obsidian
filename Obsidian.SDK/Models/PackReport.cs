using System.Text;

namespace Obsidian.SDK.Models
{
	public class PackReport
	{
		public List<string> MissingTextures { get; set; } = new();
		public List<string> UnusedTextures { get; set; } = new();

		public int MatchingTexturesCount { get; set; } = 0;

		public int TotalTextures { get; set; } = 0;

		public int Matching
			=> MatchingTexturesCount;
		public int Missing
			=> MissingTextures.Count;
		public int Unused
			=> UnusedTextures.Count;

		public string MatchingTotal
			=> $"{Matching} / {TotalTextures} ({Math.Round(((double)Matching / TotalTextures) * 100, 2)}%)";

		public string MissingTotal
			=> $"{Missing} / {TotalTextures} ({Math.Round(((double)Missing / TotalTextures) * 100, 2)}%)";

		public async Task GenerateReport(string pathAndFileName)
		{
			var sb = new StringBuilder();
			sb.AppendLine($"Matching: {MatchingTotal}");
			sb.AppendLine($"Missing: {MissingTotal}");
			sb.AppendLine($"Unused: {Unused}");
			sb.AppendLine("------------------------------\n");
			sb.AppendLine("Missing Textures:");
			foreach (var texture in MissingTextures)
				sb.AppendLine(texture);
			sb.AppendLine("------------------------------\n");
			sb.AppendLine("Unused Textures:");
			foreach (var texture in UnusedTextures)
				sb.AppendLine(texture);

			await File.WriteAllTextAsync(pathAndFileName, sb.ToString());
		}
	}
}
