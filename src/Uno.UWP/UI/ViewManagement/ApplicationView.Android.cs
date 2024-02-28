using System;
using System.Runtime.CompilerServices;
using Android.App;
using Android.Views;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.Core;

namespace Windows.UI.ViewManagement;

partial class ApplicationView
{
	partial void InitializePlatform()
	{
		TryInitializeSpanningRectsExtension();
	}

	public bool IsScreenCaptureEnabled
	{
		get
		{
			var activity = GetCurrentActivity();
			return !activity.Window.Attributes.Flags.HasFlag(WindowManagerFlags.Secure);
		}
		set
		{
			var activity = GetCurrentActivity();
			if (value)
			{
				activity.Window.ClearFlags(WindowManagerFlags.Secure);
			}
			else
			{
				activity.Window.SetFlags(WindowManagerFlags.Secure, WindowManagerFlags.Secure);
			}
		}
	}

	public string Title
	{
		get
		{
			var activity = GetCurrentActivity();
			return activity.Title;
		}
		set
		{
			var activity = GetCurrentActivity();
			activity.Title = value;
		}
	}

	private Rect _trueVisibleBounds;

	internal void SetTrueVisibleBounds(Rect trueVisibleBounds) => _trueVisibleBounds = trueVisibleBounds;

	private Activity GetCurrentActivity([CallerMemberName] string propertyName = null)
	{
		if (!(ContextHelper.Current is Activity activity))
		{
			throw new InvalidOperationException($"{propertyName} API must be called when Activity is created");
		}

		return activity;
	}
}
