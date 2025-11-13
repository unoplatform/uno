// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference InfoBadge.cpp, tag winui3/release/1.4.2

using System;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Represents a control for indicating notifications, alerts, new content, or to attract focus to an area within an app.
/// </summary>
public partial class InfoBadge : Control
{
	//private const string c_IconPresenterName = "IconPresenter";

	/// <summary>
	/// Initializes a new instance of the InfoBadge class.
	/// </summary>
	public InfoBadge()
	{
		this.SetDefaultStyleKey();

		SetValue(TemplateSettingsProperty, new InfoBadgeTemplateSettings());
		SizeChanged += OnSizeChanged;
	}

	protected override void OnApplyTemplate()
	{
		OnDisplayKindPropertiesChanged();
	}

	protected override Size MeasureOverride(Size availableSize)
	{
		var defaultDesiredSize = base.MeasureOverride(availableSize);
		if (defaultDesiredSize.Width < defaultDesiredSize.Height)
		{
			return new Size(defaultDesiredSize.Height, defaultDesiredSize.Height);
		}
		return defaultDesiredSize;
	}

	private void OnPropertyChanged(DependencyPropertyChangedEventArgs args)
	{
		var property = args.Property;
		Control thisAsControl = this;

		if (property == ValueProperty)
		{
			if (Value < -1)
			{
				throw new ArgumentOutOfRangeException("Value must be equal to or greater than -1");
			}
		}

		if (property == ValueProperty ||
			property == IconSourceProperty)
		{
			OnDisplayKindPropertiesChanged();
		}
	}

	void OnDisplayKindPropertiesChanged()
	{
		Control thisAsControl = this;
		if (Value >= 0)
		{
			VisualStateManager.GoToState(thisAsControl, "Value", true);
		}
		else if (IconSource is { } iconSource)
		{
			TemplateSettings.IconElement = iconSource.CreateIconElement();
			if (iconSource is FontIconSource)
			{
				VisualStateManager.GoToState(thisAsControl, "FontIcon", true);
			}
			else
			{
				VisualStateManager.GoToState(thisAsControl, "Icon", true);
			}
		}
		else
		{
			VisualStateManager.GoToState(thisAsControl, "Dot", true);
		}
	}

	private void OnSizeChanged(object sender, Microsoft.UI.Xaml.SizeChangedEventArgs args)
	{
		CornerRadius GetCornerRadius()
		{
			var cornerRadiusValue = ActualHeight / 2;
			if (ReadLocalValue(CornerRadiusProperty) == DependencyProperty.UnsetValue)
			{
				return new CornerRadius(cornerRadiusValue, cornerRadiusValue, cornerRadiusValue, cornerRadiusValue);
			}
			else
			{
				return new CornerRadius();
			}
			// Uno Specific: WinUI2 source had an extra if condition wrapping the previous if,
			// so this return was reachable. That condition was removed in WinUI3, but the
			// return was left here even though it's unreachable
			//return new CornerRadius(cornerRadiusValue, cornerRadiusValue, cornerRadiusValue, cornerRadiusValue);
		}

		var value = GetCornerRadius();

		TemplateSettings.InfoBadgeCornerRadius = value;
	}
}
