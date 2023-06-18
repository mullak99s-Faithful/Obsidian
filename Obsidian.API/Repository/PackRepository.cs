using LibGit2Sharp;
using MongoDB.Driver;
using Obsidian.SDK.Models;
using Pack = Obsidian.SDK.Models.Pack;

namespace Obsidian.API.Repository
{
	public class PackRepository : IPackRepository
	{
		private readonly IMongoCollection<Pack> _collection;

		public PackRepository(IMongoDatabase database)
		{
			_collection = database.GetCollection<Pack>("Pack");
		}

		public async Task<bool> AddPack(Pack pack)
		{
			if (await GetPackById(pack.Id) != null || await GetPackByName(pack.Name) != null)
				return false;

			await _collection.InsertOneAsync(pack);
			return true;
		}

		public async Task<Pack?> GetPackById(Guid id)
		{
			try
			{
				var filter = Builders<Pack>.Filter.Eq(t => t.Id, id);
				return await _collection.Find(filter).FirstOrDefaultAsync();
			}
			catch (Exception)
			{
				return null;
			}
		}

		public async Task<Pack?> GetPackByName(string name)
		{
			try
			{
				var filter = Builders<Pack>.Filter.Eq(t => t.Name, name);
				return await _collection.Find(filter).FirstOrDefaultAsync();
			}
			catch (Exception)
			{
				return null;
			}
		}

		public async Task<Dictionary<Guid, string>> GetAllPackIds()
		{
			var dictionary = new Dictionary<Guid, string>();
			try
			{
				var filter = Builders<Pack>.Filter.Empty;
				var projection = Builders<Pack>.Projection.Include(p => p.Id).Include(p => p.Name);
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

		public async Task<List<Pack>> GetAllPacks()
		{
			try
			{
				var filter = Builders<Pack>.Filter.Empty;
				var documents = await _collection.Find(filter).ToListAsync();
				return documents;
			}
			catch (Exception)
			{
				return new List<Pack>(0);
			}
		}

		public async Task<string> GetPackNameById(Guid id)
		{
			var filter = Builders<Pack>.Filter.Eq(p => p.Id, id);
			var projection = Builders<Pack>.Projection.Include(p => p.Name);

			var document = await _collection.Find(filter).Project(projection).FirstOrDefaultAsync();
			return document["Name"].AsString;
		}

		public async Task<bool> UpdatePackById(Guid id, string? newName, Guid? newTexMap, Guid? newModelMap, Guid? newBlockStateMap, string? newDesc, List<Guid>? miscAssets, bool? emissives, string? emissiveSuffix, string? gitRepoUrl)
		{
			try
			{
				var filter = Builders<Pack>.Filter.Eq(p => p.Id, id);
				var updateDefinitions = new List<UpdateDefinition<Pack>>();

				if (!string.IsNullOrWhiteSpace(newName))
					updateDefinitions.Add(Builders<Pack>.Update.Set(p => p.Name, newName));

				if (newTexMap.HasValue)
					updateDefinitions.Add(Builders<Pack>.Update.Set(p => p.TextureMappingsId, newTexMap.Value));

				if (newModelMap.HasValue)
					updateDefinitions.Add(Builders<Pack>.Update.Set(p => p.ModelMappingsId, newModelMap.Value));

				if (newBlockStateMap.HasValue)
					updateDefinitions.Add(Builders<Pack>.Update.Set(p => p.BlockStateMappingsId, newBlockStateMap.Value));

				if (!string.IsNullOrWhiteSpace(newDesc))
					updateDefinitions.Add(Builders<Pack>.Update.Set(p => p.Description, newDesc));

				if (miscAssets != null)
					updateDefinitions.Add(Builders<Pack>.Update.Set(p => p.MiscAssetIds, miscAssets));

				if (emissives != null)
					updateDefinitions.Add(Builders<Pack>.Update.Set(p => p.EnableEmissives, emissives));

				if (!string.IsNullOrWhiteSpace(emissiveSuffix))
					updateDefinitions.Add(Builders<Pack>.Update.Set(p => p.EmissiveSuffix, emissiveSuffix));

				if (!string.IsNullOrWhiteSpace(gitRepoUrl))
					updateDefinitions.Add(Builders<Pack>.Update.Set(p => p.GitRepoUrl, gitRepoUrl));

				var update = Builders<Pack>.Update.Combine(updateDefinitions);
				var updated = await _collection.UpdateOneAsync(filter, update);
				return updated.IsModifiedCountAvailable;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public async Task<bool> AddBranch(Guid id, PackBranch branch)
		{
			try
			{
				var filter = Builders<Pack>.Filter.Eq(p => p.Id, id);
				var update = Builders<Pack>.Update.Push(p => p.Branches, branch);
				var updated = await _collection.UpdateOneAsync(filter, update);
				return updated.IsModifiedCountAvailable;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public async Task<bool> EditBranch(Guid packId, Guid branchId, PackBranch newBranch)
		{
			try
			{
				Pack? pack = await GetPackById(packId);
				if (pack == null)
					return false;

				List<PackBranch> branches = pack.Branches;

				int index = -1;
				for (int i = 0; i < branches.Count; i++)
				{
					if (branches[i].Id == branchId)
					{
						index = i;
						break;
					}
				}

				if (index == -1)
					return false;

				branches.RemoveAt(index);
				branches.Insert(index, newBranch);

				var filter = Builders<Pack>.Filter.Eq(p => p.Id, packId);
				var update = Builders<Pack>.Update.Set(p => p.Branches, branches);
				var updated = await _collection.UpdateOneAsync(filter, update);
				return updated.IsModifiedCountAvailable;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public async Task<bool> DeleteById(Guid id)
		{
			try
			{
				var filter = Builders<Pack>.Filter.Eq(p => p.Id, id);
				var deleted = await _collection.DeleteOneAsync(filter);
				return deleted.DeletedCount > 0;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public async Task<bool> DeleteBranch(Guid packId, Guid branchId)
		{
			try
			{
				var filter = Builders<Pack>.Filter.Eq(p => p.Id, packId);
				var update = Builders<Pack>.Update.PullFilter(p => p.Branches, b => b.Id == branchId);
				var updated = await _collection.UpdateOneAsync(filter, update);
				return updated.IsModifiedCountAvailable;
			}
			catch (Exception)
			{
				return false;
			}
		}
	}

	public interface IPackRepository
	{
		// Create
		Task<bool> AddPack(Pack pack);

		// Read
		Task<Pack?> GetPackById(Guid id);
		Task<Pack?> GetPackByName(string name);
		Task<Dictionary<Guid, string>> GetAllPackIds();
		Task<List<Pack>> GetAllPacks();
		Task<string> GetPackNameById(Guid id);

		// Update
		Task<bool> UpdatePackById(Guid id, string? newName, Guid? newTexMap, Guid? newModelMap, Guid? newBlockStateMap, string? newDesc, List<Guid>? miscAssets, bool? emissives, string? emissiveSuffix, string? gitRepoUrl);
		Task<bool> AddBranch(Guid id, PackBranch branch);
		Task<bool> EditBranch(Guid packId, Guid branchId, PackBranch newBranch);

		// Delete
		Task<bool> DeleteById(Guid id);
		Task<bool> DeleteBranch(Guid packId, Guid branchId);
	}
}
