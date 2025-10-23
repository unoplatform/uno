#nullable enable

using System;
using Microsoft.UI.Content;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Uno.Disposables;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI.Core;
using Windows.UI.ViewManagement;

#if HAS_UNO_WINUI
using Microsoft.UI.Dispatching;
#else
using Windows.System;
#endif

namespace Uno.UI.Xaml.Controls;

internal abstract class NativeWindowWrapperBase : INativeWindowWrapper
{
	public const int InitialWidth = 1024;
	public const int InitialHeight = 640;

	protected readonly ContentSite _contentSite = new();
	private Rect _bounds;
	private Rect _visibleBounds;
	private bool _visible;
	private PointInt32 _position;
	private SizeInt32 _size;
	private string _title = "";
	private CoreWindowActivationState _activationState;
	private XamlRoot? _xamlRoot;
	private protected Window? _window;
	private float _rasterizationScale;
	private readonly SerialDisposable _presenterSubscription = new SerialDisposable();

	protected NativeWindowWrapperBase(Window window, XamlRoot xamlRoot) : this()
	{
		SetWindow(window, xamlRoot);
	}

	protected NativeWindowWrapperBase()
	{
	}

	public ContentSiteView ContentSiteView => _contentSite.View;

	internal protected XamlRoot? XamlRoot => _xamlRoot;

	internal protected Window? Window => _window;

	internal bool AssociatedWithManagedWindow => _window != null && _xamlRoot != null;

	public bool WasShown { get; set; }

	internal void SetWindow(Window window, XamlRoot xamlRoot)
	{
		_window = window;
		_xamlRoot = xamlRoot;
	}

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

	/// <summary>
	/// The same as setting <see cref="VisibleBounds"/>, <see cref="Bounds"/> and <see cref="Size"/> but makes sure the
	/// fired events are fired only after both properties are updated "atomically"
	/// </summary>
	public void SetBoundsAndVisibleBounds(Rect bounds, Rect visibleBounds)
	{
		if (_visibleBounds != visibleBounds)
		{
			_visibleBounds = visibleBounds;
			VisibleBoundsChanged?.Invoke(this, visibleBounds);
		}

		if (_bounds != bounds)
		{
			_bounds = bounds;
			SizeChanged?.Invoke(this, bounds.Size);
			RaiseContentIslandStateChanged(ContentIslandStateChangedEventArgs.ActualSizeChange);
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

	public bool IsVisible
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

	public PointInt32 Position
	{
		get => _position;
		set
		{
			if (!_position.Equals(value))
			{
				_position = value;
				_window?.AppWindow.OnAppWindowChanged(new AppWindowChangedEventArgs() { DidPositionChange = true });
			}
		}
	}

	public SizeInt32 Size
	{
		get => _size;
		set
		{
			if (!_size.Equals(value))
			{
				_size = value;
				_window?.AppWindow.OnAppWindowChanged(new AppWindowChangedEventArgs() { DidSizeChange = true });
			}
		}
	}

	public SizeInt32 ClientSize => throw new NotImplementedException();

	public DispatcherQueue DispatcherQueue => throw new NotImplementedException();

	public event EventHandler<Size>? SizeChanged;
	public event EventHandler<Rect>? VisibleBoundsChanged;
	public event EventHandler<CoreWindowActivationState>? ActivationChanged;
	public event EventHandler<bool>? VisibilityChanged;
	public event EventHandler<AppWindowClosingEventArgs>? Closing;
	public event EventHandler? Shown;

	internal protected virtual void Activate()
	{
	}

	/// <summary>
	/// Request the close of the native window
	/// </summary>
	protected virtual void CloseCore()
	{
	}

	public void Close()
	{
		CloseCore();

		IsVisible = false;
	}

	public virtual void ExtendContentIntoTitleBar(bool extend) { }

	public virtual void Show(bool activateWindow)
	{
		if (!WasShown)
		{
			WasShown = true;
			ShowCore();

			// On single-window targets, the window is already shown with splash screen
			// so we must ensure the property is initialized correctly.
			IsVisible = true;
			Shown?.Invoke(this, EventArgs.Empty);
		}

		if (activateWindow)
		{
			Activate();
		}
	}

	protected virtual void ShowCore() { }

	protected AppWindowClosingEventArgs RaiseClosing()
	{
		var args = new AppWindowClosingEventArgs();
		Closing?.Invoke(this, args);
		return args;
	}

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
	public virtual void Move(PointInt32 position)
	{
	}

	public virtual void Resize(SizeInt32 size)
	{
	}

	public void Destroy() { }

	public void Hide() => IsVisible = false;

#if __APPLE_UIKIT__
	public abstract Size GetWindowSize();
#endif
}
