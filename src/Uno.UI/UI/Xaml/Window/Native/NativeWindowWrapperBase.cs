#nullable enable

using System;
using Microsoft.UI.Content;
using Microsoft.UI.Windowing;
using Windows.UI.Xaml;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.UI.Core;

namespace Uno.UI.Xaml.Controls;

internal abstract class NativeWindowWrapperBase : INativeWindowWrapper
{
	public const int InitialWidth = 1024;
	public const int InitialHeight = 640;

	protected readonly ContentSite _contentSite = new();
	private Rect _bounds;
	private Rect _visibleBounds;
	private bool _visible;
	private string _title = "";
	private CoreWindowActivationState _activationState;
	private XamlRoot? _xamlRoot;
	private float _rasterizationScale;
	private readonly SerialDisposable _presenterSubscription = new SerialDisposable();

	protected NativeWindowWrapperBase(XamlRoot xamlRoot) : this()
	{
		SetXamlRoot(xamlRoot);
	}

	protected NativeWindowWrapperBase()
	{
	}

	public ContentSiteView ContentSiteView => _contentSite.View;

	protected XamlRoot? XamlRoot => _xamlRoot;

	internal void SetXamlRoot(XamlRoot xamlRoot) => _xamlRoot = xamlRoot;

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

				RaiseContentIslandStateChanged(ContentIslandStateChangedEventArgs.ActualSizeChange);
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

				_contentSite.IsSiteVisible = value;
				RaiseContentIslandStateChanged(ContentIslandStateChangedEventArgs.SiteVisibleChange);
			}
		}
	}

	public float RasterizationScale
	{
		get => _rasterizationScale;
		set
		{
			if (_rasterizationScale != value)
			{
				_rasterizationScale = value;

				_contentSite.ParentScale = value;
				RaiseContentIslandStateChanged(ContentIslandStateChangedEventArgs.RasterizationScaleChange);
			}
		}
	}

	public virtual string Title
	{
		get => _title;
		set
		{
			_title = value;
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning($"Setting the title of the window is not supported on this platform yet");
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

	public virtual void Activate() { }

	public virtual void Close() { }

	public virtual void ExtendContentIntoTitleBar(bool extend) { }

	public virtual void Show()
	{
		ShowCore();
		// On single-window targets, the window is already shown with splash screen
		// so we must ensure the property is initialized correctly.
		Visible = true;
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
		_presenterSubscription.Disposable?.Dispose();
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

	private void RaiseContentIslandStateChanged(ContentIslandStateChangedEventArgs args)
	{
		XamlRoot?.VisualTree.ContentRoot.CompositionContent?.RaiseStateChanged(args);
	}
}
