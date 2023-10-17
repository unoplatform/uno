#nullable enable

using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Microsoft.UI;
using Uno.Foundation.Extensibility;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

partial class NativeWindowFactory
{
	internal const string WindowIdExtraName = "Uno.UI.WindowId";

	private readonly static Dictionary<WindowId, NativeWindowWrapper> _awaitingAssociation = new();

	private static Lazy<INativeWindowFactoryExtension?> _nativeWindowFactory = new(() =>
	{
		if (!ApiExtensibility.CreateInstance<INativeWindowFactoryExtension>(typeof(DesktopWindow), out var factory))
		{
			return null;
		}

		return factory;
	});

	internal static void AssociateActivity(WindowId windowId, Activity activity)
	{
		foreach (var window in ApplicationHelper.Windows)
		{
			if (window.AppWindow.Id == windowId &&
				window.NativeWindowWrapper is NativeWindowWrapper wrapper)
			{
				wrapper.SetActivity(activity);
			}
		}
	}

	public static bool SupportsMultipleWindows => true;

	private static INativeWindowWrapper? CreateWindowPlatform(Windows.UI.Xaml.Window window, XamlRoot xamlRoot)
	{
		if (window != Window.CurrentSafe)
		{
			var application = ContextHelper.Application;
			if (application is null)
			{
				throw new InvalidOperationException("Application is not running.");
			}

			var packageManager = application.PackageManager;
			if (packageManager is null)
			{
				throw new InvalidOperationException("PackageManager is not available.");
			}

			var intent = packageManager.GetLaunchIntentForPackage(application.PackageName!)!;
			intent.AddFlags(ActivityFlags.NewTask);
			intent.AddFlags(ActivityFlags.MultipleTask);
			if (OperatingSystem.IsAndroidVersionAtLeast(24))
			{
				intent.AddFlags(ActivityFlags.LaunchAdjacent);
			}

			var windowId = window.AppWindow.Id;
			intent.PutExtra(WindowIdExtraName, (long)windowId.Value);

			application.StartActivity(intent);

			var nativeWindowWrapper = new NativeWindowWrapper(window, xamlRoot);
			return nativeWindowWrapper;
		}
		else
		{
			return NativeWindowWrapper.Instance;
		}
	}
}
