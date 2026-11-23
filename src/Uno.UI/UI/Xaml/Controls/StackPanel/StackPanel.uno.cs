using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

partial class StackPanel
{
	// WinUI syncs the core backing field and calls OnAreScrollSnapPointsRegularChanged from CStackPanel::SetValue.
	private void OnAreScrollSnapPointsRegularChanged(bool oldValue, bool newValue)
	{
		m_bAreScrollSnapPointsRegular = newValue;
		OnAreScrollSnapPointsRegularChanged();
	}

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
}
