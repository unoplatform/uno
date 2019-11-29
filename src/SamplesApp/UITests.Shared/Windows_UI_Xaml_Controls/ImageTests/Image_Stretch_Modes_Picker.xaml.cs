using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Uno.Extensions.Specialized;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ImageTests
{
	public sealed partial class Image_Stretch_Modes_Picker : UserControl
	{
		public ComboBoxItem[] StretchModes { get; } = GetValues<Stretch>().ToArray();
		public ComboBoxItem[] HorizontalAlignments { get; } = GetValues<HorizontalAlignment>().ToArray();
		public ComboBoxItem[] VerticalAlignments { get; } = GetValues<VerticalAlignment>().ToArray();

		public ObservableCollection<ModeItem> Items { get; } = new ObservableCollection<ModeItem>();

		private readonly ModeItem[] _allModes;

		public Image_Stretch_Modes_Picker()
		{
			this.InitializeComponent();

			stretchModes.ItemsSource = StretchModes;
			horizontalModes.ItemsSource = HorizontalAlignments;
			verticalModes.ItemsSource = VerticalAlignments;

			stretchModes.SelectionChanged += OnSelectionChanged;
			horizontalModes.SelectionChanged += OnSelectionChanged;
			verticalModes.SelectionChanged += OnSelectionChanged;

			_allModes = GetAllModes().ToArray();

			OnSelectionChanged(null, null);
		}

		private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var selectedStretch = (stretchModes.SelectedValue as ComboBoxItem)?.Tag as Stretch?;
			var selectedHorizontalModes = (horizontalModes.SelectedValue as ComboBoxItem)?.Tag as HorizontalAlignment?;
			var selectedVerticalMode = (verticalModes.SelectedValue as ComboBoxItem)?.Tag as VerticalAlignment?;

			Items.Clear();

			foreach (var mode in _allModes)
			{
				if(selectedStretch != null && selectedStretch != mode.Stretch)
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

				Items.Add(mode);
			}

			currentIndex.Text = Items.First().Index.ToString("00");
		}

		private IEnumerable<ModeItem> GetAllModes()
		{
			var index = 0;

			foreach (var stretch in StretchModes.Select(m=>m.Tag).OfType<Stretch>())
			{
				foreach (var horizontalAlignment in HorizontalAlignments.Select(m => m.Tag).OfType<HorizontalAlignment>())
				{
					foreach (var verticalAlignment in VerticalAlignments.Select(m => m.Tag).OfType<VerticalAlignment>())
					{
						yield return new ModeItem(index++, stretch, horizontalAlignment, verticalAlignment);
					}
				}
			}
		}

		private static IEnumerable<ComboBoxItem> GetValues<T>() where T : Enum
		{
			yield return new ComboBoxItem {Content = $"All {typeof(T).Name} Modes", Tag = null, IsSelected = true};

			foreach (T t in Enum.GetValues(typeof(T)))
			{
				yield return new ComboBoxItem {Content = t.ToString(), Tag = t};
			}
		}

		private void OnPrevious(object sender, RoutedEventArgs e)
		{
			var currentIndex = Items.First().Index;
			var nextIndex = (currentIndex + _allModes.Length - 1) % _allModes.Length;
			SelectMode(_allModes[nextIndex]);
		}

		private void OnNext(object sender, RoutedEventArgs e)
		{
			var currentIndex = Items.First().Index;
			var nextIndex = (currentIndex + _allModes.Length + 1) % _allModes.Length;
			SelectMode(_allModes[nextIndex]);
		}

		private void SelectMode(ModeItem mode)
		{
			stretchModes.SelectedIndex = StretchModes.Select(m => m.Tag).IndexOf(mode.Stretch);
			horizontalModes.SelectedIndex = HorizontalAlignments.Select(m => m.Tag).IndexOf(mode.HorizontalAlignment);
			verticalModes.SelectedIndex = VerticalAlignments.Select(m => m.Tag).IndexOf(mode.VerticalAlignment);
		}

		public class ModeItem
		{
			public int Index { get; }
			public Stretch Stretch { get; }
			public HorizontalAlignment HorizontalAlignment { get; }
			public VerticalAlignment VerticalAlignment { get; }

			public ModeItem(int index, Stretch stretch, HorizontalAlignment horizontalAlignment, VerticalAlignment verticalAlignment)
			{
				Index = index;
				Stretch = stretch;
				HorizontalAlignment = horizontalAlignment;
				VerticalAlignment = verticalAlignment;
			}
		}
	}
}
