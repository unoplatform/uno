// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference NavigationViewItemPresenter.properties.cpp, commit 65718e2813

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public partial class NavigationViewItemPresenter
{
	/// <summary>
	/// Gets or sets the icon in a NavigationView item.
	/// </summary>
	public IconElement Icon
	{
		get => (IconElement)GetValue(IconProperty);
		set => SetValue(IconProperty, value);
	}

	/// <summary>
	/// Identifies the Icon dependency property.
	/// </summary>
	public static DependencyProperty IconProperty { get; } =
		DependencyProperty.Register(nameof(Icon), typeof(IconElement), typeof(NavigationViewItemPresenter), new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets or sets the info badge in a NavigationView item.
	/// </summary>
	public InfoBadge InfoBadge
	{
		get => (InfoBadge)GetValue(InfoBadgeProperty);
		set => SetValue(InfoBadgeProperty, value);
	}

	/// <summary>
	/// Identifies the InfoBadge dependency property.
	/// </summary>
	public static DependencyProperty InfoBadgeProperty { get; } =
		DependencyProperty.Register(nameof(InfoBadge), typeof(InfoBadge), typeof(NavigationViewItemPresenter), new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets the template settings.
	/// </summary>
	public NavigationViewItemPresenterTemplateSettings TemplateSettings
	{
		get => (NavigationViewItemPresenterTemplateSettings)GetValue(TemplateSettingsProperty);
		internal set => SetValue(TemplateSettingsProperty, value);
	}

	/// <summary>
	/// Identifies the TemplateSettings dependency property.
	/// </summary>
	public static DependencyProperty TemplateSettingsProperty { get; } =
		DependencyProperty.Register(nameof(TemplateSettings), typeof(NavigationViewItemPresenterTemplateSettings), typeof(NavigationViewItemPresenter), new FrameworkPropertyMetadata(null));
}
