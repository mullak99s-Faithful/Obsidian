using System.Text.Json;
using Obsidian.API.Static;
using Obsidian.SDK.Models;

namespace Obsidian.API.Logic
{
	public interface IMappingLogic
	{
		public IEnumerable<TextureMapping> GetTextureMappings();
		public IEnumerable<Guid> GetTextureMappingIds();
		public string? GetTextureMappingName(Guid guid);
		public TextureMapping? GetTextureMapping(Guid guid);
		public TextureMapping? GetTextureMapping(string packName);
		public Task<bool> AddTextureMapping(string name, IFormFile file);
		public Task<bool> RenameTextureMapping(Guid mapGuid, string name);
		public Task<bool> DeleteTextureMapping(Guid mapGuid);
	}

	public class MappingLogic : IMappingLogic
	{
		public MappingLogic()
			=> Globals.Init();

		public IEnumerable<TextureMapping> GetTextureMappings()
			=> Globals.TextureMappings!;

		public IEnumerable<Guid> GetTextureMappingIds()
			=> Globals.TextureMappings!.Select(x => x.Id);

		public string? GetTextureMappingName(Guid guid)
			=> Globals.TextureMappings!.Find(x => x.Id == guid)?.Name;

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

		public async Task<bool> RenameTextureMapping(Guid mapGuid, string name)
		{
			TextureMapping? texMap = Globals.TextureMappings!.Find(x => x.Id == mapGuid);
			if (texMap == null) return false;

			Globals.TextureMappings.Remove(texMap);

			if (!string.IsNullOrEmpty(name))
				texMap.Name = name;

			Globals.TextureMappings.Add(texMap);

			await Globals.SaveTextureMaps();
			return true;
		}

		public async Task<bool> DeleteTextureMapping(Guid mapGuid)
		{
			TextureMapping? texMap = Globals.TextureMappings?.Find(x => x.Id == mapGuid);
			if (texMap != null) return false;

			if (Globals.Packs?.Any(x => x.TextureMappingsId == mapGuid) ?? false)
				return false;

			Globals.TextureMappings?.Remove(texMap!);
			await Globals.SaveTextureMaps();
			return true;
		}
	}
}
