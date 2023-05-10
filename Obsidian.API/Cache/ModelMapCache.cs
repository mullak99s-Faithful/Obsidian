using Microsoft.Extensions.Caching.Memory;
using Obsidian.SDK.Models.Mappings;

namespace Obsidian.API.Cache
{
	public class ModelMapCache : IModelMapCache
	{
		private readonly MemoryCache _cache;

		public ModelMapCache()
		{
			_cache = new MemoryCache(new MemoryCacheOptions());
		}

		public void Set(Guid id, ModelMapping map)
			=> _cache.Set(id, map, DateTimeOffset.Now.AddHours(1));

		public void Remove(Guid id)
			=> _cache.Remove(id);

		public bool TryGetValue(Guid id, out ModelMapping map)
			=> _cache.TryGetValue(id, out map);
	}

	public interface IModelMapCache
	{
		void Set(Guid id, ModelMapping map);
		void Remove(Guid id);
		bool TryGetValue(Guid id, out ModelMapping map);
	}
}
