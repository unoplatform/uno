#nullable enable

using System;

namespace Windows.ApplicationModel.Background;

public partial class BackgroundTaskProgressEventArgs
{
	internal BackgroundTaskProgressEventArgs(Guid instanceId, uint progress)
	{
		InstanceId = instanceId;
		Progress = progress;
	}

	public Guid InstanceId { get; }

	public uint Progress { get; }
}
