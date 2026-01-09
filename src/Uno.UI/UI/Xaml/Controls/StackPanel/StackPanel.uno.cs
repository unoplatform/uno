using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
#if __ANDROID__
using Android.Views;
#elif __APPLE_UIKIT__
using View = UIKit.UIView;
#else
using View = Microsoft.UI.Xaml.UIElement;
#endif

namespace Microsoft.UI.Xaml.Controls;

partial class StackPanel
{
#if HAS_UNO // WinUI automatically syncs the backing field, we need to do it manually.
	private void OnAreScrollSnapPointsRegularChanged(bool oldValue, bool newValue)
	{
		m_bAreScrollSnapPointsRegular = newValue;
		// Called from SetValue in WinUI
		OnAreScrollSnapPointsRegularChanged();
	}
#endif

	private void OnBackgroundSizingChanged(DependencyPropertyChangedEventArgs e)
	{
		OnBackgroundSizingChangedInnerPanel(e);
	}

	private void OnBorderBrushPropertyChanged(Brush oldValue, Brush newValue)
	{
		BorderBrushInternal = newValue;
		OnBorderBrushChanged(oldValue, newValue);
	}

	private void OnBorderThicknessPropertyChanged(Thickness oldValue, Thickness newValue)
	{
		BorderThicknessInternal = newValue;
		OnBorderThicknessChanged(oldValue, newValue);
	}

	private void OnCornerRadiusPropertyChanged(CornerRadius oldValue, CornerRadius newValue)
	{
		CornerRadiusInternal = newValue;
		OnCornerRadiusChanged(oldValue, newValue);
	}

	private void OnPaddingPropertyChanged(Thickness oldValue, Thickness newValue)
	{
		PaddingInternal = newValue;
		OnPaddingChanged(oldValue, newValue);
	}

	internal override Orientation? PhysicalOrientation => Orientation;

	protected override bool? IsWidthConstrainedInner(View requester)
	{
		if (requester != null && Orientation == Orientation.Horizontal)
		{
			return false;
		}

		return this.IsWidthConstrainedSimple();
	}

	protected override bool? IsHeightConstrainedInner(View requester)
	{
		if (requester != null && Orientation == Orientation.Vertical)
		{
			return false;
		}

		return this.IsHeightConstrainedSimple();
	}
}
