using Uno;
using Uno.UI.Samples.Controls;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Extensions;
using Private.Infrastructure;

namespace SamplesApp.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView", "ListView_FirstLastCacheIndex", Description: "Demonstrates FirstCacheIndex and LastCacheIndex properties of ItemsStackPanel.")]
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
#if !__APPLE_UIKIT__ && !UNO_REFERENCE_API
			var sv = MyListView.FindFirstChild<ScrollViewer>();
			sv.ViewChanged += (o, e2) =>
			{
				var t = UnitTestDispatcherCompat.From(this).RunAsync(UnitTestDispatcherCompat.Priority.Normal, () =>
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
