using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.Repeater;

[Sample(
	"ItemsRepeater",
	IsManualTest = true,
	Description = "Scroll a list with very variable item heights — items must not overlap, drift, or crop the top after walking up and down. Tall items (1200px) are interleaved every 10th index between short ones (40px); the list is anchored to the bottom like a chat view.")]
public sealed partial class ItemsRepeaterVariableHeights : Page
{
	private const int DefaultItemCount = 200;
	private const double TallHeight = 1200;
	private const double ShortHeight = 40;
	private const double GrownHeight = 1500;

	private static readonly Brush s_tallBrush = new SolidColorBrush(Colors.LightSteelBlue);
	private static readonly Brush s_shortBrush = new SolidColorBrush(Colors.LightGray);

	private readonly ObservableCollection<ItemModel> _items;

	public ItemsRepeaterVariableHeights()
	{
		this.InitializeComponent();
		_items = new ObservableCollection<ItemModel>(BuildItems(DefaultItemCount));
		Repeater.ItemsSource = _items;
	}

	private static IEnumerable<ItemModel> BuildItems(int count) =>
		Enumerable.Range(0, count).Select(i => new ItemModel(
			i,
			IsTall(i) ? TallHeight : ShortHeight,
			IsTall(i) ? s_tallBrush : s_shortBrush));

	private static bool IsTall(int index) => index % 10 == 3;

	private void OnTopClick(object sender, RoutedEventArgs e) =>
		Scroller.ChangeView(null, 0, null, disableAnimation: true);

	private void OnJump5000Click(object sender, RoutedEventArgs e) =>
		Scroller.ChangeView(null, 5000, null, disableAnimation: true);

	private void OnJump12000Click(object sender, RoutedEventArgs e) =>
		Scroller.ChangeView(null, 12000, null, disableAnimation: true);

	private void OnBottomClick(object sender, RoutedEventArgs e) =>
		Scroller.ChangeView(null, Scroller.ScrollableHeight, null, disableAnimation: true);

	private async void OnWalkDownUpClick(object sender, RoutedEventArgs e)
	{
		const int Steps = 30;
		var bottom = Scroller.ScrollableHeight;
		for (var i = 1; i <= Steps; i++)
		{
			Scroller.ChangeView(null, bottom * i / Steps, null, disableAnimation: true);
			await YieldAsync();
		}
		for (var i = Steps - 1; i >= 1; i--)
		{
			Scroller.ChangeView(null, bottom * i / Steps, null, disableAnimation: true);
			await YieldAsync();
		}
		Scroller.ChangeView(null, 0, null, disableAnimation: true);
	}

	private void OnGrowItem5Click(object sender, RoutedEventArgs e)
	{
		if (_items.Count > 5)
		{
			_items[5].Height = GrownHeight;
		}
	}

	private void OnResetClick(object sender, RoutedEventArgs e)
	{
		for (var i = 0; i < _items.Count; i++)
		{
			_items[i].Height = IsTall(i) ? TallHeight : ShortHeight;
		}
		Scroller.ChangeView(null, Scroller.ScrollableHeight, null, disableAnimation: true);
	}

	private static Task YieldAsync() => Task.Delay(16);

	public sealed class ItemModel : INotifyPropertyChanged
	{
		private double _height;

		public ItemModel(int index, double height, Brush background)
		{
			Index = index;
			_height = height;
			Background = background;
		}

		public int Index { get; }

		public Brush Background { get; }

		public double Height
		{
			get => _height;
			set
			{
				if (_height != value)
				{
					_height = value;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Height)));
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
