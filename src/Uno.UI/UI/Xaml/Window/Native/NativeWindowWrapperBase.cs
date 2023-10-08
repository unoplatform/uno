#nullable enable

using System;
using Microsoft.UI.Windowing;
using Windows.Foundation;
using Windows.UI.Core;

namespace Uno.UI.Xaml.Controls;

internal abstract class NativeWindowWrapperBase : INativeWindowWrapper
{
	private Rect _bounds;
	private Rect _visibleBounds;
	private bool _visible;
	private CoreWindowActivationState _activationState;

	public Rect Bounds
	{
		get => _bounds;
		set
		{
			if (_bounds != value)
			{
				_bounds = value;
				SizeChanged?.Invoke(this, value.Size);
			}
		}
	}

	public Rect VisibleBounds
	{
		get => _visibleBounds;
		set
		{
			if (_visibleBounds != value)
			{
				_visibleBounds = value;
				VisibleBoundsChanged?.Invoke(this, value);
			}
		}
	}

	public CoreWindowActivationState ActivationState
	{
		get => _activationState;
		set
		{
			if (_activationState != value)
			{
				_activationState = value;
				ActivationChanged?.Invoke(this, value);
			}
		}
	}

	public bool Visible
	{
		get => _visible;
		set
		{
			if (_visible != value)
			{
				_visible = value;
				VisibilityChanged?.Invoke(this, value);
			}
		}
	}

	public event EventHandler<Size>? SizeChanged;
	public event EventHandler<Rect>? VisibleBoundsChanged;
	public event EventHandler<CoreWindowActivationState>? ActivationChanged;
	public event EventHandler<bool>? VisibilityChanged;
	public event EventHandler<AppWindowClosingEventArgs>? Closing;
	public event EventHandler? Closed;
	public event EventHandler? Shown;

	public abstract void Activate();

	public abstract void Close();

	public virtual void Show()
	{
		ShowCore();
		Shown?.Invoke(this, EventArgs.Empty);
	}

	protected abstract void ShowCore();

	protected AppWindowClosingEventArgs RaiseClosing()
	{
		var args = new AppWindowClosingEventArgs();
		Closing?.Invoke(this, args);
		return args;
	}

	protected void RaiseClosed() => Closed?.Invoke(this, EventArgs.Empty);
}
