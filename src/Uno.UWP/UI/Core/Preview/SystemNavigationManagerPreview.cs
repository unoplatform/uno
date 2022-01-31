#if __MACOS__ || __SKIA__
using System;

namespace Windows.UI.Core.Preview;

public partial class SystemNavigationManagerPreview
{
	private static readonly SystemNavigationManagerPreview _instance = new();

	private SystemNavigationManagerPreview() => InitializePlatform();

	partial void InitializePlatform();

	public event EventHandler<SystemNavigationCloseRequestedPreviewEventArgs> CloseRequested;

	internal bool HasConfirmedClose { get; private set; }

	public static SystemNavigationManagerPreview GetForCurrentView() => _instance;

	internal bool RequestAppClose()
	{
		HasConfirmedClose = false;
		var eventArgs = new SystemNavigationCloseRequestedPreviewEventArgs(OnCloseRequestedDeferralComplete);
		CloseRequested?.Invoke(null, eventArgs);		
		var completedSynchronously = eventArgs.DeferralManager.EventRaiseCompleted();
		return completedSynchronously ? !eventArgs.Handled : false;
	}

	private void OnCloseRequestedDeferralComplete(SystemNavigationCloseRequestedPreviewEventArgs args)
	{
		if (!args.Handled)
		{
			// Set flag and close the application's window again.
			HasConfirmedClose = true;
			if (!args.DeferralManager.CompletedSynchronously)
			{
				// Initiate the app closing again.
				CloseApp();
			}
		}
	}

	partial void CloseApp();
}
#endif
