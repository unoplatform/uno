#nullable enable

using System;
using System.Collections.Generic;
using Gtk;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Core.Preview;
using Windows.UI.Xaml;
using WinUIApplication = Windows.UI.Xaml.Application;

namespace Uno.UI.Runtime.Skia.Gtk.UI.Controls;

internal class GtkWindowWrapper : NativeWindowWrapperBase
{
	private bool _wasShown;
	private readonly UnoGtkWindow _gtkWindow;
	private List<PendingWindowStateChangedInfo>? _pendingWindowStateChanged = new();

	public GtkWindowWrapper(UnoGtkWindow gtkWindow)
	{
		_gtkWindow = gtkWindow ?? throw new ArgumentNullException(nameof(gtkWindow));
		_gtkWindow.Shown += OnWindowShown;
		_gtkWindow.Host.SizeChanged += OnHostSizeChanged;
		_gtkWindow.DeleteEvent += OnWindowClosing;
		_gtkWindow.Destroyed += OnWindowClosed;
		_gtkWindow.WindowStateEvent += OnWindowStateChanged;
	}

	/// <summary>
	/// GTK overrides show as the initialization is asynchronous.
	/// </summary>
	public override async void Show()
	{
		try
		{
			await _gtkWindow.Host.InitializeAsync();
			base.Show();
		}
		catch (Exception ex)
		{
			this.Log().Error("Failed to initialize the UnoGtkWindow", ex);
		}
	}

	protected override void ShowCore() => _gtkWindow.ShowAll();

	private void OnWindowShown(object? sender, EventArgs e)
	{
		_wasShown = true;
		ReplayPendingWindowStateChanges();
	}

	public UnoGtkWindow NativeWindow => _gtkWindow;

	public override void Activate() => _gtkWindow.Activate();

	private void OnWindowClosed(object? sender, EventArgs e) => RaiseClosed();

	private void OnWindowClosing(object sender, DeleteEventArgs args)
	{
		var manager = SystemNavigationManagerPreview.GetForCurrentView();
		if (!manager.HasConfirmedClose)
		{
			if (!manager.RequestAppClose())
			{
				// App closing was prevented, handle event
				args.RetVal = true;
				return;
			}
		}

		// Closing should continue, perform suspension.
		WinUIApplication.Current.RaiseSuspending();

		// All prerequisites passed, can safely close.
		args.RetVal = false;
		Main.Quit();
	}

	private void OnHostSizeChanged(object? sender, Windows.Foundation.Size size) => Bounds = new Rect(default, size);

	private void OnWindowStateChanged(object o, WindowStateEventArgs args)
	{
		var newState = args.Event.NewWindowState;
		var changedMask = args.Event.ChangedMask;

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"OnWindowStateChanged: {newState}/{changedMask}");
		}

		if (_wasShown)
		{
			ProcessWindowStateChanged(newState, changedMask);
		}
		else
		{
			// Store state changes to replay once the application has been
			// initalized completely (initialization can be delayed if the render
			// surface is automatically detected).
			_pendingWindowStateChanged?.Add(new(newState, changedMask));
		}
	}

	private void ReplayPendingWindowStateChanges()
	{
		if (_pendingWindowStateChanged is not null)
		{
			foreach (var state in _pendingWindowStateChanged)
			{
				ProcessWindowStateChanged(state.newState, state.changedMask);
			}

			_pendingWindowStateChanged = null;
		}
	}

	private void ProcessWindowStateChanged(Gdk.WindowState newState, Gdk.WindowState changedMask)
	{
		var winUIApplication = WinUIApplication.Current;

		var isVisible =
			!(newState.HasFlag(Gdk.WindowState.Withdrawn) ||
			newState.HasFlag(Gdk.WindowState.Iconified));

		var isVisibleChanged =
			changedMask.HasFlag(Gdk.WindowState.Withdrawn) ||
			changedMask.HasFlag(Gdk.WindowState.Iconified);

		var focused = newState.HasFlag(Gdk.WindowState.Focused);
		var focusChanged = changedMask.HasFlag(Gdk.WindowState.Focused);

		if (!focused && focusChanged)
		{
			ActivationState = CoreWindowActivationState.Deactivated;
		}

		if (isVisibleChanged)
		{
			if (isVisible)
			{
				winUIApplication?.RaiseLeavingBackground(() => Visible = _gtkWindow.IsVisible);
			}
			else
			{
				Visible = _gtkWindow.IsVisible;
				winUIApplication?.RaiseEnteredBackground(null);
			}
		}

		if (focused && focusChanged)
		{
			ActivationState = Windows.UI.Core.CoreWindowActivationState.CodeActivated;
		}
	}
}
