using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using nVentive.Umbrella.Views.Extensions;
using Uno.UI.Samples.Controls;

using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Uno.UI.Samples.Content.UITests.ListViewTests
{
	[SampleControlInfoAttribute("ListView", "ListView_FirstLastCacheIndex", typeof(nVentive.Umbrella.Presentation.Light.ViewModelBase), description: "Demonstrates FirstCacheIndex and LastCacheIndex properties of ItemsStackPanel.")]
	public sealed partial class ListView_FirstLastCacheIndex : UserControl
	{
		public ListView_FirstLastCacheIndex()
		{
			this.InitializeComponent();

			MyListView.DataContext = new MyViewModel();
			this.Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
#if !XAMARIN_IOS && !NETSTANDARD2_0
			var sv = MyListView.FindFirstChild<Windows.UI.Xaml.Controls.ScrollViewer>();
			sv.ViewChanged += (o, e2) =>
			  {
				  // 
				  Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
				  {
					  var panel = MyListView.ItemsPanelRoot as ItemsStackPanel;
					  FirstCacheIndexTextBlock.Text = $"FirstCacheIndex: {panel.FirstCacheIndex}";
					  LastCacheIndexTextBlock.Text = $"LastCacheIndex: {panel.LastCacheIndex}";
				  });
			  };
#else
			FirstCacheIndexTextBlock.Text = "Not implemented";
			LastCacheIndexTextBlock.Text = "Not implemented";
#endif
		}

#if XAMARIN
		[Preserve(AllMembers = true)]
#endif
		public class MyViewModel
		{
			public int[] LotsOfNumbers { get; } = Enumerable.Range(0, 500).ToArray();
		}
	}
}
