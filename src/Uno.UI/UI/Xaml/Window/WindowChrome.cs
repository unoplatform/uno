#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using DirectUI;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Disposables;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Media;
using Windows.Graphics;

namespace Uno.UI.Xaml.Controls;

internal sealed partial class WindowChrome : ContentControl
{
	private const string ToolTipRestore = "TEXT_TOOLTIP_RESTORE";
	private const string ToolTipMaximize = "TEXT_TOOLTIP_MAXIMIZE";
	private const string ToolTipMinimize = "TEXT_TOOLTIP_MINIMIZE";
	private const string ToolTipClose = "TEXT_TOOLTIP_CLOSE";

	private readonly SerialDisposable m_titleBarMinMaxCloseContainerLayoutUpdatedEventHandler = new();
	private readonly SerialDisposable m_closeButtonClickedEventHandler = new();
	private readonly SerialDisposable m_minimizeButtonClickedEventHandler = new();
	private readonly SerialDisposable m_maximizeButtonClickedEventHandler = new();
	private readonly SerialDisposable m_presenterDisposable = new();
	private SerialDisposable m_userTitleBarDisposable = new();
	private UIElement? m_userTitleBar;
	private readonly Window _window;

	private FrameworkElement? m_tpTitleBarMinMaxCloseContainerPart;
	private Button? m_tpCloseButtonPart;
	private Button? m_tpMinimizeButtonPart;
	private Button? m_tpMaximizeButtonPart;

	private Dictionary<NonClientRegionKind, RectInt32[]> _nonClientRegions = new Dictionary<NonClientRegionKind, RectInt32[]>();

	private InputNonClientPointerSource? m_inputNonClientPtrSrc;

	public WindowChrome(Microsoft.UI.Xaml.Window parent)
	{
		DefaultStyleKey = typeof(WindowChrome);

		HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
		VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch;
		HorizontalContentAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Stretch;
		VerticalContentAlignment = Microsoft.UI.Xaml.VerticalAlignment.Stretch;

		IsTabStop = false;
		parent.AppWindow.Changed += OnAppWindowChanged;
		parent.AppWindow.TitleBar.ExtendsContentIntoTitleBarChanged += OnExtendsContentIntoTitleBarChanged;
		parent.AppWindow.TitleBar.Changed += OnTitleBarChanged;
		parent.Activated += OnActivate;
		Loaded += OnLoaded;
		_window = parent;
	}

	private void OnTitleBarChanged(object? sender, EventArgs e)
	{
		ConfigureWindowChrome();
	}

	private void ResizeCaptionButtons()
	{
		var scale = _window.AppWindow.NativeAppWindow.RasterizationScale;
		var height = _window.AppWindow.TitleBar.Height / scale;
		if (height > 0)
		{
			if (m_tpMaximizeButtonPart is not null)
			{
				m_tpMaximizeButtonPart.Height = height;
			}

			if (m_tpMinimizeButtonPart is not null)
			{
				m_tpMinimizeButtonPart.Height = height;
			}

			if (m_tpCloseButtonPart is not null)
			{
				m_tpCloseButtonPart.Height = height;
			}
		}
	}

	private void OnActivate(object sender, WindowActivatedEventArgs e)
	{
		// TODO: Remove workaround for theme brushes and foreground reset when the underlying issue is resolved.  
		// See: https://github.com/unoplatform/uno/issues/21688
		DefaultBrushes.ResetDefaultThemeBrushes();
		this.SetValue(ForegroundProperty, DefaultBrushes.TextForegroundBrush, DependencyPropertyValuePrecedences.DefaultValue);
		if (e.WindowActivationState is Windows.UI.Core.CoreWindowActivationState.CodeActivated or Windows.UI.Core.CoreWindowActivationState.PointerActivated)
		{
			VisualStateManager.GoToState(this, "Normal", true);
		}
		else
		{
			VisualStateManager.GoToState(this, "WindowInactive", true);
		}
	}

	private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
	{
		if (args.DidPresenterChange)
		{
			m_presenterDisposable.Disposable = null;
			if (_window.AppWindow.Presenter is OverlappedPresenter overlappedPresenter)
			{
				UpdateContainerSize();
				ConfigureWindowChrome();

				void OnBorderAndTitleBarChanged(object? s, object? e)
				{
					ConfigureWindowChrome();
				}
				overlappedPresenter.BorderAndTitleBarChanged += OnBorderAndTitleBarChanged;
				m_presenterDisposable.Disposable = Disposable.Create(() =>
				{
					overlappedPresenter.BorderAndTitleBarChanged -= OnBorderAndTitleBarChanged;
				});
			}
		}

		UpdateAllNonClientRegions();
	}

	private void OnStateChanged(object? sender, EventArgs e) => UpdateContainerSize();

	// to apply min, max and close style definitions to custom titlebar,
	// one needs to apply Content Control style with key WindowChromeStyle defined in generic.xaml
	internal void ApplyStylingForMinMaxCloseButtons()
	{
		var style = (Style)Application.Current.Resources["WindowChromeStyle"];
		SetValue(StyleProperty, style);
	}

	protected override void OnApplyTemplate()
	{
		// detach event handlers
		m_titleBarMinMaxCloseContainerLayoutUpdatedEventHandler.Disposable = null;
		m_closeButtonClickedEventHandler.Disposable = null;
		m_minimizeButtonClickedEventHandler.Disposable = null;
		m_maximizeButtonClickedEventHandler.Disposable = null;

		base.OnApplyTemplate();

		// attach event handlers

		m_tpTitleBarMinMaxCloseContainerPart = GetTemplateChild("TitleBarMinMaxCloseContainer") as FrameworkElement;

		if (m_tpTitleBarMinMaxCloseContainerPart is not null)
		{
			var titleBarMinMaxCloseContainer = m_tpTitleBarMinMaxCloseContainerPart;

			void OnTitleBarMinMaxCloseLayoutUpdated(object? sender, object? args)
			{
				UpdateAllNonClientRegions();
			}

			titleBarMinMaxCloseContainer.LayoutUpdated += OnTitleBarMinMaxCloseLayoutUpdated;
			m_titleBarMinMaxCloseContainerLayoutUpdatedEventHandler.Disposable = Disposable.Create(() =>
			{
				titleBarMinMaxCloseContainer.LayoutUpdated -= OnTitleBarMinMaxCloseLayoutUpdated;
			});
		}

		// adding listeners to minimize, maximize and close XAML buttons so that they behave like Win32 counterparts

		// close button
		if ((m_tpCloseButtonPart = GetTemplateChild("CloseButton") as Button) is not null)
		{
			void OnCloseButtonClicked(object sender, RoutedEventArgs args)
			{
				CloseWindow();
			}

			m_tpCloseButtonPart.Click += OnCloseButtonClicked;
			m_closeButtonClickedEventHandler.Disposable = Disposable.Create(() =>
			{
				m_tpCloseButtonPart!.Click -= OnCloseButtonClicked;
			});

			SetTooltip(m_tpCloseButtonPart, ToolTipClose);
		}

		// minimize button
		if ((m_tpMinimizeButtonPart = GetTemplateChild("MinimizeButton") as Button) is not null)
		{
			void OnMinimizeButtonClicked(object sender, RoutedEventArgs args)
			{
				MinimizeWindow();
			}

			m_tpMinimizeButtonPart.Click += OnMinimizeButtonClicked;
			m_minimizeButtonClickedEventHandler.Disposable = Disposable.Create(() =>
			{
				m_tpMinimizeButtonPart!.Click -= OnMinimizeButtonClicked;
			});

			SetTooltip(m_tpMinimizeButtonPart, ToolTipMinimize);
		}

		// maximize button/restore button
		if ((m_tpMaximizeButtonPart = GetTemplateChild("MaximizeButton") as Button) is not null)
		{
			void OnRestoreOrMaximizeButtonClicked(object sender, RoutedEventArgs args)
			{
				MaximizeOrRestoreWindow();
			}

			m_tpMaximizeButtonPart.Click += OnRestoreOrMaximizeButtonClicked;
			m_maximizeButtonClickedEventHandler.Disposable = Disposable.Create(() =>
			{
				m_tpMaximizeButtonPart!.Click -= OnRestoreOrMaximizeButtonClicked;
			});

			SetTooltip(m_tpMaximizeButtonPart, IsWindowMaximized() ? ToolTipRestore : ToolTipMaximize);
		}
	}

	private void SetTooltip(DependencyObject element, string resourceStringID)
	{
		// Windows automatically provides the tooltips when the caption button
		// regions are reported to it.
		if (!OperatingSystem.IsWindows())
		{
			var toolTipText = DXamlCore.Current.GetLocalizedResourceString(resourceStringID);
			var toolTip = new ToolTip()
			{
				Content = toolTipText
			};

			ToolTipService.SetToolTip(element, toolTip);
		}
	}

	private void UpdateAllNonClientRegions()
	{
		if (_window?.AppWindow is null)
		{
			return;
		}

		if (!InputNonClientPointerSource.TryGetForWindowId(_window.AppWindow.Id, out var input))
		{
			return;
		}

		if (CaptionVisibility == Visibility.Visible)
		{
			if (m_tpMaximizeButtonPart is not null)
			{
				var rect = GetScreenRect(m_tpMaximizeButtonPart);
				UpdateNonClientRegions(NonClientRegionKind.Maximize, [rect]);
			}

			if (m_tpMinimizeButtonPart is not null)
			{
				var rect = GetScreenRect(m_tpMinimizeButtonPart);
				UpdateNonClientRegions(NonClientRegionKind.Minimize, [rect]);
			}

			if (m_tpCloseButtonPart is not null)
			{
				var rect = GetScreenRect(m_tpCloseButtonPart);
				UpdateNonClientRegions(NonClientRegionKind.Close, [rect]);
			}
		}
		else
		{
			UpdateNonClientRegions(NonClientRegionKind.Minimize, []);
			UpdateNonClientRegions(NonClientRegionKind.Maximize, []);
			UpdateNonClientRegions(NonClientRegionKind.Close, []);
		}

		UpdateCaptionRegion();
	}

	private void UpdateCaptionRegion()
	{
		// Caption area rect has the following precedence
		// 1. DragRectangles from AppWindowTitleBar
		// 2. Default caption area (area to the left of caption buttons or nothing if collapsed)
		// + User defined titlebar UIElement (that is always added in addition to either of the above)

		var captionElementRect = m_userTitleBar is not null ? GetScreenRect(m_userTitleBar) : default;

		RectInt32[] precedenceRects;
		if (_window.AppWindow.TitleBar.DragRectangles.Length > 0)
		{
			precedenceRects = _window.AppWindow.TitleBar.DragRectangles;
		}
		else if (CaptionVisibility == Visibility.Visible)
		{
			precedenceRects = [GetDefaultCaptionRegionRect()];
		}
		else
		{
			precedenceRects = [];
		}

		if (captionElementRect != default)
		{
			precedenceRects = [.. precedenceRects, captionElementRect];
		}

		UpdateNonClientRegions(NonClientRegionKind.Caption, precedenceRects);
	}

	private RectInt32 GetDefaultCaptionRegionRect()
	{
		// Caption area should be everything to the left of the buttons (except for the container)
		var titleBarContainer = m_tpTitleBarMinMaxCloseContainerPart;
		if (titleBarContainer is not null)
		{
			var scale = _window.AppWindow.NativeAppWindow.RasterizationScale;
			var transform = titleBarContainer.TransformToVisual(null);
			var point = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
			return new RectInt32
			{
				X = 0,
				Y = 0,
				Width = (int)(point.X * scale),
				Height = (int)(titleBarContainer.ActualHeight * scale)
			};
		}

		return default;
	}

	private void UpdateNonClientRegions(NonClientRegionKind kind, params RectInt32[] replacementRegions)
	{
		if (_window?.AppWindow is null)
		{
			return;
		}

		var input = InputNonClientPointerSource.GetForWindowId(_window.AppWindow.Id);
		var allRegions = input.GetRegionRects(kind);
		var newRegions = allRegions.AsEnumerable();
		if (_nonClientRegions.TryGetValue(kind, out var existingRegions))
		{
			foreach (var region in existingRegions)
			{
				newRegions = newRegions.Where(r =>
					r.X != region.X ||
					r.Y != region.Y ||
					r.Width != region.Width ||
					r.Height != region.Height);
			}
		}

		newRegions = newRegions.Union(replacementRegions);

		_nonClientRegions[kind] = replacementRegions;

		input.SetRegionRects(kind, newRegions.ToArray());
	}

	private RectInt32 GetScreenRect(UIElement uiElement)
	{
		var scale = _window.AppWindow.NativeAppWindow.RasterizationScale;
		var transform = uiElement.TransformToVisual(null);
		var point = transform.TransformPoint(new Windows.Foundation.Point(0, 0));
		var size = new Windows.Foundation.Size(uiElement.RenderSize.Width, uiElement.RenderSize.Height);
		return new RectInt32
		{
			X = (int)(point.X * scale),
			Y = (int)(point.Y * scale),
			Width = (int)(size.Width * scale),
			Height = (int)(size.Height * scale)
		};
	}

	public Visibility CaptionVisibility
	{
		get => (Visibility)GetValue(CaptionVisibilityProperty);
		set => SetValue(CaptionVisibilityProperty, value);
	}

	public static DependencyProperty CaptionVisibilityProperty { get; } =
		DependencyProperty.Register(nameof(CaptionVisibility), typeof(Visibility), typeof(WindowChrome), new FrameworkPropertyMetadata(Visibility.Collapsed));

	protected override void OnContentChanged(object oldContent, object newContent)
	{
		base.OnContentChanged(oldContent, newContent);

		// Fire XamlRoot.Changed
		var xamlIslandRoot = VisualTree.GetXamlIslandRootForElement(this);
		xamlIslandRoot!.ContentRoot.AddPendingXamlRootChangedEvent(ContentRoot.ChangeType.Content);
	}

	private void NotifyUpdate(OverlappedPresenterState state)
	{
		var maximizeButton = m_tpMaximizeButtonPart;
		if (maximizeButton is not null)
		{
			SetTooltip(maximizeButton, state == OverlappedPresenterState.Maximized ? ToolTipRestore : ToolTipMaximize);
		}
	}

	private void UpdateContainerSize()
	{
		if (_window.AppWindow.Presenter is not OverlappedPresenter overlapped)
		{
			return;
		}

		var maximizeButton = m_tpMaximizeButtonPart;
		if (maximizeButton is null)
		{
			return;
		}

		switch (overlapped.State)
		{
			case OverlappedPresenterState.Maximized:
				VisualStateManager.GoToState(maximizeButton, "WindowStateMaximized", false);
				NotifyUpdate(overlapped.State);
				break;
			default:
				VisualStateManager.GoToState(maximizeButton, "WindowStateNormal", false);
				NotifyUpdate(overlapped.State);
				break;
		}

		//UpdateCompIslandWindowSizePosition(compIslandWindowHandle);
		//OnTitleBarSizeChanged();
	}

	private void CloseWindow() => _window.Close();

	private void MinimizeWindow()
	{
		if (_window.AppWindow.Presenter is OverlappedPresenter presenter)
		{
			presenter.Minimize();
		}
	}

	private void MaximizeOrRestoreWindow()
	{
		if (_window.AppWindow.Presenter is OverlappedPresenter presenter)
		{
			if (presenter.State == OverlappedPresenterState.Maximized)
			{
				presenter.Restore();
			}
			else
			{
				presenter.Maximize();
			}
		}
	}

	private void OnLoaded(object sender, RoutedEventArgs args) => ConfigureWindowChrome();

	private bool IsWindowMaximized()
	{
		if (_window.AppWindow.Presenter is OverlappedPresenter presenter)
		{
			return presenter.State == OverlappedPresenterState.Maximized;
		}

		return false;
	}


	private void ConfigureWindowChrome()
	{
		if (!AppWindowTitleBar.IsCustomizationSupported())
		{
			return;
		}

		var appWindowTitleBar = _window.AppWindow.TitleBar;
		var windowChromeCaptionButtonsRequested = appWindowTitleBar.ExtendsContentIntoTitleBar && appWindowTitleBar.PreferredHeightOption != TitleBarHeightOption.Collapsed;

		var hasPresenterTitleBar = true;
		if (_window.AppWindow.Presenter is OverlappedPresenter overlappedPresenter)
		{
			hasPresenterTitleBar = overlappedPresenter.HasTitleBar;
		}

		// If the presenter has no title bar, we shouldn't show caption buttons.
		windowChromeCaptionButtonsRequested = windowChromeCaptionButtonsRequested && hasPresenterTitleBar;

		CaptionVisibility = windowChromeCaptionButtonsRequested ?
			Visibility.Visible :
			Visibility.Collapsed;

		// On Windows, maximized window stretches out of the bounds of the screen slightly.
		// https://www.reddit.com/r/csharp/comments/921k9l/fixing_8_pixel_overhang_on_maximized_window_state/
		var isClientAreaExtended = appWindowTitleBar.ExtendsContentIntoTitleBar || !hasPresenterTitleBar;
		var shouldHavePadding =
			OperatingSystem.IsWindows() &&
			IsWindowMaximized() &&
			isClientAreaExtended;
		Padding = new(shouldHavePadding ? 4 : 0);

		ResizeCaptionButtons();
	}

	private void OnExtendsContentIntoTitleBarChanged(bool extends) => ConfigureWindowChrome();

	internal void SetTitleBar(UIElement? titleBar)
	{
		if (m_inputNonClientPtrSrc is null)
		{
			var appWindow = _window.AppWindow;

			if (appWindow is not null)
			{
				var id = appWindow.Id;

				m_inputNonClientPtrSrc = InputNonClientPointerSource.GetForWindowId(id);
			}
		}

		//detach everything from existing titlebar
		if (m_userTitleBar is not null)
		{
			m_userTitleBarDisposable.Disposable = null;
			m_userTitleBar = null;
		}

		// attach everything to new titlebar
		if (titleBar is not null)
		{
			var newTitleBar = titleBar;

			m_userTitleBar = newTitleBar;

			if (m_userTitleBar is FrameworkElement fe)
			{
				void OnSizeChanged(object sender, object args)
				{
					OnTitleBarSizeChanged();
				}
				fe.SizeChanged += OnSizeChanged;
				m_userTitleBarDisposable.Disposable = Disposable.Create(() =>
				{
					fe.SizeChanged -= OnSizeChanged;
				});
			}
		}

		// Update size of the titleBar container
		OnTitleBarSizeChanged();
	}

	private void OnTitleBarSizeChanged()
	{
		//// if top-level window is getting minimized then no need to resize titlebar and dragbar window as they get minimized
		////if (::IsIconic(m_topLevelWindow))
		////{
		////	return S_OK;
		////}

		//bool didRectChange = false;
		//if (!IsChromeActive())
		//{
		//	RECT empty = { 0, 0, 0, 0 };
		//	if (m_enabledDrag != m_enabledDragCached || !::EqualRect(&m_dragRegionCached, &empty))
		//	{
		//		didRectChange = true;
		//		IFC_RETURN(SetDragRegion(empty));
		//	}
		//}
		//else
		//{
		//	auto userTitlebar = GetPeer()->GetUserTitleBarNoRef();
		//	const wf::Rect logicalWindowRect = WindowHelpers::GetLogicalWindowCoordinates(m_topLevelWindow);
		//	if (userTitlebar)
		//	{
		//		// this function gets called multiple times, in some times layout has not finished 
		//		// so width or height can be 0 in those case. skip setting the values in those times
		//		if (userTitlebar->GetActualWidth() == 0 || userTitlebar->GetActualHeight() == 0)
		//		{
		//			return S_OK;
		//		}

		//		RECT dragBarRect = WindowHelpers::GetClientAreaLogicalRectForUIElement(userTitlebar);
		//		if (m_enabledDrag != m_enabledDragCached || !::EqualRect(&m_dragRegionCached, &dragBarRect))
		//		{
		//			didRectChange = true;
		//			IFC_RETURN(SetDragRegion(dragBarRect));
		//		}
		//	}
		//}

		//if (didRectChange)
		//{
		//	IFC_RETURN(RefreshToolbarOffset());
		//}
	}

	//private void SetDragRegion(RECT rf)
	//{
	//	//UINT32 captionRectLength;
	//	//wil::unique_cotaskmem_ptr<wgr::RectInt32[]> captionRects;

	//	//IFC_RETURN(GetPeer()->GetNonClientInputPtrSrc()->GetRegionRects(mui::NonClientRegionKind_Caption, &captionRectLength, wil::out_param(captionRects)));

	//	//std::vector<wgr::RectInt32> captionRegions{ captionRects.get(), captionRects.get() + captionRectLength }
	//	//;

	//	//// We'll remove the region rect we previously added. We'll add the new one later, if we need to.
	//	//captionRegions.erase(
	//	//	std::remove_if(
	//	//		captionRegions.begin(),
	//	//		captionRegions.end(),
	//	//		[&](wgr::RectInt32 const&rect)
	//	//          {
	//	//	return rect.X == m_scaledDragRegionCached.X &&
	//	//		rect.Y == m_scaledDragRegionCached.Y &&
	//	//		rect.Width == m_scaledDragRegionCached.Width &&
	//	//		rect.Height == m_scaledDragRegionCached.Height;
	//	//}),
	//	//      captionRegions.end());

	//	//// sometimes dragging needs to be disabled temporarily
	//	//// for example : when content dialog's smoke screen is displayed
	//	//if (m_enabledDrag && !IsRectEmpty(&rf))
	//	//{
	//	//	// Xaml works with logical (dpi-applied) client coordinates
	//	//	// InputNonClientPointerSource apis take non-dpi client coordinates
	//	//	// physical client coordinates = dpi applied coordinates * dpi scale
	//	//	float scale = WindowHelpers::GetCurrentDpiScale(m_topLevelWindow);
	//	//	m_scaledDragRegionCached = {
	//	//		static_cast<int>(std::round(rf.left * scale)),
	//	//          static_cast<int>(std::round(rf.top * scale)),
	//	//          static_cast<int>(std::round(RectHelpers::rectWidth(rf) * scale)),
	//	//          static_cast<int>(std::round(RectHelpers::rectHeight(rf) * scale))

	//	//}
	//	//	;
	//	//}
	//	//else
	//	//{
	//	//	m_scaledDragRegionCached = { 0, 0, 0, 0 }
	//	//	;
	//	//}

	//	//captionRegions.push_back(m_scaledDragRegionCached);

	//	//IFC_RETURN(GetPeer()->GetNonClientInputPtrSrc()->SetRegionRects(mui::NonClientRegionKind_Caption, captionRegions.size(), captionRegions.data()));

	//	//m_dragRegionCached = rf;
	//	//m_enabledDragCached = m_enabledDrag;
	//	//return S_OK;
	//}
}

