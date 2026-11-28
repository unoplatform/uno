#nullable enable

using System;
using System.Threading.Tasks;
using Foundation;
using UIKit;
using Uno.Foundation.Logging;
using Uno.Storage.Internal;
using Windows.Storage;

namespace Windows.System
{
	public static partial class Launcher
	{
#if __IOS__
		private static LaunchFileInteraction? _activeLaunchFileInteraction;

		internal static Task<bool> LaunchFilePlatformAsync(IStorageFile file)
		{
			var presenter = UIApplication.SharedApplication.KeyWindow?.RootViewController;
			while (presenter?.PresentedViewController is { } presented)
			{
				presenter = presented;
			}

			if (presenter?.View is not { } presenterView)
			{
				if (typeof(Launcher).Log().IsEnabled(LogLevel.Error))
				{
					typeof(Launcher).Log().Error($"{nameof(LaunchFileAsync)} needs a visible window to present the file from.");
				}

				return Task.FromResult(false);
			}

			var url = file is StorageFile { Implementation: StorageFile.SecurityScopedFile scopedFile }
				? scopedFile.NsUrl
				: NSUrl.FromFilename(file.Path);

			_activeLaunchFileInteraction?.Dispose();
			var interaction = new LaunchFileInteraction(url, presenter);
			_activeLaunchFileInteraction = interaction;

			// Files QuickLook can't preview fall back to the list of apps that can open them.
			if (interaction.Controller.PresentPreview(true)
				|| interaction.Controller.PresentOpenInMenu(presenterView.Bounds, presenterView, true))
			{
				return Task.FromResult(true);
			}

			interaction.Dispose();
			return Task.FromResult(false);
		}

		private sealed class LaunchFileInteraction : UIDocumentInteractionControllerDelegate
		{
			private readonly UIViewController _presenter;
			private readonly IDisposable _securityScope;
			private bool _isSendingToApplication;

			public LaunchFileInteraction(NSUrl url, UIViewController presenter)
			{
				_presenter = presenter;
				_securityScope = url.BeginSecurityScopedAccess();
				Controller = UIDocumentInteractionController.FromUrl(url);
				Controller.Delegate = this;
			}

			public UIDocumentInteractionController Controller { get; }

			public override UIViewController ViewControllerForPreview(UIDocumentInteractionController controller) => _presenter;

			public override void DidEndPreview(UIDocumentInteractionController controller) => Dispose();

			public override void WillBeginSendingToApplication(UIDocumentInteractionController controller, string? application) =>
				_isSendingToApplication = true;

			public override void DidEndSendingToApplication(UIDocumentInteractionController controller, string? application) => Dispose();

			public override void DidDismissOpenInMenu(UIDocumentInteractionController controller)
			{
				if (!_isSendingToApplication)
				{
					Dispose();
				}
			}

			protected override void Dispose(bool disposing)
			{
				if (disposing)
				{
					_securityScope.Dispose();
					Controller.WeakDelegate = null;

					if (ReferenceEquals(_activeLaunchFileInteraction, this))
					{
						_activeLaunchFileInteraction = null;
					}
				}

				base.Dispose(disposing);
			}
		}
#else
		internal static Task<bool> LaunchFilePlatformAsync(IStorageFile file)
		{
			// tvOS has no document interaction or file-sharing UI to hand a file to another app.
			if (typeof(Launcher).Log().IsEnabled(LogLevel.Warning))
			{
				typeof(Launcher).Log().Warn($"{nameof(LaunchFileAsync)} is not supported on tvOS.");
			}

			return Task.FromResult(false);
		}
#endif
	}
}
