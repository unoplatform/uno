using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using SamplesApp.Windows_UI_Xaml_Controls.ListView;
using Uno.Extensions.Specialized;

namespace Uno.UI.Samples.UITests.Image
{
	public sealed partial class Image_Stretch_Modes_Picker : UserControl
	{
		public ComboBoxItem[] StretchModes { get; } = GetValues<Stretch>().ToArray();
		public ComboBoxItem[] HorizontalAlignments { get; } = GetValues<HorizontalAlignment>().ToArray();
		public ComboBoxItem[] VerticalAlignments { get; } = GetValues<VerticalAlignment>().ToArray();

		public static DependencyProperty ModeProperty { get; } = DependencyProperty.Register(
			"Mode", typeof(string), typeof(Image_Stretch_Modes_Picker), new PropertyMetadata(default(string), ModeChanged));

		public string Mode
		{
			get { return (string)GetValue(ModeProperty); }
			set { SetValue(ModeProperty, value); }
		}

		public static DependencyProperty ItemsProperty { get; } = DependencyProperty.Register(
			"Items",
			typeof(List<StretchModeItem>),
			typeof(Image_Stretch_Modes_Picker),
			new PropertyMetadata(default(List<StretchModeItem>)));

		private static void ModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var picker = d as Image_Stretch_Modes_Picker;
			var modeStr = e.NewValue as string ?? "";
			if (modeStr.Length >= 1)
			{
				switch (modeStr[0])
				{
					case 'S':
						picker.OnNextStretch(null, null);
						break;
					case 'H':
						picker.OnNextHorizontal(null, null);
						break;
					case 'V':
						picker.OnNextVertical(null, null);
						break;
				}
			}
		}

		public List<StretchModeItem> Items
		{
			get { return (List<StretchModeItem>)GetValue(ItemsProperty); }
			set { SetValue(ItemsProperty, value); }
		}

		private readonly StretchModeItem[] _allModes;
		private bool _suspend;

		public Image_Stretch_Modes_Picker()
		{
			this.InitializeComponent();

			stretchModes.ItemsSource = StretchModes;
			horizontalModes.ItemsSource = HorizontalAlignments;
			verticalModes.ItemsSource = VerticalAlignments;

			stretchModes.SelectedIndex = 0;
			horizontalModes.SelectedIndex = 0;
			verticalModes.SelectedIndex = 0;

			stretchModes.SelectionChanged += OnSelectionChanged;
			horizontalModes.SelectionChanged += OnSelectionChanged;
			verticalModes.SelectionChanged += OnSelectionChanged;

			_allModes = GetAllModes().ToArray();

			OnSelectionChanged(null, null);
		}

		private async void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (_suspend)
			{
				return;
			}

			await Task.Yield();  // requeue on dispatcher

			var selectedStretch = (stretchModes.SelectedValue as ComboBoxItem)?.Tag as Stretch?;
			var selectedHorizontalModes = (horizontalModes.SelectedValue as ComboBoxItem)?.Tag as HorizontalAlignment?;
			var selectedVerticalMode = (verticalModes.SelectedValue as ComboBoxItem)?.Tag as VerticalAlignment?;

			var items = new List<StretchModeItem>();

			foreach (var mode in _allModes)
			{
				if (selectedStretch != null && selectedStretch != mode.Stretch)
				{
					continue;
				}

				if (selectedHorizontalModes != null && selectedHorizontalModes != mode.HorizontalAlignment)
				{
					continue;
				}

				if (selectedVerticalMode != null && selectedVerticalMode != mode.VerticalAlignment)
				{
					continue;
				}

				items.Add(mode);
			}

			Items = items;

			currentMode.Content = Items.First().Index.ToString("00");
		}

		private IEnumerable<StretchModeItem> GetAllModes()
		{
			var index = 0;

			foreach (var stretch in StretchModes.Select(m => m.Tag).OfType<Stretch>())
			{
				foreach (var horizontalAlignment in HorizontalAlignments.Select(m => m.Tag).OfType<HorizontalAlignment>())
				{
					foreach (var verticalAlignment in VerticalAlignments.Select(m => m.Tag).OfType<VerticalAlignment>())
					{
						yield return new StretchModeItem(index++, stretch, horizontalAlignment, verticalAlignment);
					}
				}
			}
		}

		private static IEnumerable<ComboBoxItem> GetValues<T>() where T : struct, Enum
		{
			yield return new ComboBoxItem { Content = $"All {typeof(T).Name} Modes", Tag = null };

			foreach (T t in Enum.GetValues<T>())
			{
				yield return new ComboBoxItem { Content = t.ToString(), Tag = t };
			}
		}

		private void OnPrevious(object sender, RoutedEventArgs e)
		{
			var currentIndex = Items.First().Index;
			var nextIndex = (currentIndex + _allModes.Length - 1) % _allModes.Length;
			SelectMode(_allModes[nextIndex]);
		}

		private void OnCurrent(object sender, RoutedEventArgs e)
		{
			SelectMode(Items.First());
		}

		private void OnNext(object sender, RoutedEventArgs e)
		{
			var currentIndex = Items.First().Index;
			var nextIndex = (currentIndex + _allModes.Length + 1) % _allModes.Length;
			SelectMode(_allModes[nextIndex]);
		}

		private void SelectMode(StretchModeItem mode)
		{
			_suspend = true;
			stretchModes.SelectedIndex = StretchModes.Select(m => m.Tag).IndexOf(mode.Stretch);
			horizontalModes.SelectedIndex = HorizontalAlignments.Select(m => m.Tag).IndexOf(mode.HorizontalAlignment);
			verticalModes.SelectedIndex = VerticalAlignments.Select(m => m.Tag).IndexOf(mode.VerticalAlignment);
			_suspend = false;
			OnSelectionChanged(null, null);
		}

		private void OnNextStretch(object sender, RoutedEventArgs e)
		{
			var nextIndex = (stretchModes.SelectedIndex + 1) % StretchModes.Length;
			stretchModes.SelectedIndex = nextIndex;
		}

		private void OnNextHorizontal(object sender, RoutedEventArgs e)
		{
			var nextIndex = (horizontalModes.SelectedIndex + 1) % HorizontalAlignments.Length;
			horizontalModes.SelectedIndex = nextIndex;
		}

		private void OnNextVertical(object sender, RoutedEventArgs e)
		{
			var nextIndex = (verticalModes.SelectedIndex + 1) % VerticalAlignments.Length;
			verticalModes.SelectedIndex = nextIndex;
		}
	}

	[Bindable]
	public class StretchModeItem
	{
		public int Index { get; }
		public Stretch Stretch { get; }
		public HorizontalAlignment HorizontalAlignment { get; }
		public VerticalAlignment VerticalAlignment { get; }

		public StretchModeItem(int index, Stretch stretch, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment)
		{
			Index = index;
			Stretch = stretch;
			HorizontalAlignment = horizontalAlignment;
			VerticalAlignment = verticalAlignment;
		}
	}
}
