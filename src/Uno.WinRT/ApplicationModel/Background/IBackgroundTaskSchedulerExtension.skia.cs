#nullable enable

using System;
using System.Collections.Generic;

namespace Windows.ApplicationModel.Background;

internal interface IBackgroundTaskSchedulerExtension
{
	bool IsSupported { get; }

	void Reconcile();

	void Register(BackgroundTaskRegistrationRecord registration);

	void Unregister(BackgroundTaskRegistrationRecord registration, bool cancelTask);

	void CompleteOneShot(BackgroundTaskRegistrationRecord registration);
}

internal sealed class BackgroundTaskRegistrationRecord
{
	internal required Guid TaskId { get; init; }

	internal required string Name { get; init; }

	internal required string TaskEntryPoint { get; init; }

	internal required TimeTrigger Trigger { get; init; }

	internal bool CancelOnConditionLoss { get; init; }

	internal bool IsNetworkRequested { get; init; }

	internal string? GroupId { get; init; }

	internal string? GroupName { get; init; }

	internal required string ExecutablePath { get; init; }

	internal required IReadOnlyList<string> ExecutableArguments { get; init; }

	internal required string WorkingDirectory { get; init; }
}
