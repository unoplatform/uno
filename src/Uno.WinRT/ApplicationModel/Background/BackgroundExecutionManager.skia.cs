#nullable enable

using System;
using System.Threading.Tasks;
using Uno.Extensions;
using Windows.Foundation;

namespace Windows.ApplicationModel.Background;

public static partial class BackgroundExecutionManager
{
	public static IAsyncOperation<BackgroundAccessStatus> RequestAccessAsync()
		=> Task.FromResult(GetAccessStatus()).AsAsyncOperation();

	public static IAsyncOperation<BackgroundAccessStatus> RequestAccessAsync(
		string applicationId)
	{
		ArgumentNullException.ThrowIfNull(applicationId);
		return RequestAccessAsync();
	}

	public static void RemoveAccess()
	{
	}

	public static void RemoveAccess(string applicationId)
	{
		ArgumentNullException.ThrowIfNull(applicationId);
		RemoveAccess();
	}

	public static BackgroundAccessStatus GetAccessStatus()
		=> BackgroundTaskScheduler.TryGetExtension(out _)
			? BackgroundAccessStatus.AllowedSubjectToSystemPolicy
			: BackgroundAccessStatus.DeniedBySystemPolicy;

	public static BackgroundAccessStatus GetAccessStatus(string applicationId)
	{
		ArgumentNullException.ThrowIfNull(applicationId);
		return GetAccessStatus();
	}

	public static IAsyncOperation<bool> RequestAccessKindAsync(
		BackgroundAccessRequestKind requestedAccess,
		string reason)
	{
		ArgumentNullException.ThrowIfNull(reason);
		return Task
			.FromResult(
				GetAccessStatus() ==
				BackgroundAccessStatus.AllowedSubjectToSystemPolicy)
			.AsAsyncOperation();
	}
}
