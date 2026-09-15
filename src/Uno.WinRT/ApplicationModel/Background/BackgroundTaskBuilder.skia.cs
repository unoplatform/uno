#nullable enable

using System;
using System.Collections.Generic;

namespace Windows.ApplicationModel.Background;

public partial class BackgroundTaskBuilder
{
	private readonly List<IBackgroundCondition> _conditions = [];
	private IBackgroundTrigger? _trigger;
	private string _name = string.Empty;
	private string _taskEntryPoint = string.Empty;

	public bool CancelOnConditionLoss { get; set; }

	public bool IsNetworkRequested { get; set; }

	public string Name
	{
		get => _name;
		set => _name = value ?? throw new ArgumentNullException(nameof(value));
	}

	public string TaskEntryPoint
	{
		get => _taskEntryPoint;
		set => _taskEntryPoint = value ?? throw new ArgumentNullException(nameof(value));
	}

	public BackgroundTaskRegistrationGroup? TaskGroup { get; set; }

	public BackgroundTaskBuilder()
	{
	}

	public void SetTrigger(IBackgroundTrigger trigger)
		=> _trigger = trigger ?? throw new ArgumentNullException(nameof(trigger));

	public void AddCondition(IBackgroundCondition condition)
		=> _conditions.Add(condition ?? throw new ArgumentNullException(nameof(condition)));

	public BackgroundTaskRegistration Register()
	{
		if (string.IsNullOrWhiteSpace(Name))
		{
			throw new InvalidOperationException(
				"A background task name must be supplied before registration.");
		}

		if (string.IsNullOrWhiteSpace(TaskEntryPoint))
		{
			throw new PlatformNotSupportedException(
				"Skia background tasks require TaskEntryPoint to name an IBackgroundTask type.");
		}

		if (_trigger is null)
		{
			throw new InvalidOperationException(
				"A background task trigger must be set before registration.");
		}

		if (_trigger is not TimeTrigger timeTrigger)
		{
			throw new PlatformNotSupportedException(
				"Skia desktop currently supports TimeTrigger background tasks.");
		}

		if (timeTrigger.FreshnessTime < TimeTrigger.MinimumFreshnessTime)
		{
			throw new ArgumentOutOfRangeException(
				nameof(TimeTrigger.FreshnessTime),
				$"TimeTrigger.FreshnessTime must be at least {TimeTrigger.MinimumFreshnessTime} minutes.");
		}

		if (_conditions.Count != 0)
		{
			throw new PlatformNotSupportedException(
				"Background task conditions are not supported on Skia desktop targets.");
		}

		if (TaskGroup is not null)
		{
			throw new PlatformNotSupportedException(
				"Background task registration groups are not supported on Skia desktop targets.");
		}

		var taskId = Guid.NewGuid();
		var command = BackgroundTaskProcessCommand.Create(taskId);
		return BackgroundTaskRegistrationStore.Register(
			new BackgroundTaskRegistrationRecord
			{
				TaskId = taskId,
				Name = Name,
				TaskEntryPoint = TaskEntryPoint,
				Trigger = timeTrigger,
				CancelOnConditionLoss = CancelOnConditionLoss,
				IsNetworkRequested = IsNetworkRequested,
				GroupId = TaskGroup?.Id,
				GroupName = TaskGroup?.Name,
				ExecutablePath = command.ExecutablePath,
				ExecutableArguments = command.Arguments,
				WorkingDirectory = command.WorkingDirectory
			});
	}
}
