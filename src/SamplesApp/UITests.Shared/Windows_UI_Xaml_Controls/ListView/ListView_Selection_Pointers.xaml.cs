using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView", "Pointers", IgnoreInSnapshotTests = true)]
	public sealed partial class ListView_Selection_Pointers : Page
	{
		public ListView_Selection_Pointers()
		{
			this.InitializeComponent();

			DataContext = "!";
		}

		/// <inheritdoc />
		protected override void OnPointerEntered(PointerRoutedEventArgs e)
		{
			_pageEntered.Text = (e.OriginalSource as FrameworkElement)?.DataContext?.ToString() ?? "?";
			base.OnPointerEntered(e);
		}

		/// <inheritdoc />
		protected override void OnPointerExited(PointerRoutedEventArgs e)
		{
			_pageExited.Text = (e.OriginalSource as FrameworkElement)?.DataContext?.ToString() ?? "?";
			base.OnPointerExited(e);
		}

		/// <inheritdoc />
		protected override void OnPointerPressed(PointerRoutedEventArgs e)
		{
			_pagePressed.Text = (e.OriginalSource as FrameworkElement)?.DataContext?.ToString() ?? "?";
			base.OnPointerPressed(e);
		}

		/// <inheritdoc />
		protected override void OnPointerReleased(PointerRoutedEventArgs e)
		{
			_pageReleased.Text = (e.OriginalSource as FrameworkElement)?.DataContext?.ToString() ?? "?";
			base.OnPointerReleased(e);
		}

		/// <inheritdoc />
		protected override void OnTapped(TappedRoutedEventArgs e)
		{
			_pageTapped.Text = (e.OriginalSource as FrameworkElement)?.DataContext?.ToString() ?? "?";
			base.OnTapped(e);
		}

		private void OnItemEntered(object sender, PointerRoutedEventArgs e)
			=> _itemEntered.Text = (sender as FrameworkElement)?.DataContext?.ToString() ?? "?";

		private void OnItemExited(object sender, PointerRoutedEventArgs e)
			=> _itemExited.Text = (sender as FrameworkElement)?.DataContext?.ToString() ?? "?";

		private void OnItemPressed(object sender, PointerRoutedEventArgs e)
			=> _itemPressed.Text = (sender as FrameworkElement)?.DataContext?.ToString() ?? "?";

		private void OnItemReleased(object sender, PointerRoutedEventArgs e)
			=> _itemReleased.Text = (sender as FrameworkElement)?.DataContext?.ToString() ?? "?";

		private void OnItemTapped(object sender, TappedRoutedEventArgs e)
			=> _itemTapped.Text = (sender as FrameworkElement)?.DataContext?.ToString() ?? "?";

		private void OnItemClicked(object sender, ItemClickEventArgs e)
			=> _itemClicked.Text = e.ClickedItem?.ToString() ?? "?";
	}
}
