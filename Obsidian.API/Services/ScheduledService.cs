using Obsidian.API.Extensions;
using Obsidian.API.Logic;
using Obsidian.API.Repository;
using Obsidian.SDK.Models;

namespace Obsidian.API.Services
{
	public class ScheduledService : BackgroundService
	{
		private readonly DateTime _scheduledTime;
		private readonly List<Func<Task>> _scheduledTasks = new();
		private readonly object _lock = new();

		private readonly IServiceScopeFactory _scopeFactory;

		public ScheduledService(IServiceScopeFactory scopeFactory)
		{
			_scopeFactory = scopeFactory;
			_scheduledTime = DateTime.Today.AddDays(1);
		}

		public void AddScheduledTask(Func<Task> task)
		{
			lock (_lock)
			{
				_scheduledTasks.Add(task);
			}
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
			=> await ScheduleTask(stoppingToken);

		private async Task ScheduleTask(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				DateTime currentTime = DateTime.Now;
				TimeSpan timeUntilScheduledTime = _scheduledTime - currentTime;

				if (timeUntilScheduledTime < TimeSpan.Zero)
					timeUntilScheduledTime = TimeSpan.Zero;

				Console.WriteLine($"Schedule will run at {_scheduledTime:hh\\:mm tt} (UTC). This is in {timeUntilScheduledTime.Hours}h {timeUntilScheduledTime.Minutes}m {timeUntilScheduledTime.Seconds}s.");
				await Task.Delay(timeUntilScheduledTime, stoppingToken);

				// Call your desired method here
				await ExecuteScheduledTasks();

				// Schedule the task for the next day at the same time
				DateTime nextExecutionTime = DateTime.Today.AddDays(1).Add(_scheduledTime.TimeOfDay);
				TimeSpan delayUntilNextExecution = nextExecutionTime - DateTime.Now;
				await Task.Delay(delayUntilNextExecution, stoppingToken);
			}
		}

		private async Task ExecuteScheduledTasks()
		{
			Console.WriteLine("Executing scheduled tasks...");

			Func<Task>[] tasksToExecute;
			lock (_lock)
			{
				tasksToExecute = _scheduledTasks.ToArray();
				_scheduledTasks.Clear();
			}
			await tasksToExecute.Select(x => x()).WhenAllThrottledAsync(5);
			await CommitPacks();

			Console.WriteLine("Finished executing scheduled tasks.");
		}

		private async Task CommitPacks()
		{
			using IServiceScope scope = _scopeFactory.CreateScope();
			IPackRepository packRepository = scope.ServiceProvider.GetRequiredService<IPackRepository>();
			IContinuousPackLogic continuousPackLogic = scope.ServiceProvider.GetRequiredService<IContinuousPackLogic>();

			List<Pack> packs = await packRepository.GetAllPacks();
			foreach (Pack pack in packs)
				continuousPackLogic.CommitPack(pack);
		}
	}

}
