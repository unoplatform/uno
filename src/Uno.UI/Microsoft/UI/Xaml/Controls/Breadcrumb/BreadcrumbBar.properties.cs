// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class BreadcrumbBar : Control
	{
		public object? ItemsSource
		{
			get => GetValue(ItemsSourceProperty);
			set => SetValue(ItemsSourceProperty, value);
		}

		public static DependencyProperty ItemsSourceProperty { get; } =
			DependencyProperty.Register(
				nameof(ItemsSource),
				typeof(object),
				typeof(BreadcrumbBar),
				new PropertyMetadata(null));



		public object? ItemTemplate
		{
			get => GetValue(ItemTemplateProperty);
			set => SetValue(ItemTemplateProperty, value);
		}

		public static DependencyProperty ItemTemplateProperty { get; } =
			DependencyProperty.Register(
				nameof(ItemTemplate),
				typeof(object),
				typeof(BreadcrumbBar),
				new PropertyMetadata(null));


	}
}
