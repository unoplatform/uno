// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference XamlOM/Model/Microsoft.UI.Xaml.Controls.Primitives.cs, tag winui3/release/2.5.1

using Uno.UI.Helpers.Boxes;

namespace Microsoft.UI.Xaml.Controls.Primitives;

// TODO Uno: WinUI declares this class sealed.
public partial class ListViewItemTemplateSettings : ListViewBaseItemTemplateSettings
{
	public int DragItemsCount
	{
		get => (int)GetValue(DragItemsCountProperty);
		internal set => SetValue(DragItemsCountProperty, value);
	}

	private static DependencyProperty DragItemsCountProperty { get; } =
		DependencyProperty.Register(
			nameof(DragItemsCount),
			typeof(int),
			typeof(ListViewItemTemplateSettings),
			new FrameworkPropertyMetadata(IntBoxes.Zero));

	internal ListViewItemTemplateSettings()
	{
	}
}
