using System.Linq;

namespace Uno.UI.RemoteControl.HotReload.Messages;

internal partial record HotReloadStatusMessage
{
	public HotReloadStatusMessage(Uno.HotReload.Tracking.HotReloadStatusInfo status)
		: this(
			(HotReloadState)status.State,
			[.. status.Operations.Select(op => new HotReloadServerOperationData(op))],
			status.ServerError) { }
}

public partial record HotReloadServerOperationData
{
	public HotReloadServerOperationData(Uno.HotReload.Tracking.HotReloadOperationInfo op)
		: this(op.Id, op.StartTime, op.FilePaths, op.IgnoredFilePaths, op.EndTime, (HotReloadServerResult?)op.Result, op.Diagnostics) { }
}
