using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Obsidian.SDK.Models.Tools;
using Octokit;
using System.IO.Compression;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Obsidian.API.Repository;
using Obsidian.SDK;
using Obsidian.SDK.Enums;
using FileMode = System.IO.FileMode;
using System;

namespace Obsidian.API.Logic
{
	public class ToolsLogic : IToolsLogic
	{
		private const int ASSET_VERSION = 2;
		private const string BEDROCK_ZIP_REGEX = "Mojang-bedrock-samples-[a-zA-Z0-9]+\\/resource_pack\\/";

		private readonly IVersionAssetsRepository _vaRepository;
		private readonly IGitHubClient _client;

		public ToolsLogic(IVersionAssetsRepository vaRepository, IGitHubClient githubClient)
		{
			_vaRepository = vaRepository;
			_client = githubClient;
		}

		private readonly DefaultContractResolver contractResolver = new DefaultContractResolver
		{
			NamingStrategy = new CamelCaseNamingStrategy()
		};

		public async Task<ResponseModel<MCAssets>> GetMinecraftJavaAssets(string version, bool bypassHighestVersionLimit = false)
		{
			List<AssetMCVersion> supportedVersions = await GetJavaMCVersions(bypassHighestVersionLimit);

			return await GetMinecraftAssets(version, MCEdition.Java, supportedVersions);
		}

		public async Task<ResponseModel<MCAssets>> GetMinecraftBedrockAssets(string version)
		{
			List<AssetMCVersion> supportedVersions = await GetBedrockMCVersions();

			return await GetMinecraftAssets(version, MCEdition.Bedrock, supportedVersions);
		}

		public async Task<ResponseModel<string>> GetMinecraftJavaJar(string version)
		{
			List<AssetMCVersion> supportedVersions = await GetJavaMCVersions();
			AssetMCVersion? mcVer = supportedVersions.FirstOrDefault(x => x.Id == version && x.Edition == "java");

			string jUrl = string.Empty;
			if (mcVer != null)
				jUrl = await GetJarDownload(mcVer);

			if (string.IsNullOrWhiteSpace(jUrl))
				return new ResponseModel<string>
				{
					IsSuccess = false,
					Message = "Invalid version"
				};

			return new ResponseModel<string>()
			{
				IsSuccess = true,
				Data = jUrl
			};
		}

		public async Task PurgeAssets()
		{
			List<MinecraftVersionAssets> allAssets = await _vaRepository.GetAllVersionAssets();

			Task<List<AssetMCVersion>> javaVersionsTask = GetJavaMCVersions();
			Task<List<AssetMCVersion>> bedrockVersionsTask = GetBedrockMCVersions();

			await Task.WhenAll(javaVersionsTask, bedrockVersionsTask);

			List<AssetMCVersion> allVersions = javaVersionsTask.Result.Concat(bedrockVersionsTask.Result).ToList();
			List<MinecraftVersionAssets> unsupportedAssets = allAssets
				.Where(x => !allVersions.Select(y => y.Id).Contains(x.Name) || x.Version != ASSET_VERSION).ToList();

			List<Task> removeTasks = unsupportedAssets.Select(asset => _vaRepository.DeleteVersionAssets(asset.Name, asset.Edition, asset.Version)).Cast<Task>().ToList();

			await Utils.ProcessTasksInBatches(removeTasks);
		}

		public async Task<bool> PregenetateJavaAssets(List<AssetMCVersion>? versions = null)
		{
			if (versions == null || versions.Count == 0)
				versions = await GetJavaMCVersions();

			foreach (AssetMCVersion ver in versions)
				await GetMinecraftAssets(ver.Id, MCEdition.Java, versions);

			return true;
		}

		public async Task<bool> PregenetateBedrockAssets(List<AssetMCVersion>? versions = null)
		{
			if (versions == null || versions.Count == 0)
				versions = await GetBedrockMCVersions();

			foreach (AssetMCVersion ver in versions)
				await GetMinecraftAssets(ver.Id, MCEdition.Bedrock, versions);

			return true;
		}

		public async Task<List<AssetMCVersion>> GetJavaMCVersions(bool bypassHighestVersionLimit = false)
		{
			try
			{
				HttpClient httpClient = new HttpClient();
				using var httpResponse = await httpClient.GetAsync("https://launchermeta.mojang.com/mc/game/version_manifest.json", HttpCompletionOption.ResponseHeadersRead);
				httpResponse.EnsureSuccessStatusCode();

				var rawJson = await httpResponse.Content.ReadAsStringAsync();
				var json = JObject.Parse(rawJson);

				JToken? latestReleaseT = json.SelectToken("$.latest.release");
				JToken? latestSnapshotT = json.SelectToken("$.latest.snapshot");

				string latestRelease = latestReleaseT != null ? latestReleaseT.ToString() : "";
				string latestSnapshot = latestSnapshotT != null ? latestSnapshotT.ToString() : "";

				List<AssetMCVersion> versions = new List<AssetMCVersion>();
				json.SelectTokens($"$.versions[?(@.type == 'release' || @.id == '{latestSnapshot}' || @.id == '{latestRelease}')]")
					.ToList().ForEach(x =>
					{
						var obj = x.ToObject<AssetMCVersion>();
						if (obj != null)
						{
							obj.Edition = "java";
							versions.Add(obj);
						}
					});
				return LimitVersions(versions, bypassHighestVersionLimit);
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
			}
		}

		public async Task<List<AssetMCVersion>> GetBedrockMCVersions()
		{
			try
			{
				List<AssetMCVersion> versions = new List<AssetMCVersion>();
				var allReleases = await _client.Repository.Release.GetAll("Mojang", "bedrock-samples");

				Release? preRelease = allReleases.OrderByDescending(x => x.CreatedAt).FirstOrDefault(x => x.Prerelease);
				if (preRelease != null)
				{
					versions.Add(new AssetMCVersion
					{
						Id = TrimBedrockReleaseId(preRelease.Name),
						ReleaseTime = preRelease.CreatedAt.UtcDateTime,
						Time = preRelease.CreatedAt.UtcDateTime,
						Type = "beta",
						Edition = "bedrock",
						Url = preRelease.ZipballUrl
					});
				}

				Release? release = allReleases.OrderByDescending(x => x.CreatedAt).FirstOrDefault(x => x.Prerelease == false);
				if (release != null)
				{
					versions.Add(new AssetMCVersion
					{
						Id = TrimBedrockReleaseId(release.Name),
						ReleaseTime = release.CreatedAt.UtcDateTime,
						Time = preRelease.CreatedAt.UtcDateTime,
						Type = "release",
						Edition = "bedrock",
						Url = release.ZipballUrl
					});
				}
				return versions;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				throw;
			}
		}

		private async Task<ResponseModel<MCAssets>> GetMinecraftAssets(string version, MCEdition edition, List<AssetMCVersion> supportedVersions)
		{
			string editionString = edition == MCEdition.Java ? "java" : "bedrock";
			AssetMCVersion? mcVer = supportedVersions.FirstOrDefault(x => x.Id == version && x.Edition == editionString);

			// Version is technically unsupported, but it still might exist in the db.
			// This is basically a client-side cache work-around (old entries will eventually be wiped from the db).
			if (mcVer == null)
			{
				MinecraftVersionAssets? existingAsset = await _vaRepository.GetVersionAssets(version, editionString, ASSET_VERSION);
				if (existingAsset != null)
				{
					MCAssets? data = JsonConvert.DeserializeObject<MCAssets>(existingAsset.JSON);
					if (data != null)
					{
						return new ResponseModel<MCAssets>()
						{
							IsSuccess = true,
							Data = data
						};
					}
				}
			}
			else
			{
				MinecraftVersionAssets? existingAsset = await _vaRepository.GetVersionAssets(version, editionString, ASSET_VERSION);

				if (existingAsset == null)
				{
					return new ResponseModel<MCAssets>()
					{
						IsSuccess = true,
						Data = await CreateMCVA(mcVer, edition)
					};
				}
				MCAssets? data = JsonConvert.DeserializeObject<MCAssets>(existingAsset.JSON);
				if (data != null)
				{
					return new ResponseModel<MCAssets>()
					{
						IsSuccess = true,
						Data = data
					};
				}
			}
			return new ResponseModel<MCAssets>()
			{
				IsSuccess = false,
				Message = "Could not get the assets for the specified version"
			};
		}

		private async Task<MCAssets> CreateMCVA(AssetMCVersion version, MCEdition edition)
		{
			MCAssets assets = await GenerateAssets(version, edition);
			MinecraftVersionAssets mcva = new MinecraftVersionAssets()
			{
				Name = assets.Name,
				Version = assets.Version,
				Edition = edition == MCEdition.Java ? "java" : "bedrock",
				JSON = JsonConvert.SerializeObject(assets, new JsonSerializerSettings
				{
					ContractResolver = contractResolver,
					Formatting = Formatting.None
				})
			};
			await _vaRepository.AddVersionAssets(mcva);
			return assets;
		}

		private string TrimBedrockReleaseId(string releaseName)
			=> releaseName.TrimStart('v').Replace("-preview", "");

		private List<AssetMCVersion> LimitVersions(List<AssetMCVersion> versions, bool bypassHighestPatchCheck = false)
		{
			List<AssetMCVersion> newList = new List<AssetMCVersion>();
			DateTime OldestDateTime = DateTime.Parse("2013-04-24T15:45:00+00:00");

			// Select all full-release versions >=1.5.2 (and the latest snapshot)
			List<AssetMCVersion> tempList = versions.Where(x => x.ReleaseTime > OldestDateTime)
				.OrderByDescending(x => x.ReleaseTime).ToList();

			if (bypassHighestPatchCheck)
			{
				newList = tempList;
			}
			else
			{
				// This logic is very confusing. Limit to the highest patch (MAJOR.MINOR.PATCH) for each MAJOR.MINOR
				// release (e.g. 1.8.9, 1.17.1), with the exception of the latest snapshot / pre-release / RC.
				tempList.ToList().ForEach(x =>
				{
					List<string> split = x.Id.Split('.').ToList();
					// Include latest snapshot if it is newer than the latest full-release
					if ((split.Count == 1 || x.Id.Any(char.IsLetter)) && x.ReleaseTime >= tempList[0].ReleaseTime)
						newList.Add(x);
					else
					{
						List<int> verSplit = split.Select(int.Parse).ToList();
						if (!newList.Any(y => y.Id.StartsWith($"{verSplit[0]}.{verSplit[1]}") && y.Type == "release"))
							newList.Add(x);
					}
				});
			}
			return newList;
		}

		private async Task<string> GetJarDownload(AssetMCVersion version)
		{
			if (version.Edition != "java")
				return string.Empty;

			string url = version.Url;
			HttpClient httpClient = new HttpClient();
			using var httpResponse = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
			httpResponse.EnsureSuccessStatusCode();

			var rawJson = await httpResponse.Content.ReadAsStringAsync();
			var json = JObject.Parse(rawJson);

			JToken? downloadUrl = json.SelectToken("$.downloads.client.url");
			string dUrl = downloadUrl != null ? downloadUrl.ToString() : "";
			return dUrl;
		}

		private async Task<bool> DownloadJar(AssetMCVersion version, string path)
		{
			try
			{
				string url = await GetJarDownload(version);
				if (string.IsNullOrWhiteSpace(url))
					return false;

				return await DownloadFile(url, path);
			}
			catch (Exception)
			{
				return false;
			}
		}

		private async Task<bool> DownloadFile(string url, string path)
		{
			try
			{
				HttpClient client = new HttpClient();
				client.DefaultRequestHeaders.Add("User-Agent", "mullak99s-Faithful");
				HttpResponseMessage response = await client.GetAsync(url);

				if (!response.IsSuccessStatusCode) return false;

				Stream stream = await response.Content.ReadAsStreamAsync();
				await using FileStream fileStream = new FileStream(path, FileMode.Create);
				await stream.CopyToAsync(fileStream);
				return true;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return false;
			}
		}

		private List<string> GetFileListFromJar(string jarPath)
		{
			List<string> files = new List<string>();
			using ZipArchive zip = ZipFile.OpenRead(jarPath);
			foreach (var file in zip.Entries)
			{
				if (file.FullName.EndsWith("png"))
					files.Add(file.FullName);
			}
			zip.Dispose();
			return files;
		}

		private List<string> GetFileListFromBedrockZip(string bedrockZip)
		{
			List<string> files = new List<string>();
			using ZipArchive zip = ZipFile.OpenRead(bedrockZip);
			foreach (var file in zip.Entries.Where(x => Regex.IsMatch(x.FullName, BEDROCK_ZIP_REGEX)))
			{
				string fileName = Regex.Replace(file.FullName, BEDROCK_ZIP_REGEX, string.Empty);
				if (fileName.EndsWith("png") || fileName.EndsWith("tga"))
					files.Add(fileName);
			}
			zip.Dispose();
			return files;
		}

		private async Task<MCAssets> GenerateAssets(AssetMCVersion version, MCEdition edition)
		{
			MCAssets mcAssets = new MCAssets();

			Guid downloadFolder = Guid.NewGuid();
			string assetGenPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AssetGeneration");
			string path = Path.Combine(assetGenPath, downloadFolder.ToString());
			Directory.CreateDirectory(path);
			string file = Path.Combine(path, "temp.zip");

			if (edition == MCEdition.Java ? await DownloadJar(version, file) : await DownloadFile(version.Url, file))
			{
				List<string> textures = edition == MCEdition.Java ? GetFileListFromJar(file) : GetFileListFromBedrockZip(file);

				if (textures is { Count: > 0 })
				{
					mcAssets = new MCAssets()
					{
						Name = version.Id,
						Version = ASSET_VERSION,
						CreatedDate = DateTime.UtcNow,
						Minecraft = new MinecraftRelease()
						{
							Version = version.Id,
							Type = version.Type,
							Edition = edition == MCEdition.Java ? "java" : "bedrock",
							ReleaseTime = version.ReleaseTime
						},
						Textures = textures
					};
				}
			}
			else
			{
				// Logging needed
			}

			if (Directory.Exists(path))
				Directory.Delete(path, true);

			return mcAssets;
		}
	}

	public interface IToolsLogic
	{
		Task<List<AssetMCVersion>> GetJavaMCVersions(bool bypassHighestVersionLimit = false);
		Task<List<AssetMCVersion>> GetBedrockMCVersions();
		Task<ResponseModel<MCAssets>> GetMinecraftJavaAssets(string version, bool bypassHighestVersionLimit = false);
		Task<ResponseModel<MCAssets>> GetMinecraftBedrockAssets(string version);
		Task<bool> PregenetateJavaAssets(List<AssetMCVersion>? versions = null);
		Task<bool> PregenetateBedrockAssets(List<AssetMCVersion>? versions = null);
		Task<ResponseModel<string>> GetMinecraftJavaJar(string version);
		Task PurgeAssets();
	}
}
