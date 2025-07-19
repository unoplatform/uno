// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TwoPaneView.properties.cpp, tag winui3/release/1.4.2

using System;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents a container with two views that size and position content
/// in the available space, either side-by-side or top-bottom.
/// </summary>
public partial class TwoPaneView : Microsoft.UI.Xaml.Controls.Control
{
#if !UNO_HAS_ENHANCED_LIFECYCLE
	private bool _subscribedToXamlRoot;
#endif

	private const string c_pane1ScrollViewerName = "PART_Pane1ScrollViewer";
	private const string c_pane2ScrollViewerName = "PART_Pane2ScrollViewer";

	private const string c_columnLeftName = "PART_ColumnLeft";
	private const string c_columnMiddleName = "PART_ColumnMiddle";
	private const string c_columnRightName = "PART_ColumnRight";
	private const string c_rowTopName = "PART_RowTop";
	private const string c_rowMiddleName = "PART_RowMiddle";
	private const string c_rowBottomName = "PART_RowBottom";

	/// <summary>
	/// Initializes a new instance of the TwoPaneView class.
	/// </summary>
	public TwoPaneView()
	{
		DefaultStyleKey = typeof(TwoPaneView);

		SizeChanged += OnSizeChanged;

		// TODO Uno specific: Revoke events - https://github.com/microsoft/microsoft-ui-xaml/issues/6357
		Unloaded += (s, e) =>
		{
			m_xamlRootChangedRevoker.Disposable = null;
			m_pane1LoadedRevoker.Disposable = null;
			m_pane2LoadedRevoker.Disposable = null;
		};
	}

#if !UNO_HAS_ENHANCED_LIFECYCLE
	private protected override void OnLoaded()
	{
		if (!_subscribedToXamlRoot)
		{
			var xamlRoot = XamlRoot;
			xamlRoot!.Changed += OnXamlRootChanged;
			m_xamlRootChangedRevoker.Disposable = Disposable.Create(() => xamlRoot!.Changed -= OnXamlRootChanged);
			_subscribedToXamlRoot = true;
		}

		base.OnLoaded();
	}
#endif

	protected override void OnApplyTemplate()
	{
		m_loaded = true;

		var xamlRoot = XamlRoot;
		// Uno-specific workaround: On some platforms (mainly mobile), we can get here with incorrectly null xamlRoot.
#if !UNO_HAS_ENHANCED_LIFECYCLE
		_subscribedToXamlRoot = xamlRoot is not null;
		if (_subscribedToXamlRoot)
		{
			xamlRoot!.Changed += OnXamlRootChanged;
			m_xamlRootChangedRevoker.Disposable = Disposable.Create(() => xamlRoot!.Changed -= OnXamlRootChanged);
		}
#else
		xamlRoot!.Changed += OnXamlRootChanged;
		m_xamlRootChangedRevoker.Disposable = Disposable.Create(() => xamlRoot!.Changed -= OnXamlRootChanged);
#endif

		SetScrollViewerProperties(c_pane1ScrollViewerName, m_pane1LoadedRevoker);
		SetScrollViewerProperties(c_pane2ScrollViewerName, m_pane2LoadedRevoker);

		if (GetTemplateChild(c_columnLeftName) is ColumnDefinition column)
		{
			m_columnLeft = column;
		}
		if (GetTemplateChild(c_columnMiddleName) is ColumnDefinition middleColumn)
		{
			m_columnMiddle = middleColumn;
		}
		if (GetTemplateChild(c_columnRightName) is ColumnDefinition columnRight)
		{
			m_columnRight = columnRight;
		}
		if (GetTemplateChild(c_rowTopName) is RowDefinition rowTop)
		{
			m_rowTop = rowTop;
		}
		if (GetTemplateChild(c_rowMiddleName) is RowDefinition rowMiddle)
		{
			m_rowMiddle = rowMiddle;
		}
		if (GetTemplateChild(c_rowBottomName) is RowDefinition rowBottom)
		{
			m_rowBottom = rowBottom;
		}
	}

	private void SetScrollViewerProperties(string scrollViewerName, SerialDisposable revoker)
	{
		if (GetTemplateChild(scrollViewerName) is ScrollViewer scrollViewer)
		{
			scrollViewer.Loaded += OnScrollViewerLoaded;
			revoker.Disposable = Disposable.Create(() => scrollViewer.Loaded -= OnScrollViewerLoaded);
		}
	}

	private void OnScrollViewerLoaded(object sender, RoutedEventArgs args)
	{
		if (sender is FrameworkElement scrollViewer)
		{
			var scrollContentPresenterFE = SharedHelpers.FindInVisualTreeByName(scrollViewer, "ScrollContentPresenter");
			if (scrollContentPresenterFE != null)
			{
				if (scrollContentPresenterFE is ScrollContentPresenter scrollContentPresenter)
				{
					scrollContentPresenter.SizesContentToTemplatedParent = true;
				}
			}
		}
	}

	private void OnXamlRootChanged(object sender, XamlRootChangedEventArgs args)
	{
		UpdateMode();
	}

	private void OnSizeChanged(object sender, SizeChangedEventArgs args)
	{
		UpdateMode();
	}

	private void UpdateMode()
	{
		// Don't bother running this logic until after we hit OnApplyTemplate.
		if (!m_loaded) return;

		double controlWidth = ActualWidth;
		double controlHeight = ActualHeight;

		ViewMode newMode = (PanePriority == TwoPaneViewPriority.Pane1) ? ViewMode.Pane1Only : ViewMode.Pane2Only;

		// Calculate new mode
		DisplayRegionHelperInfo info = DisplayRegionHelper.GetRegionInfo();
		Rect rcControl = GetControlRect();
		bool isInMultipleRegions = IsInMultipleRegions(info, rcControl);

		if (isInMultipleRegions)
		{
			if (info.Mode == TwoPaneViewMode.Wide)
			{
				// Regions are laid out horizontally
				if (WideModeConfiguration != TwoPaneViewWideModeConfiguration.SinglePane)
				{
					newMode = (WideModeConfiguration == TwoPaneViewWideModeConfiguration.LeftRight) ? ViewMode.LeftRight : ViewMode.RightLeft;
				}
			}
			else if (info.Mode == TwoPaneViewMode.Tall)
			{
				// Regions are laid out vertically
				if (TallModeConfiguration != TwoPaneViewTallModeConfiguration.SinglePane)
				{
					newMode = (TallModeConfiguration == TwoPaneViewTallModeConfiguration.TopBottom) ? ViewMode.TopBottom : ViewMode.BottomTop;
				}
			}
		}
		else
		{
			// One region
			if (controlWidth > MinWideModeWidth && WideModeConfiguration != TwoPaneViewWideModeConfiguration.SinglePane)
			{
				// Split horizontally
				newMode = (WideModeConfiguration == TwoPaneViewWideModeConfiguration.LeftRight) ? ViewMode.LeftRight : ViewMode.RightLeft;
			}
			else if (controlHeight > MinTallModeHeight && TallModeConfiguration != TwoPaneViewTallModeConfiguration.SinglePane)
			{
				// Split vertically
				newMode = (TallModeConfiguration == TwoPaneViewTallModeConfiguration.TopBottom) ? ViewMode.TopBottom : ViewMode.BottomTop;
			}
		}

		// Update row/column sizes (this may need to happen even if the mode doesn't change)
		UpdateRowsColumns(newMode, info, rcControl);

		// Update mode if necessary
		if (newMode != m_currentMode)
		{
			m_currentMode = newMode;

			TwoPaneViewMode newViewMode = TwoPaneViewMode.SinglePane;

			switch (m_currentMode)
			{
				case ViewMode.Pane1Only: VisualStateManager.GoToState(this, "ViewMode_OneOnly", true); break;
				case ViewMode.Pane2Only: VisualStateManager.GoToState(this, "ViewMode_TwoOnly", true); break;
				case ViewMode.LeftRight: VisualStateManager.GoToState(this, "ViewMode_LeftRight", true); newViewMode = TwoPaneViewMode.Wide; break;
				case ViewMode.RightLeft: VisualStateManager.GoToState(this, "ViewMode_RightLeft", true); newViewMode = TwoPaneViewMode.Wide; break;
				case ViewMode.TopBottom: VisualStateManager.GoToState(this, "ViewMode_TopBottom", true); newViewMode = TwoPaneViewMode.Tall; break;
				case ViewMode.BottomTop: VisualStateManager.GoToState(this, "ViewMode_BottomTop", true); newViewMode = TwoPaneViewMode.Tall; break;
			}

			if (newViewMode != Mode)
			{
				SetValue(ModeProperty, newViewMode);
				ModeChanged?.Invoke(this, this);
			}
		}
	}

	private void UpdateRowsColumns(ViewMode newMode, DisplayRegionHelperInfo info, Rect rcControl)
	{
		if (m_columnLeft != null && m_columnMiddle != null && m_columnRight != null && m_rowTop != null && m_rowMiddle != null && m_rowBottom != null)
		{
			// Reset split lengths
			m_columnMiddle.Width = new GridLength(0, GridUnitType.Pixel);
			m_rowMiddle.Height = new GridLength(0, GridUnitType.Pixel);

			// Set columns lengths
			if (newMode == ViewMode.LeftRight || newMode == ViewMode.RightLeft)
			{
				m_columnLeft.Width = (newMode == ViewMode.LeftRight) ? Pane1Length : Pane2Length;
				m_columnRight.Width = (newMode == ViewMode.LeftRight) ? Pane2Length : Pane1Length;
			}
			else
			{
				m_columnLeft.Width = new GridLength(1, GridUnitType.Star);
				m_columnRight.Width = new GridLength(0, GridUnitType.Pixel);
			}

			// Set row lengths
			if (newMode == ViewMode.TopBottom || newMode == ViewMode.BottomTop)
			{
				m_rowTop.Height = (newMode == ViewMode.TopBottom) ? Pane1Length : Pane2Length;
				m_rowBottom.Height = (newMode == ViewMode.TopBottom) ? Pane2Length : Pane1Length;
			}
			else
			{
				m_rowTop.Height = new GridLength(1, GridUnitType.Star);
				m_rowBottom.Height = new GridLength(0, GridUnitType.Pixel);
			}

			// Handle regions
			if (IsInMultipleRegions(info, rcControl) && newMode != ViewMode.Pane1Only && newMode != ViewMode.Pane2Only)
			{
				Rect rc1 = info.Regions[0];
				Rect rc2 = info.Regions[1];
				Rect rcWindow = DisplayRegionHelper.WindowRect();

				if (info.Mode == TwoPaneViewMode.Wide)
				{
					m_columnMiddle.Width = new GridLength(rc2.X - rc1.Width, GridUnitType.Pixel);

					m_columnLeft.Width = new GridLength(rc1.Width - rcControl.X, GridUnitType.Pixel);

					// UNO TODO: Max is needed when regions don't match the Window size orientation
					m_columnRight.Width = new GridLength(Math.Max(0, rc2.Width - ((rcWindow.Width - rcControl.Width) - rcControl.X)), GridUnitType.Pixel);
				}
				else
				{
					m_rowMiddle.Height = new GridLength(rc2.Y - rc1.Height, GridUnitType.Pixel);

					m_rowTop.Height = new GridLength(rc1.Height - rcControl.Y, GridUnitType.Pixel);

					// UNO TODO: Max is needed when regions don't match the Window size orientation
					m_rowBottom.Height = new GridLength(Math.Max(0, rc2.Height - ((rcWindow.Height - rcControl.Height) - rcControl.Y)), GridUnitType.Pixel);
				}
			}
		}
	}

	private Rect GetControlRect()
	{
		// Find out where this control is in the window
		GeneralTransform transform = TransformToVisual(DisplayRegionHelper.WindowElement());
		return transform.TransformBounds(new Rect(0, 0, (float)ActualWidth, (float)ActualHeight));
	}

	private bool IsInMultipleRegions(DisplayRegionHelperInfo info, Rect rcControl)
	{
		bool isInMultipleRegions = false;

		if (info.Mode != TwoPaneViewMode.SinglePane)
		{
			Rect rc1 = info.Regions[0];
			Rect rc2 = info.Regions[1];

			if (info.Mode == TwoPaneViewMode.Wide)
			{
				// Check that the control is over the split
				if (rcControl.X < rc1.Width && rcControl.X + rcControl.Width > rc2.X)
				{
					isInMultipleRegions = true;
				}
			}
			else if (info.Mode == TwoPaneViewMode.Tall)
			{
				// Check that the control is over the split
				if (rcControl.Y < rc1.Height && rcControl.Y + rcControl.Height > rc2.Y)
				{
					isInMultipleRegions = true;
				}
			}
		}

		return isInMultipleRegions;
	}

	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		var property = args.Property;

		// Clamp property values -- early return if the values were clamped as we'll come back with the new value.
		if (property == MinWideModeWidthProperty || property == MinTallModeHeightProperty)
		{
			var value = (double)args.NewValue;
			var clampedValue = Math.Max(0.0, value);
			if (clampedValue != value)
			{
				SetValue(property, clampedValue);
				return;
			}
		}

		if (property == PanePriorityProperty
			|| property == Pane1LengthProperty
			|| property == Pane2LengthProperty
			|| property == WideModeConfigurationProperty
			|| property == TallModeConfigurationProperty
			|| property == MinWideModeWidthProperty
			|| property == MinTallModeHeightProperty)
		{
			UpdateMode();
		}
	}
}
