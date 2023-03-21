using Obsidian.SDK.Models;
using System.Text.Json;
using ObsidianAPI.Static;

namespace ObsidianAPI.Logic
{
	public interface IMappingLogic
	{
		public IEnumerable<TextureMapping> GetTextureMappings();
		public TextureMapping? GetTextureMapping(Guid guid);
		public TextureMapping? GetTextureMapping(string packName);
		public Task<bool> AddTextureMapping(string name, IFormFile file);
	}

	public class MappingLogic : IMappingLogic
	{
		public MappingLogic()
		{
			Globals.Init();
		}

		public IEnumerable<TextureMapping> GetTextureMappings()
			=> Globals.TextureMappings!;

		public TextureMapping? GetTextureMapping(Guid guid)
			=> Globals.TextureMappings!.Find(x => x.Id == guid);

		public TextureMapping? GetTextureMapping(string packName)
			=> Globals.TextureMappings!.Find(x => x.Name == packName);

		public async Task<bool> AddTextureMapping(string name, IFormFile file)
		{
			try
			{
				using var streamReader = new StreamReader(file.OpenReadStream());
				string json = await streamReader.ReadToEndAsync();
				List<Asset>? map = JsonSerializer.Deserialize<List<Asset>>(json);

				if (map != null)
				{
					Globals.TextureMappings!.Add(new TextureMapping()
					{
						Id = Guid.NewGuid(),
						Name = name,
						Assets = map
					});
					await Globals.SaveTextureMaps();
					return true;
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
			return false;
		}
	}
}
