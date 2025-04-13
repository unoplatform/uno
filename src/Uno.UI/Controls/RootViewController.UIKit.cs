using System;
using Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using ObjCRuntime;
using UIKit;
using Uno.Helpers.Theming;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

namespace Uno.UI.Controls
{
	public partial class RootViewController : UINavigationController, IRotationAwareViewController
	{
		private XamlRoot _xamlRoot;

		internal event Action VisibleBoundsChanged;

		public RootViewController()
		{
			Initialize();
		}

		public RootViewController(UIViewController rootViewController)
			: base(rootViewController)
		{
			Initialize();
		}

		public RootViewController(NSObjectFlag t)
			: base(t)
		{
			Initialize();
		}

		public RootViewController(NSCoder coder)
			: base(coder)
		{
			Initialize();
		}

		public RootViewController(NativeHandle handle)
			: base(handle)
		{
			Initialize();
		}

		private void Initialize()
		{
			// TODO Uno: When we support multi-window, this should close popups for the appropriate XamlRoot #13847.

#if !__TVOS__
			// Dismiss on device rotation: this reproduces the windows behavior
			UIApplication.Notifications
				.ObserveDidChangeStatusBarOrientation(DismissPopups);
#endif

			// Dismiss when the app is entering background
			UIApplication.Notifications
				.ObserveWillResignActive(DismissPopups);

#if NET9_0_OR_GREATER
			// iOS 17+ only
			if (UIDevice.CurrentDevice.CheckSystemVersion(17, 0))
			{
				((IUITraitChangeObservable)this).RegisterForTraitChanges((env, traits) => SystemThemeHelper.RefreshSystemTheme(), typeof(UITraitUserInterfaceStyle));
			}
#endif
		}

		private void DismissPopups(object sender, object args)
		{
			if (_xamlRoot is not null)
			{
				VisualTreeHelper.CloseLightDismissPopups(_xamlRoot);
			}
		}

		internal void SetXamlRoot(XamlRoot xamlRoot) => _xamlRoot = xamlRoot;

		// This will handle when the status bar is showed / hidden by the system on iPhones
		public override void ViewSafeAreaInsetsDidChange()
		{
			base.ViewSafeAreaInsetsDidChange();
			VisibleBoundsChanged?.Invoke();
		}

		public bool CanAutorotate { get; set; } = true;

#pragma warning disable CA1422 // Deprecated in iOS 17+, replaced by RegisterForTraitChanges in Initialize()
		public override void TraitCollectionDidChange(UITraitCollection previousTraitCollection)
		{
			base.TraitCollectionDidChange(previousTraitCollection);
			SystemThemeHelper.RefreshSystemTheme();
		}
#pragma warning restore CA1422 // Deprecated in iOS 17+
	}
}
