﻿using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Obsidian.SDK.Models.Tools;

namespace Obsidian.API.Cache
{
	public class VersionAssetsCache : IVersionAssetsCache
	{
		private readonly MemoryCache _cache;

		public VersionAssetsCache()
		{
			_cache = new MemoryCache(new MemoryCacheOptions());
		}

		public void Set(string name, string edition, int assetVersion, MinecraftVersionAssets assets)
			=> _cache.Set(ConvertToId(name, edition, assetVersion), assets, DateTimeOffset.Now.AddHours(12));

		public void Remove(string name, string edition, int assetVersion)
			=> _cache.Remove(ConvertToId(name, edition, assetVersion));

		public bool TryGetValue(string name, string edition, int assetVersion, out MinecraftVersionAssets assets)
			=> _cache.TryGetValue(ConvertToId(name, edition, assetVersion), out assets);

		private string ConvertToId(string name, string edition, int assetVersion)
		{
			string input = $"{name}:{edition}:{assetVersion}";

			using SHA1 sha1 = SHA1.Create();
			byte[] inputBytes = Encoding.UTF8.GetBytes(input);
			byte[] hashBytes = sha1.ComputeHash(inputBytes);

			return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
		}
	}

	public interface IVersionAssetsCache
	{
		void Set(string name, string edition, int assetVersion, MinecraftVersionAssets assets);
		void Remove(string name, string edition, int assetVersion);
		bool TryGetValue(string name, string edition, int assetVersion, out MinecraftVersionAssets assets);
	}
}
