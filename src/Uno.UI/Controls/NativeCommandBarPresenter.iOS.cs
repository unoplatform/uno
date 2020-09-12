using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.UI;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Controls;
using Windows.UI;
using Windows.UI.Xaml;
using System.IO;
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

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			// TODO: Find a proper way to decide whether a CommandBar exists on canvas (within Page), or is mapped to the UINavigationController's NavigationBar.

			var commandBar = TemplatedParent as CommandBar;
			var navigationBar = commandBar?.GetRenderer(() => new CommandBarRenderer(commandBar)).Native;
			if (navigationBar.Superview == null) // Prevents the UINavigationController's NavigationBar instance from being moved to the Page
			{
				Content = navigationBar;
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
				navigationBar.SetNeedsLayout();
				navigationBar.Superview.SetNeedsLayout();
			}
		}

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			_statusBarSubscription.Disposable = null;
			_orientationSubscription.Disposable = null;
		}
	}
}
