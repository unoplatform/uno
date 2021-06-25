// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#nullable enable

using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class BreadcrumbBar : Control
	{
		/// <summary>
		/// Gets or sets an object source used to generate the content of the BreadcrumbBar.
		/// </summary>
		public object? ItemsSource
		{
			get => GetValue(ItemsSourceProperty);
			set => SetValue(ItemsSourceProperty, value);
		}

		/// <summary>
		/// Identifies the ItemsSource dependency property.
		/// </summary>
		public static DependencyProperty ItemsSourceProperty { get; } =
			DependencyProperty.Register(
				nameof(ItemsSource),
				typeof(object),
				typeof(BreadcrumbBar),
				new FrameworkPropertyMetadata(null));

		/// <summary>
		/// Gets or sets the data template for the BreadcrumbBarItem.
		/// </summary>
		public object? ItemTemplate
		{
			get => GetValue(ItemTemplateProperty);
			set => SetValue(ItemTemplateProperty, value);
		}

		/// <summary>
		/// Identifies the ItemTemplate dependency property.
		/// </summary>
		public static DependencyProperty ItemTemplateProperty { get; } =
			DependencyProperty.Register(
				nameof(ItemTemplate),
				typeof(object),
				typeof(BreadcrumbBar),
				new FrameworkPropertyMetadata(null));

		/// <summary>
		/// Occurs when an item is clicked in the BreadcrumbBar.
		/// </summary>
		public event TypedEventHandler<BreadcrumbBar, BreadcrumbBarItemClickedEventArgs>? ItemClicked;
	}
}
