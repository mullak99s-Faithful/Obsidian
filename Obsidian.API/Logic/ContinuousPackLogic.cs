using LibGit2Sharp;
using Obsidian.API.Git;
using Obsidian.API.Repository;
using Obsidian.SDK.Extensions;
using Obsidian.SDK.Models;
using Obsidian.SDK.Models.Assets;
using Obsidian.SDK.Models.Mappings;
using Obsidian.SDK.Models.Tools;
using System.Security.Cryptography;
using System.Text;

namespace Obsidian.API.Logic
{
	public class ContinuousPackLogic : IContinuousPackLogic
	{
		private readonly string _rootPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Continuous");
		private readonly PushOptions _pushOptions;

		private readonly ITextureBucket _textureBucket;
		private readonly ITextureMapRepository _textureMapRepository;
		private readonly IPackRepository _packRepository;
		private readonly IMiscBucket _miscBucket;
		private readonly IPackPngLogic _packPngLogic;
		private readonly IToolsLogic _toolsLogic;
		private readonly IPackValidationLogic _packValidationLogic;
		private readonly IGitOptions _gitOptions;

		public ContinuousPackLogic(ITextureBucket textureBucket, ITextureMapRepository textureMapRepository, IPackRepository packRepository, IMiscBucket miscBucket, IPackPngLogic packPngLogic, IToolsLogic toolsLogic, IPackValidationLogic packValidationLogic, IGitOptions gitOptions)
		{
			_textureBucket = textureBucket;
			_textureMapRepository = textureMapRepository;
			_packRepository = packRepository;
			_miscBucket = miscBucket;
			_packPngLogic = packPngLogic;
			_toolsLogic = toolsLogic;
			_packValidationLogic = packValidationLogic;
			_gitOptions = gitOptions;

			_pushOptions = new()
			{
				CredentialsProvider = (_, _, _) => new UsernamePasswordCredentials
				{
					Username = _gitOptions.PersonalAccessToken,
					Password = string.Empty
				}
			};
		}

		public void AddPack(Pack pack)
		{
			string packPath = GetPackPath(pack);
			if (!Directory.Exists(packPath))
			{
				Console.WriteLine($"[ContinuousPackLogic] Created pack {pack.Name} ({pack.Id})");
				Directory.CreateDirectory(packPath);
			}
		}

		public void DeletePack(Pack pack)
		{
			string packPath = GetPackPath(pack);
			if (Directory.Exists(packPath))
			{
				Console.WriteLine($"[ContinuousPackLogic] Deleted pack {pack.Name} ({pack.Id})");
				Task.Run(() => Utils.FastDeleteAll(packPath));
			}
		}

		public void AddBranch(Pack pack, PackBranch branch)
		{
			string branchPath = GetBranchPath(pack, branch);
			if (!Directory.Exists(branchPath))
			{
				Console.WriteLine($"[ContinuousPackLogic] Created branch {branch.Name} ({branch.Id}) for {pack.Name} ({pack.Id})");
				Directory.CreateDirectory(branchPath);

				_ = GetBranch(pack, branch);
			}
		}

		public void DeleteBranch(Pack pack, PackBranch branch)
		{
			string branchPath = GetBranchPath(pack, branch);
			if (!Directory.Exists(branchPath))
			{
				Console.WriteLine($"[ContinuousPackLogic] Deleted branch {branch.Name} ({branch.Id}) for {pack.Name} ({pack.Id})");
				Task.Run(() => Utils.FastDeleteAll(branchPath));
			}
		}

		public async Task AddTexture(Pack pack, TextureAsset texture)
		{
			List<PackBranch> branches = pack.Branches;
			byte[]? textureBytes = await _textureBucket.DownloadTexture(pack.Id, texture.Id);
			byte[]? mcMetaBytes = null;
			if (textureBytes == null)
				return;

			List<string> texDestinationPaths = new();
			List<string> mcMetaDestinationPaths = new();

			foreach (PackBranch branch in branches)
			{
				string branchPath = GetBranchPath(pack, branch);
				foreach (var tex in texture.TexturePaths.Where(x => x.MCVersion.IsMatchingVersion(branch.Version)))
				{
					texDestinationPaths.Add(Path.Combine(branchPath, tex.Path));
					if (tex.MCMeta)
					{
						mcMetaBytes ??= await _textureBucket.DownloadMCMeta(pack.Id, texture.Id);
						mcMetaDestinationPaths.Add(Path.Combine(branchPath, tex.MCMetaPath));
					}
				}
			}

			List<Task> ioTasks = new();
			foreach (string destinationPath in texDestinationPaths)
			{
				string? dir = Path.GetDirectoryName(destinationPath);
				if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
					Directory.CreateDirectory(dir);

				ioTasks.Add(WriteChangedFileBytes(destinationPath, textureBytes));
			}

			if (mcMetaBytes != null && mcMetaDestinationPaths.Count > 0)
			{
				foreach (string destinationPath in mcMetaDestinationPaths)
				{
					string? dir = Path.GetDirectoryName(destinationPath);
					if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
						Directory.CreateDirectory(dir);

					ioTasks.Add(WriteChangedFileBytes(destinationPath, mcMetaBytes));
				}
			}
			await Task.WhenAll(ioTasks);
		}

		public async Task AddModel(Pack pack, ModelAsset asset, List<TextureAsset>? textureAssets = null)
		{
			if (textureAssets == null)
			{
				TextureMapping? textureMapping = await _textureMapRepository.GetTextureMappingById(pack.Id);
				if (textureMapping == null)
					return;

				textureAssets = textureMapping.Assets;
			}

			List<Task> ioTasks = new();
			foreach (PackBranch branch in pack.Branches)
			{
				if (!asset.MCVersion.IsMatchingVersion(branch.Version))
				{
					// Skip asset since its not for this branch version
					continue;
				}
				if (asset.Model == null)
				{
					Console.WriteLine($"[ContinuousPackLogic] Broken model asset! {asset.Path}\\{asset.FileName}");
					continue;
				}

				string modelJson = asset.Serialize(textureAssets, branch.Version);
				if (!string.IsNullOrWhiteSpace(modelJson))
				{
					string dirPath = Path.Combine(GetMinecraftDirectory(pack, branch), "models", asset.Path);
					Directory.CreateDirectory(dirPath);

					string filePath = Path.Combine(dirPath, asset.FileName);
					ioTasks.Add(File.WriteAllTextAsync(filePath, modelJson, Encoding.UTF8));
				}
			}
			await Task.WhenAll(ioTasks);
		}

		public async Task AddBlockState(Pack pack, BlockState blockState)
		{
			if (blockState.Data.Length > 0)
			{
				List<Task> ioTasks = new();
				foreach (var dirPath in pack.Branches.Where(x => blockState.MCVersion.IsMatchingVersion(x.Version))
					         .Select(branch => Path.Combine(GetMinecraftDirectory(pack, branch), "blockstates")))
				{
					Directory.CreateDirectory(dirPath);

					string filePath = Path.Combine(dirPath, blockState.FileName);
					ioTasks.Add(File.WriteAllBytesAsync(filePath, blockState.Data));
				}
				await Task.WhenAll(ioTasks);
			}
		}

		public async Task AddMisc(Pack pack, PackBranch? branch = null)
		{
			List<Task<MiscAsset?>> downloadTasks = pack.MiscAssetIds.Select(id => _miscBucket.DownloadMisc(id)).ToList();
			await Task.WhenAll(downloadTasks);

			List<Task> ioTasks = new();
			List<PackBranch> branches = new();

			if (branch == null)
				branches.AddRange(pack.Branches);
			else
				branches.Add(branch);

			foreach (PackBranch br in branches)
			{
				ioTasks.AddRange(downloadTasks.Select(task => task.Result)
					.Where(x => x != null && x.MCVersion.IsMatchingVersion(br.Version))
					.Select(asset => asset!.Extract(GetBranchPath(pack, br))));
			}
			await Task.WhenAll(ioTasks);
		}

		public void PurgeBranches(Pack pack)
			=> Parallel.ForEach(pack.Branches, branch => PurgeBranch(pack, branch));

		public void PurgeBranch(Pack pack, PackBranch branch, string? subFolder = null)
		{
			string path = subFolder == null ? Path.Combine(GetBranchPath(pack, branch), "assets") : Path.Combine(GetBranchPath(pack, branch), subFolder);
			if (Directory.Exists(path))
				Utils.FastDeleteAll(path);
		}

		public async Task PackAutomation(Pack pack)
		{
			List<Task> automationTasks = pack.Branches.Select(branch => PackBranchAutomation(pack, branch)).ToList();
			await Task.WhenAll(automationTasks);
		}

		public async Task PackBranchAutomation(Pack pack, PackBranch branch)
		{
			Console.WriteLine($"Running automation for {pack.Name} - {branch.Name}");

			List<Task> automationTasks = new();

			string destination = GetBranchPath(pack, branch);
			automationTasks.Add(File.WriteAllTextAsync(Path.Combine(destination, "pack.mcmeta"), pack.CreatePackMCMeta(branch), Encoding.UTF8)); // pack.mcmeta

			// pack.png
			byte[]? packPng = await _packPngLogic.DownloadPackPng(pack.Id);
			if (packPng is { Length: > 0 })
				automationTasks.Add(File.WriteAllBytesAsync(Path.Combine(destination, "pack.png"), packPng));

			// Optifine
			string optifineDirectory = GetOptifineDirectory(pack, branch);
			Directory.CreateDirectory(optifineDirectory);

			// Meta
			string readMeText = $"# {pack.Name}\n\n**Minecraft Version:** _{branch.Version.GetEnumDescription()}_";
			automationTasks.Add(File.WriteAllTextAsync(Path.Combine(destination, "README.md"), readMeText));

			if (pack.EnableEmissives)
				automationTasks.Add(File.WriteAllTextAsync(Path.Combine(optifineDirectory, "emissive.properties"), $"suffix.emissive={pack.EmissiveSuffix}"));

			await Task.WhenAll(automationTasks);
		}

		public void DeletePackMCMeta(Pack pack, PackBranch branch)
		{
			string packMetaPath = Path.Combine(GetBranchPath(pack, branch), "pack.mcmeta");

			if (File.Exists(packMetaPath))
				File.Delete(packMetaPath);
		}

		public void DeletePackPng(Pack pack, PackBranch branch)
		{
			string packPngPath = Path.Combine(GetBranchPath(pack, branch), "pack.png");

			if (File.Exists(packPngPath))
				File.Delete(packPngPath);
		}

		public async Task PackValidation(Pack pack)
		{
			IEnumerable<Task> validationTasks = pack.Branches.Select(branch => BranchValidation(pack, branch));
			await Task.WhenAll(validationTasks);
		}

		public async Task BranchValidation(Pack pack, PackBranch branch)
		{
			var response = await _toolsLogic.GetMinecraftJavaAssets(branch.GetVersion(), true);

			if (!response.IsSuccess || response.Data == null)
			{
				Console.WriteLine($"Unable to run pack validation for {pack.Name} - {branch.Name}: {response.Message}");
				return;
			}

			Console.WriteLine($"Running pack validation for {pack.Name} - {branch.Name}");

			string branchPath = GetBranchPath(pack, branch);
			MCAssets assets = response.Data;
			string reportPath = Path.Combine(branchPath, $"Report-{pack.Name}-{branch.Name}.txt");

			PackReport packReport = await _packValidationLogic.CompareTextures(branchPath, assets.Textures);
			branch.Report = packReport;

			Console.WriteLine($"Missing textures for {pack.Name} - {branch.Name}: {packReport.MissingTotal}");

			Task editBranchTask = _packRepository.EditBranch(pack.Id, branch.Id, branch);
			Task generateReportTask = packReport.GenerateReport(reportPath);
			await Task.WhenAll(editBranchTask, generateReportTask);
		}

		private async Task WriteChangedFileBytes(string path, byte[] bytes)
		{
			if (File.Exists(path))
			{
				try
				{
					using SHA1 sha1 = SHA1.Create();
					await using var fileStream = File.OpenRead(path);
					byte[] existingFileHash = await sha1.ComputeHashAsync(fileStream);
					byte[] newFileHash = sha1.ComputeHash(bytes);

					// Don't replace files if they are the same
					if (existingFileHash.SequenceEqual(newFileHash))
						return;
				}
				catch (Exception e)
				{
					Console.WriteLine($"[ContinuousPackLogic] Error: {e}");
				}
			}
			await File.WriteAllBytesAsync(path, bytes);
		}

		private (LibGit2Sharp.Repository repo, Branch branch)? GetBranch(Pack pack, PackBranch branch)
		{
			if (pack.UseGit())
			{
				string repoUrl = pack.GitRepoUrl!;
				string gitBranchName = branch.Name; // TODO: Needs an alternate way to set this since branch names can be changed
				string localPath = GetBranchPath(pack, branch);

				string gitPath = !LibGit2Sharp.Repository.IsValid(localPath)
					? LibGit2Sharp.Repository.Init(localPath, localPath)
					: LibGit2Sharp.Repository.Discover(localPath);

				using LibGit2Sharp.Repository repo = new LibGit2Sharp.Repository(gitPath);

				try
				{
					// Add remote
					_ = repo.Network.Remotes["origin"] ?? repo.Network.Remotes.Add("origin", repoUrl);

					// Check if the branch already exists
					Branch? existingBranch = repo.Branches[gitBranchName];
					if (existingBranch != null)
						return (repo, existingBranch);
				}
				catch (Exception e)
				{
					Console.WriteLine($"[ContinuousPackLogic] An error occurred when getting the branch {pack.Name} - {branch.Name}: {e}");
				}

				try
				{
					// Initial commit
					Signature author = GetSignature();
					Commit commit = repo.Commit($"Create branch {gitBranchName}", author, author);

					// Create and checkout a new branch
					Branch? gitBranch = repo.CreateBranch(gitBranchName, commit); // TODO: Need a way of setting this in the pack settings, or get automatically
					if (gitBranch == null)
						return null;

					// Set the upstream branch
					repo.Branches.Update(gitBranch, b => b.UpstreamBranch = gitBranch.CanonicalName);

					Commands.Checkout(repo, gitBranch);

					// Push the commit to the remote repository
					repo.Network.Push(repo.Network.Remotes["origin"], $"refs/heads/{gitBranchName}:refs/heads/{gitBranchName}", _pushOptions);
					Console.WriteLine($"[ContinuousPackLogic] Pushed {pack.Name} - {branch.Name}: Created Branch!");

					return (repo, gitBranch);
				}
				catch (Exception e)
				{
					Console.WriteLine($"[ContinuousPackLogic] An error occurred when creating the branch {pack.Name} - {branch.Name}: {e}");
				}
			}
			return null;
		}

		public void CommitPack(Pack pack, string? overrideAutoMessage = null)
			=> pack.Branches.ForEach(branch => CommitBranch(pack, branch, overrideAutoMessage));

		public void CommitBranch(Pack pack, PackBranch branch, string? overrideAutoMessage = null)
		{
			(LibGit2Sharp.Repository getRepo, Branch getBranch)? repoAndBranch = GetBranch(pack, branch);
			if (!repoAndBranch.HasValue)
				return;

			try
			{
				// Need to get these again. Will cause a memory access violation otherwise
				string localPath = GetBranchPath(pack, branch);
				string gitPath = LibGit2Sharp.Repository.Discover(localPath);

				using LibGit2Sharp.Repository gitRepo = new LibGit2Sharp.Repository(gitPath);

				Branch? gitBranch = gitRepo.Branches[branch.Name]; // TODO: Needs an alternate way to set this since branch names can be changed
				if (gitBranch == null)
				{
					Console.WriteLine($"[ContinuousPackLogic] Cannot find {pack.Name} - {branch.Name}");
					return;
				}

				// TODO: Will likely need to pull the branch just in case

				// Stage all files in the repository
				Commands.Stage(gitRepo, "*");

				// Check if there are any changes
				if (gitRepo.RetrieveStatus().IsDirty)
				{
					Console.WriteLine($"[ContinuousPackLogic] Committing {pack.Name} - {branch.Name}");

					// Commit the changes
					string dateTime = DateTime.Now.ToUniversalTime().ToString("yyyyMMddHHmmss");
					string message = overrideAutoMessage ?? $"[Obsidian ({dateTime})] Auto-commit for {branch.Name}";

					Signature author = GetSignature();
					gitRepo.Commit(message, author, author);
				}
				else
					Console.WriteLine($"[ContinuousPackLogic] No changes for {pack.Name} - {branch.Name}: Skipping commit!");

				// Push the commit to the remote repository (inc. and missed pushes due to any errors)
				// TODO: Needs an alternate way to set this since branch names can be changed
				gitRepo.Network.Push(gitRepo.Network.Remotes["origin"], $"refs/heads/{branch.Name}:refs/heads/{branch.Name}", _pushOptions);
				Console.WriteLine($"[ContinuousPackLogic] Pushed {pack.Name} - {branch.Name}");
			}
			catch (LibGit2SharpException e)
			{
				Console.WriteLine(e.Message.Contains("The operation timed out")
					? $"[ContinuousPackLogic] Push operation timed out for {pack.Name} - {branch.Name}: {e}"
					: $"[ContinuousPackLogic] An error occurred when commiting {pack.Name} - {branch.Name}: {e}");
			}
			catch (Exception e)
			{
				Console.WriteLine($"[ContinuousPackLogic] An unknown error occurred when commiting {pack.Name} - {branch.Name}: {e}");
			}
		}

		private Signature GetSignature()
			=> new(_gitOptions.AuthorName, _gitOptions.AuthorEmail, DateTimeOffset.Now);

		private string GetPackPath(Pack pack)
			=> Path.Combine(_rootPath, pack.Name); // Can't use Guid since it can make the path too long

		private string GetBranchPath(Pack pack, PackBranch branch)
			=> Path.Combine(GetPackPath(pack), branch.Name); // Can't use Guid since it can make the path too long

		private string GetMinecraftDirectory(Pack pack, PackBranch branch)
			=> Path.Combine(GetBranchPath(pack, branch), "assets", "minecraft");

		private string GetOptifineDirectory(Pack pack, PackBranch branch)
			=> Path.Combine(GetMinecraftDirectory(pack, branch), (int)branch.Version < 13 ? "mcpatcher" : "optifine");
	}

	public interface IContinuousPackLogic
	{
		void AddPack(Pack pack);
		void DeletePack(Pack pack);
		void AddBranch(Pack pack, PackBranch branch);
		void DeleteBranch(Pack pack, PackBranch branch);
		Task AddTexture(Pack pack, TextureAsset texture);
		Task AddModel(Pack pack, ModelAsset asset, List<TextureAsset>? textureAssets = null);
		Task AddBlockState(Pack pack, BlockState blockState);
		Task AddMisc(Pack pack, PackBranch? branch = null);
		Task PackAutomation(Pack pack);
		Task PackBranchAutomation(Pack pack, PackBranch branch);
		void DeletePackMCMeta(Pack pack, PackBranch branch);
		void DeletePackPng(Pack pack, PackBranch branch);
		Task PackValidation(Pack pack);
		Task BranchValidation(Pack pack, PackBranch branch);
		void CommitPack(Pack pack, string? overrideAutoMessage = null);
		void CommitBranch(Pack pack, PackBranch branch, string? overrideAutoMessage = null);
		void PurgeBranches(Pack pack);
		void PurgeBranch(Pack pack, PackBranch branch, string? subFolder = null);
	}
}
