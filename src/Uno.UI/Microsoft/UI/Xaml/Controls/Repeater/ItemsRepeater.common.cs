// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ItemsRepeater.common.cpp, tag winui3/release/1.4.2

#nullable enable

using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

internal partial class CachedVisualTreeHelpers
{
	public static Rect GetLayoutSlot(FrameworkElement element) => 
		LayoutInformation.GetLayoutSlot(element);

	public static DependencyObject GetParent(DependencyObject child) =>
		VisualTreeHelper.GetParent(child);

	public static IDataTemplateComponent GetDataTemplateComponent(UIElement element) =>
		XamlBindingHelper.GetDataTemplateComponent(element);
}
