// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ColorPickerSlider.properties.cpp, tag winui3/release/1.4.2

using Windows.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	public partial class ColorPickerSlider
	{
		public ColorPickerHsvChannel ColorChannel
		{
			get => (ColorPickerHsvChannel)GetValue(ColorChannelProperty);
			set => SetValue(ColorChannelProperty, value);
		}

		public static DependencyProperty ColorChannelProperty { get; } =
			DependencyProperty.Register(
				nameof(ColorChannel),
				typeof(ColorPickerHsvChannel),
				typeof(ColorPickerSlider),
				new FrameworkPropertyMetadata(ColorPickerHsvChannel.Value));
	}
}
