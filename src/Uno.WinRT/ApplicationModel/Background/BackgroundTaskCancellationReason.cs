namespace Windows.ApplicationModel.Background;

public enum BackgroundTaskCancellationReason
{
	Abort = 0,
	Terminating = 1,
	LoggingOff = 2,
	ServicingUpdate = 3,
	IdleTask = 4,
	Uninstall = 5,
	ConditionLoss = 6,
	SystemPolicy = 7,
	QuietHoursEntered = 8,
	ExecutionTimeExceeded = 9,
	ResourceRevocation = 10,
	EnergySaver = 11
}
