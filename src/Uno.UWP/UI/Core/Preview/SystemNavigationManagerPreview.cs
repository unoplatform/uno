#if __MACOS__
using System;
using AppKit;

namespace Windows.UI.Core.Preview
{
	public partial class SystemNavigationManagerPreview
	{
		private static readonly SystemNavigationManagerPreview _instance = new SystemNavigationManagerPreview();

		private SystemNavigationManagerPreview()
		{
		}

		internal bool HasConfirmedClose { get; private set; }

		public static SystemNavigationManagerPreview GetForCurrentView() => _instance;

		public event EventHandler<SystemNavigationCloseRequestedPreviewEventArgs> CloseRequested;

		internal void OnCloseRequested()
		{			
			var eventArgs = new SystemNavigationCloseRequestedPreviewEventArgs(OnCloseRequestedDeferralComplete);
			CloseRequested?.Invoke(null, eventArgs);
			eventArgs.EventRaiseCompleted();

			if (!eventArgs.IsDeferred)
			{
				HasConfirmedClose = !eventArgs.Handled;
			}
		}

		private void OnCloseRequestedDeferralComplete(SystemNavigationCloseRequestedPreviewEventArgs args)
		{
			if (!args.Handled)
			{
				// Set flag and close the application's window again.
				HasConfirmedClose = true;
				NSApplication.SharedApplication.KeyWindow.PerformClose(null);
			}
		}
	}
}
#endif
