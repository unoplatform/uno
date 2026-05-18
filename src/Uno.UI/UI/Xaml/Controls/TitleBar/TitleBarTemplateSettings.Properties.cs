// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TitleBarTemplateSettings.idl, TitleBarTemplateSettings.properties.cpp, TitleBarTemplateSettings.properties.h, commit 5f9e85113

namespace Microsoft.UI.Xaml.Controls;

public partial class TitleBarTemplateSettings
{
	/// <summary>
	/// Gets or sets the icon element for the title bar.
	/// </summary>
	public IconElement IconElement
	{
		get => (IconElement)GetValue(IconElementProperty);
		set => SetValue(IconElementProperty, value);
	}

	/// <summary>
	/// Identifies the IconElement dependency property.
	/// </summary>
	public static DependencyProperty IconElementProperty { get; } =
		DependencyProperty.Register(
			nameof(IconElement),
			typeof(IconElement),
			typeof(TitleBarTemplateSettings),
			new FrameworkPropertyMetadata(null));
}
