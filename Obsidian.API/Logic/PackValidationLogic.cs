using Obsidian.SDK.Models;
using System.Text.RegularExpressions;

namespace Obsidian.API.Logic
{
	public class PackValidationLogic : IPackValidationLogic
	{
		private readonly List<string> _textureBlacklist = new()
		{
			@"assets\/minecraft\/optifine",
			@"assets\/\b(?!minecraft\b|realms\b).*?\b",
			@"textures\/.+\/.+_e(missive)?\.png",
			@"assets\/minecraft\/optifine",
			@"textures\/misc",
			@"textures\/font",
			@"_MACOSX",
			@"assets\/minecraft\/textures\/ctm",
			@"assets\/minecraft\/textures\/custom",
			@"textures\/colormap",
			@"background\/panorama_overlay.png",
			@"assets\/minecraft\/textures\/environment\/clouds.png",
			@"assets\/minecraft\/textures\/block\/lightning_rod_on.png",
			@"assets\/realms\/textures\/gui\/realms\/inspiration.png",
			@"assets\/realms\/textures\/gui\/realms\/upload.png",
			@"assets\/realms\/textures\/gui\/realms\/survival_spawn.png",
			@"assets\/realms\/textures\/gui\/realms\/new_world.png",
			@"assets\/realms\/textures\/gui\/realms\/experience.png",
			@"assets\/realms\/textures\/gui\/realms\/adventure.png",
			@"assets\/minecraft\/textures\/gui\/presets",
			@"assets\/minecraft\/textures\/trims\/color_palettes",
			@"assets\/minecraft\/textures\/entity\/llama\/spit.png",
			@"assets\/minecraft\/textures\/gui\/title"
		};

		public List<string> GetAllBlacklistRules()
			=> _textureBlacklist;

		public async Task<PackReport> CompareTextures(string packFilesPath, List<string> refFiles)
			=> await CompareTextures(GetAllTextures(packFilesPath), refFiles);

		public async Task<PackReport> CompareTextures(List<string> packFiles, List<string> refFiles)
		{
			PackReport packReport = new PackReport();

			// Run through all of the reference (MC) textures
			Task refTask = Task.Run(() =>
			{
				refFiles.ForEach(x =>
				{
					if (!_textureBlacklist.Any(rule => Regex.IsMatch(x, rule)))
					{
						packReport.TotalTextures++;
						if (packFiles.Contains(x))
							packReport.MatchingTexturesCount++; // Pack contains this texture
						else
							packReport.MissingTextures.Add(x); // Pack doesn't contain this texture
					}
				});
			});

			// Run through all of the pack textures
			Task packTask = Task.Run(() =>
			{
				packFiles.ForEach(x =>
				{
					if (!_textureBlacklist.Any(rule => Regex.IsMatch(x, rule)) && !refFiles.Contains(x))
						packReport.UnusedTextures.Add(x); // MC doesn't contain this texture
				});
			});
			await Task.WhenAll(refTask, packTask); // Run async to improve speed
			return packReport;
		}

		public List<string> GetAllTextures(string path)
		{
			string[] pngFiles = Directory.GetFiles(path, "*.png", SearchOption.AllDirectories);
			return pngFiles.Select(file => file.Replace("/", "\\").Replace(path, "").Replace("\\", "/").TrimStart('/')).ToList();
		}
	}

	public interface IPackValidationLogic
	{
		List<string> GetAllBlacklistRules();
		Task<PackReport> CompareTextures(string packFilesPath, List<string> refFiles);
		Task<PackReport> CompareTextures(List<string> packFiles, List<string> refFiles);
		List<string> GetAllTextures(string path);
	}
}
