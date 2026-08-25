#nullable enable

using System;

namespace Windows.ApplicationModel.Background;

public partial class BackgroundTaskCompletedEventArgs
{
	private readonly string? _errorMessage;

	internal BackgroundTaskCompletedEventArgs(Guid instanceId, string? errorMessage)
	{
		InstanceId = instanceId;
		_errorMessage = errorMessage;
	}

	public Guid InstanceId { get; }

	public void CheckResult()
	{
		if (_errorMessage is not null)
		{
			throw new InvalidOperationException(_errorMessage);
		}
	}
}
