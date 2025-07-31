#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using Windows.UI;
using Microsoft.UI.Xaml;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.ViewManagement;
using Uno.Disposables;
using Windows.Graphics.Display;
using UIKit;

namespace Uno.UI.Controls
{
	public partial class NativeCommandBarPresenter : ContentPresenter
	{
		private readonly SerialDisposable _statusBarSubscription = new SerialDisposable();
		private readonly SerialDisposable _orientationSubscription = new SerialDisposable();
		private WeakReference<CommandBar?>? _commandBar;

		private readonly bool _isPhone = UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Phone;

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			// TODO: Find a proper way to decide whether a CommandBar exists on canvas (within Page), or is mapped to the UINavigationController's NavigationBar.

			CommandBar? commandBar = null;
			_commandBar?.TryGetTarget(out commandBar);

			if (_commandBar == null)
			{
				commandBar = GetTemplatedParent() as CommandBar;
				_commandBar = new WeakReference<CommandBar?>(commandBar);
			}

			if ((commandBar?.TryGetNative<CommandBar, CommandBarRenderer, UINavigationBar>(out var nativeBar) ?? false)
				&& nativeBar is { })
			{
				LayoutNativeBar(nativeBar);
			}
			else if (commandBar is { } && FeatureConfiguration.CommandBar.AllowNativePresenterContent)
			{
				var commandBarRenderer = new CommandBarRenderer(commandBar);
				commandBar.SetRenderer(commandBarRenderer);


				var commandBarItemRenderer = new CommandBarNavigationItemRenderer(commandBar);
				commandBar.SetRenderer(commandBarItemRenderer);

				var navigationItem = new UINavigationItem();
				var navBar = new UINavigationBar();
				navBar.PushNavigationItem(navigationItem, false);

				commandBarItemRenderer.Native = navigationItem;
				commandBarRenderer.Native = navBar;

				LayoutNativeBar(navBar);
			}
		}

		private void LayoutNativeBar(UINavigationBar nativeBar)
		{
			nativeBar.SetNeedsLayout();

			var navigationBarSuperview = nativeBar.Superview;

			// Allows the UINavigationController's NavigationBar instance to be moved to the Page. This feature
			// is used in the context of the sample application to test NavigationBars outside of a NativeFramePresenter for 
			// UI Testing. In general cases, this should not happen as the bar may be moved back to to this presenter while
			// another page is already visible, making this bar overlay on top of another.			
			if (FeatureConfiguration.CommandBar.AllowNativePresenterContent && (navigationBarSuperview == null || navigationBarSuperview is NativeCommandBarPresenter))
			{
				Content = nativeBar;
			}

			var statusBar = StatusBar.GetForCurrentView();

			statusBar.Showing += OnStatusBarChanged;
			statusBar.Hiding += OnStatusBarChanged;

			_statusBarSubscription.Disposable = Disposable.Create(() =>
			{
				statusBar.Showing -= OnStatusBarChanged;
				statusBar.Hiding -= OnStatusBarChanged;
			});

			// iOS doesn't automatically update the navigation bar position when the status bar visibility changes.
			void OnStatusBarChanged(StatusBar sender, object args)
			{
				nativeBar.SetNeedsLayout();
				nativeBar.Superview?.SetNeedsLayout();
			}
		}

#if !__TVOS__
		protected override Size MeasureOverride(Size size)
		{
			var measuredSize = base.MeasureOverride(size);

			if (_isPhone)
			{
				switch (UIDevice.CurrentDevice.Orientation)
				{
					// On iPhone, the OS may wrongly report the height of the UINavigationBar:
					//   - After the creation when it's in landscape mode, it's wrongly
					//     returning the portrait height as measured height (the width is correct)
					//   - After the rotation from landscape to portrait, the next measured
					//     height will be the landscape one. Width is also correct.
					//
					// This is a ugly hack to circumvent this annoying OS bug.
					//
					// NOTE: those values are been fixed for all iOS versions. Tested on iOS 14.
					case UIDeviceOrientation.LandscapeLeft:
					case UIDeviceOrientation.LandscapeRight:
						measuredSize = new Size(measuredSize.Width, 32);
						break;
					case UIDeviceOrientation.Portrait:
					case UIDeviceOrientation.PortraitUpsideDown:
						measuredSize = new Size(measuredSize.Width, 44);
						break;
				}
			}

			return measuredSize;
		}
#endif

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			_statusBarSubscription.Disposable = null;
			_orientationSubscription.Disposable = null;
		}
	}
}
