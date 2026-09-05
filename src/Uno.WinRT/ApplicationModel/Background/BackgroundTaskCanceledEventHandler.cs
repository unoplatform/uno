#nullable enable

namespace Windows.ApplicationModel.Background;

public delegate void BackgroundTaskCanceledEventHandler(
	IBackgroundTaskInstance sender,
	BackgroundTaskCancellationReason reason);
