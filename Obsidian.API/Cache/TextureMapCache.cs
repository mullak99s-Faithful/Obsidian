using Microsoft.Extensions.Caching.Memory;
using Obsidian.SDK.Models.Mappings;

namespace Obsidian.API.Cache
{
	public class TextureMapCache : ITextureMapCache
	{
		private readonly MemoryCache _cache;

		public TextureMapCache()
		{
			_cache = new MemoryCache(new MemoryCacheOptions());
		}

		public void Set(Guid id, TextureMapping map)
			=> _cache.Set(id, map, DateTimeOffset.Now.AddHours(1));

		public void Remove(Guid id)
			=> _cache.Remove(id);

		public bool TryGetValue(Guid id, out TextureMapping map)
			=> _cache.TryGetValue(id, out map);
	}

	public interface ITextureMapCache
	{
		void Set(Guid id, TextureMapping map);
		void Remove(Guid id);
		bool TryGetValue(Guid id, out TextureMapping map);
	}
}
