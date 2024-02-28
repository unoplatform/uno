#nullable enable

using System;
using Microsoft.UI.Windowing;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Microsoft.UI.Windowing.Native;
using Windows.UI.Core;

namespace Uno.UI.Xaml.Controls;

internal abstract class NativeWindowWrapperBase : INativeWindowWrapper
{
	private Rect _bounds;
	private Rect _visibleBounds;
	private bool _visible;
	private CoreWindowActivationState _activationState;
	private readonly SerialDisposable _presenterSubscription = new SerialDisposable();

	public abstract object? NativeWindow { get; }

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

	public abstract string Title { get; set; }

	public event EventHandler<Size>? SizeChanged;
	public event EventHandler<Rect>? VisibleBoundsChanged;
	public event EventHandler<CoreWindowActivationState>? ActivationChanged;
	public event EventHandler<bool>? VisibilityChanged;
	public event EventHandler<AppWindowClosingEventArgs>? Closing;
	public event EventHandler? Closed;
	public event EventHandler? Shown;

	public virtual void Activate() { }

	public virtual void Close() { }

	public virtual void Show()
	{
		ShowCore();
		Shown?.Invoke(this, EventArgs.Empty);
	}

	protected virtual void ShowCore() { }

	protected AppWindowClosingEventArgs RaiseClosing()
	{
		var args = new AppWindowClosingEventArgs();
		Closing?.Invoke(this, args);
		return args;
	}

	protected void RaiseClosed() => Closed?.Invoke(this, EventArgs.Empty);

	public void SetPresenter(AppWindowPresenter presenter)
	{
		switch (presenter)
		{
			case FullScreenPresenter _:
				_presenterSubscription.Disposable = ApplyFullScreenPresenter();
				break;
			case OverlappedPresenter overlapped:
				_presenterSubscription.Disposable = ApplyOverlappedPresenter(overlapped);
				break;
			default:
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().LogWarning($"AppWindow presenter type {presenter.GetType()} is not supported yet");
				}
				break;
		}
	}

	protected virtual IDisposable ApplyFullScreenPresenter() => Disposable.Empty;

	protected virtual IDisposable ApplyOverlappedPresenter(OverlappedPresenter presenter) => Disposable.Empty;
}
