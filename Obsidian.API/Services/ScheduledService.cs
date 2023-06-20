using Obsidian.API.Extensions;
using Obsidian.API.Logic;
using Obsidian.API.Repository;
using Obsidian.SDK.Models;

namespace Obsidian.API.Services
{
	public class ScheduledService : BackgroundService
	{
		private readonly TimeSpan _scheduledTime = new(0, 0, 0);
		private readonly List<Func<Task>> _scheduledTasks = new();
		private readonly object _lock = new();

		private readonly IPackRepository _packRepository;
		private readonly IContinuousPackLogic _continuousPackLogic;

		public ScheduledService(IPackRepository packRepository, IContinuousPackLogic continuousPackLogic)
		{
			_packRepository = packRepository;
			_continuousPackLogic = continuousPackLogic;
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
				TimeSpan currentTime = DateTime.Now.TimeOfDay;
				TimeSpan timeUntilScheduledTime = _scheduledTime - currentTime;

				if (timeUntilScheduledTime < TimeSpan.Zero)
					timeUntilScheduledTime = TimeSpan.Zero;

				await Task.Delay(timeUntilScheduledTime, stoppingToken);

				// Call your desired method here
				await ExecuteScheduledTasks();

				// Schedule the task for the next day at the same time
				DateTime nextExecutionTime = DateTime.Today.AddDays(1).Add(_scheduledTime);
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
			List<Pack> packs = await _packRepository.GetAllPacks();
			foreach (Pack pack in packs)
				_continuousPackLogic.CommitPack(pack);
		}
	}
}
