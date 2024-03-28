// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX reference PagerControlTemplateSettings.properties.cpp, tag winui3/release/1.4.2

using System.Collections.Generic;
using Windows.UI.Xaml;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

public partial class PagerControlTemplateSettings : DependencyObject
{
	public PagerControlTemplateSettings()
	{
	}

	public IList<object> Pages
	{
		get => (IList<object>)GetValue(PagesProperty);
		set => SetValue(PagesProperty, value);
	}

	public static DependencyProperty PagesProperty { get; } =
		DependencyProperty.Register(nameof(Pages), typeof(IList<object>), typeof(PagerControlTemplateSettings), new FrameworkPropertyMetadata(null));

	public IList<object> NumberPanelItems
	{
		get { return (IList<object>)GetValue(NumberPanelItemsProperty); }
		set { SetValue(NumberPanelItemsProperty, value); }
	}

	public static DependencyProperty NumberPanelItemsProperty { get; } =
		DependencyProperty.Register(nameof(NumberPanelItems), typeof(IList<object>), typeof(PagerControlTemplateSettings), new FrameworkPropertyMetadata(null));
}
