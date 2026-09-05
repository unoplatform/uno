#nullable enable

using System;

namespace Windows.ApplicationModel.Background;

public partial interface IBackgroundTaskInstance
{
	Guid InstanceId { get; }

	uint Progress { get; set; }

	uint SuspendedCount { get; }

	BackgroundTaskRegistration Task { get; }

	object TriggerDetails { get; }

	BackgroundTaskDeferral GetDeferral();

	event BackgroundTaskCanceledEventHandler Canceled;
}
