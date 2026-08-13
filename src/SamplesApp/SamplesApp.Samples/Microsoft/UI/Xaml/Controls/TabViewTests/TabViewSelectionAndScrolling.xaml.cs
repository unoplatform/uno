#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.Extensions;
using Uno.UI.Samples.Controls;
using Uno.UI.Samples.UITests.Helpers;

using TabView = Microsoft.UI.Xaml.Controls.TabView;
using TabViewItem = Microsoft.UI.Xaml.Controls.TabViewItem;
using TabViewTabCloseRequestedEventArgs = Microsoft.UI.Xaml.Controls.TabViewTabCloseRequestedEventArgs;
using TabViewTabDragStartingEventArgs = Microsoft.UI.Xaml.Controls.TabViewTabDragStartingEventArgs;
using TabViewTabDragCompletedEventArgs = Microsoft.UI.Xaml.Controls.TabViewTabDragCompletedEventArgs;

using static Uno.UI.Extensions.ViewExtensions;
using static Uno.UI.Extensions.PrettyPrint;
using System.Numerics;

namespace UITests.Microsoft_UI_Xaml_Controls.TabViewTests;

[Sample("TabView", "MUX", IsManualTest = true)]
public sealed partial class TabViewSelectionAndScrolling : Page
{
	private MainViewModel ViewModel { get; set; }

	public TabViewSelectionAndScrolling()
	{
		this.InitializeComponent();
		DataContext = ViewModel = new MainViewModel();
	}

	private void TabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
	{
		if (ViewModel.Tabs?.Contains(args.Item) == true)
		{
			//ViewModel.Tabs = ViewModel.Tabs.Except(new[] { args.Item }).ToArray();
			ViewModel.Tabs.Remove(args.Item);
		}
	}

	private void DebugVT(object sender, RoutedEventArgs e)
	{
		var tree = (SUT as FrameworkElement).TreeGraph(DescribeVT);
	}

	private IEnumerable<string> DescribeVT(object x)
	{
#if __ANDROID__
		if (x is Android.Views.View v)
		{
			yield return $"LTRB={v.Left},{v.Top},{v.Right},{v.Bottom}";
		}
#elif __APPLE_UIKIT__
		if (x is UIKit.UIView view && view.Superview is { })
		{
			var abs = view.Superview.ConvertPointToView(view.Frame.Location, toView: null);
			yield return $"Abs=[Rect {view.Frame.Width:0.#}x{view.Frame.Height:0.#}@{abs.X:0.#},{abs.Y:0.#}]";
		}
#endif
#if !WINAPPSDK && !WINDOWS_UWP
		if (x is FrameworkElement fe)
		{
			yield return $"Desired={FormatSize(fe.DesiredSize)}";
			yield return $"Actual={FormatSize(fe.ActualSize.ToSize())}";
			yield return $"IsLoaded={fe.IsLoaded}";
		}
		if (TryGetDpValue<Thickness>(x, "Margin", out var margin)) yield return $"Margin={FormatThickness(margin)}";
		if (TryGetDpValue<Thickness>(x, "Padding", out var padding)) yield return $"Padding={FormatThickness(padding)}";
		if (x is ItemsStackPanel isp) yield return $"Layouter={FormatObject((isp as IVirtualizingPanel)?.GetLayouter())}";
		if (x is ScrollViewer sv)
		{
			yield return $"Offset=({sv.HorizontalOffset},{sv.VerticalOffset}), Viewport=({sv.ViewportHeight},{sv.ViewportWidth}), Extent=({sv.ExtentHeight},{sv.ExtentWidth})";
		}
#endif
		if (x is ContentPresenter cp) yield return $"Content={FormatObject(cp.Content)}";
		if (x is ListViewItem lvi)
		{
			yield return $"Index={(ItemsControl.ItemsControlFromItemContainer(lvi)?.IndexFromContainer(lvi) ?? -1)}";
		}
	}

	private void SelectItem(object sender, RoutedEventArgs e)
	{
		if (sender is Button btn && int.TryParse(btn.Tag as string, out int index))
		{
			ViewModel.SelectedTab = ViewModel.Tabs?.ElementAtOrDefault(index);
		}
	}

	private void ScrollTo(object sender, RoutedEventArgs e)
	{
		if (sender is Button btn && int.TryParse(btn.Tag as string, out int index))
		{
			var lv = SUT as object as ListView ?? SUT.FindFirstDescendant<ListView>()!;
			lv.ScrollIntoView(ViewModel.Tabs?.ElementAtOrDefault(index));
		}
	}

	private void ScrollToLeading(object sender, RoutedEventArgs e)
	{
		if (sender is Button btn && int.TryParse(btn.Tag as string, out int index))
		{
			var lv = SUT as object as ListView ?? SUT.FindFirstDescendant<ListView>()!;
			lv.ScrollIntoView(ViewModel.Tabs?.ElementAtOrDefault(index), ScrollIntoViewAlignment.Leading);
		}
	}

	private class MainViewModel : ViewModelBase
	{
		private object? _selectedTab;

		public ObservableCollection<object>? Tabs { get; }
		public object? SelectedTab { get => _selectedTab; set => Set(ref _selectedTab, value); }

		public MainViewModel()
		{
			Tabs = new ObservableCollection<object>
			(
				Enumerable.Range(0, 100).Select(x => x.ToString())
			);
			SelectedTab = Tabs.FirstOrDefault();
		}
	}
}
