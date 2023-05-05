﻿using MongoDB.Driver;
using Obsidian.SDK.Models;

namespace Obsidian.API.Repository
{
	public class TextureMapRepository : ITextureMapRepository
	{
		private readonly IMongoCollection<TextureMapping> _collection;

		public TextureMapRepository(IMongoDatabase database)
		{
			_collection = database.GetCollection<TextureMapping>("TextureMapping");
		}

		#region Create
		public async Task AddTextureMap(TextureMapping textureMapping)
			=> await _collection.InsertOneAsync(textureMapping);
		#endregion

		#region Read
		public async Task<TextureMapping?> GetTextureMappingById(Guid id)
		{
			try
			{
				var filter = Builders<TextureMapping>.Filter.Eq(t => t.Id, id);
				return await _collection.Find(filter).FirstOrDefaultAsync();
			}
			catch (Exception)
			{
				return null;
			}
		}

		public async Task<List<TextureMapping>> GetTextureMappingsByName(string name)
		{
			try
			{
				var filter = Builders<TextureMapping>.Filter.Eq(t => t.Name, name);
				return await _collection.Find(filter).ToListAsync();
			}
			catch (Exception)
			{
				return new List<TextureMapping>(0);
			}
		}

		public async Task<Dictionary<Guid, string>> GetAllTextureMappingIds()
		{
			var dictionary = new Dictionary<Guid, string>();
			try
			{
				var filter = Builders<TextureMapping>.Filter.Empty;
				var projection = Builders<TextureMapping>.Projection.Include(t => t.Id).Include(t => t.Name);
				var cursor = await _collection.Find(filter).Project(projection).ToCursorAsync();

				while (await cursor.MoveNextAsync())
				{
					foreach (var document in cursor.Current)
						dictionary.Add(document["_id"].AsGuid, document["Name"].AsString);
				}
				return dictionary;
			}
			catch (Exception)
			{
				return dictionary;
			}
		}

		public async Task<List<TextureMapping>> GetAllTextureMappings()
		{
			try
			{
				var filter = Builders<TextureMapping>.Filter.Empty;
				var documents = await _collection.Find(filter).ToListAsync();
				return documents;
			}
			catch (Exception)
			{
				return new List<TextureMapping>(0);
			}
		}

		public async Task<string> GetTextureMappingNameById(Guid id)
		{
			var filter = Builders<TextureMapping>.Filter.Eq(t => t.Id, id);
			var projection = Builders<TextureMapping>.Projection.Include(t => t.Name);

			var document = await _collection.Find(filter).Project(projection).FirstOrDefaultAsync();
			return document["Name"].AsString;
		}
		#endregion

		#region Update
		public async Task<bool> UpdateNameById(Guid id, string newName)
		{
			try
			{
				var filter = Builders<TextureMapping>.Filter.Eq(t => t.Id, id);
				var update = Builders<TextureMapping>.Update.Set(t => t.Name, newName);
				var updated = await _collection.UpdateOneAsync(filter, update);
				return updated.IsAcknowledged;
			}
			catch (Exception)
			{
				return false;
			}
		}
		#endregion

		#region Delete
		public async Task<bool> DeleteById(Guid id)
		{
			try
			{
				var filter = Builders<TextureMapping>.Filter.Eq(t => t.Id, id);
				var deleted = await _collection.DeleteOneAsync(filter);
				return deleted.IsAcknowledged;
			}
			catch (Exception)
			{
				return false;
			}
		}
		#endregion
	}

	public interface ITextureMapRepository
	{
		// Create
		Task AddTextureMap(TextureMapping textureMapping);

		// Read
		Task<TextureMapping?> GetTextureMappingById(Guid id);
		Task<List<TextureMapping>> GetTextureMappingsByName(string name);
		Task<Dictionary<Guid, string>> GetAllTextureMappingIds();
		Task<List<TextureMapping>> GetAllTextureMappings();
		Task<string> GetTextureMappingNameById(Guid id);

		// Update
		Task<bool> UpdateNameById(Guid id, string newName);

		// Delete
		Task<bool> DeleteById(Guid id);
	}
}