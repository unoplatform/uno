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

using Microsoft.UI.Dispatching;

namespace Uno.UI.Xaml.Controls;

internal abstract class NativeWindowWrapperBase : INativeWindowWrapper
{
	public virtual bool IsClosingCancellable => NativeWindowFactory.SupportsClosingCancellation;

	public const int InitialWidth = 1024;
	public const int InitialHeight = 640;

	protected readonly ContentSite _contentSite = new();
	private Rect _bounds;
	private Rect _visibleBounds;
	private bool _visible;
	private PointInt32 _position;
	private string _title = "";
	private CoreWindowActivationState _activationState;
	private XamlRoot? _xamlRoot;
	private protected Window? _window;
	private float _rasterizationScale;
	private StagedChanges _staged;
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
			StageBounds(value);
			PublishChanges();
		}
	}

	public Rect VisibleBounds
	{
		get => _visibleBounds;
		set
		{
			StageVisibleBounds(value);
			PublishChanges();
		}
	}

	/// <summary>
	/// The same as setting <see cref="VisibleBounds"/> and <see cref="Bounds"/>, but the events are raised only once both
	/// are updated.
	/// </summary>
	public void SetBoundsAndVisibleBounds(Rect bounds, Rect visibleBounds)
	{
		StageBounds(bounds);
		StageVisibleBounds(visibleBounds);
		PublishChanges();
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
			StageRasterizationScale(value);
			PublishChanges();
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

	public SizeInt32 Size { get; private set; }

	public SizeInt32 ClientSize { get; private set; }

	protected void SetSizes(SizeInt32 size, SizeInt32 clientSize)
	{
		StageSizes(size, clientSize);
		PublishChanges();
	}

	// A native change that moves several values (a configure, WM_SIZE, a DPI change) stages them all, then publishes
	// once, so every observer sees the complete new state.
	protected void StageBounds(Rect bounds)
	{
		if (_bounds != bounds)
		{
			_bounds = bounds;
			_staged |= StagedChanges.Bounds;
		}
	}

	protected void StageVisibleBounds(Rect visibleBounds)
	{
		if (_visibleBounds != visibleBounds)
		{
			_visibleBounds = visibleBounds;
			_staged |= StagedChanges.VisibleBounds;
		}
	}

	protected void StageSizes(SizeInt32 size, SizeInt32 clientSize)
	{
		if (!Size.Equals(size) || !ClientSize.Equals(clientSize))
		{
			Size = size;
			ClientSize = clientSize;
			_staged |= StagedChanges.Size;
		}
	}

	protected void StageRasterizationScale(float scale)
	{
		if (_rasterizationScale != scale)
		{
			_rasterizationScale = scale;
			_contentSite.ParentScale = scale;
			_staged |= StagedChanges.Scale;
		}
	}

	protected void StagePresenterChange() => _staged |= StagedChanges.Presenter;

	/// <summary>
	/// Raises the events of the staged changes in a fixed order: scale, presenter, size, visible bounds, bounds.
	/// </summary>
	protected void PublishChanges()
	{
		var staged = _staged;
		_staged = StagedChanges.None;
		if ((staged & StagedChanges.Scale) != 0)
		{
			RaiseContentIslandStateChanged(ContentIslandStateChangedEventArgs.RasterizationScaleChange);
		}
		if ((staged & StagedChanges.Presenter) != 0)
		{
			_window?.AppWindow.OnAppWindowChanged(AppWindowChangedEventArgs.PresenterChangedEventArgs);
		}
		if ((staged & StagedChanges.Size) != 0)
		{
			_window?.AppWindow.OnAppWindowChanged(new AppWindowChangedEventArgs() { DidSizeChange = true });
		}
		if ((staged & StagedChanges.VisibleBounds) != 0)
		{
			VisibleBoundsChanged?.Invoke(this, _visibleBounds);
		}
		if ((staged & StagedChanges.Bounds) != 0)
		{
			SizeChanged?.Invoke(this, _bounds.Size);
			RaiseContentIslandStateChanged(ContentIslandStateChangedEventArgs.ActualSizeChange);
		}
	}

	[Flags]
	private enum StagedChanges
	{
		None = 0,
		Scale = 1,
		Presenter = 2,
		Size = 4,
		VisibleBounds = 8,
		Bounds = 16,
	}

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

	/// <summary>
	/// True when the platform draws its own caption buttons over extended content (e.g. the macOS traffic lights),
	/// so WindowChrome must not draw the XAML ones and the host reports the AppWindowTitleBar insets itself.
	/// </summary>
	internal virtual bool HasNativeCaptionButtons => false;

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

	public virtual PointInt32 ConvertLocalToScreen(Point localPoint)
	{
		var scale = RasterizationScale == 0 ? 1 : RasterizationScale;
		return new PointInt32(
			Position.X + (int)Math.Round(localPoint.X * scale),
			Position.Y + (int)Math.Round(localPoint.Y * scale));
	}

	public virtual Point ConvertScreenToLocal(PointInt32 screenPoint)
	{
		var scale = RasterizationScale == 0 ? 1 : RasterizationScale;
		return new Point(
			(screenPoint.X - Position.X) / scale,
			(screenPoint.Y - Position.Y) / scale);
	}

	public virtual bool TryConvertLocalToScreen(Point localPoint, out PointInt32 screenPoint)
	{
		screenPoint = ConvertLocalToScreen(localPoint);
		return true;
	}

	public virtual bool TryConvertScreenToLocal(PointInt32 screenPoint, out Point localPoint)
	{
		localPoint = ConvertScreenToLocal(screenPoint);
		return true;
	}

	public void Destroy() { }

	public void Hide() => IsVisible = false;

	public virtual void SetIcon(string iconPath)
	{
	}

	public virtual void SetSystemBackdrop(Microsoft.UI.Xaml.Media.SystemBackdrop? backdrop)
	{
	}

	// Heads that don't override SetSystemBackdrop render no material, so they must not report support:
	// the window would go transparent over nothing.
	public virtual bool IsSystemBackdropSupported(Microsoft.UI.Xaml.Media.SystemBackdrop backdrop) => false;
}
