using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Uno.UI.Samples.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UITests.Windows_UI_Xaml_Controls.ListView
{
	[Sample("ListView", Name = nameof(ListView_Selection_Events), Description = "The sequence of events when selecting an item should happen in the same order when compared to uwp.")]
	public sealed partial class ListView_Selection_Events : Page
	{
		private CustomViewModel ViewModel { get; } = new CustomViewModel();

		public ListView_Selection_Events()
		{
			this.InitializeComponent();
			SampleListView.SelectionChanged += (s, e) => AddLog(
				"LV.SelectionChanged: (Item|Value|Index): " +
				$"\n\t- lv:({Format(SampleListView.SelectedItem)}|{Format(SampleListView.SelectedValue)}|{Format(SampleListView.SelectedIndex)}), " +
				$"\n\t- vm:({Format(ViewModel.SelectedItem)}|{Format(ViewModel.SelectedValue)}|{Format(ViewModel.SelectedIndex)})"
			);
			ViewModel.PropertyChanged += (s, e) =>
			{
				AddLog($"VM.PropertyChanged: [{e.PropertyName}]->{Format(GetValueFrom(e.PropertyName))}");

				object GetValueFrom(string propertyName) => propertyName switch
				{
					nameof(CustomViewModel.SelectedItem) => ViewModel.SelectedItem,
					nameof(CustomViewModel.SelectedValue) => ViewModel.SelectedValue,
					nameof(CustomViewModel.SelectedIndex) => ViewModel.SelectedIndex,

					_ => throw new ArgumentOutOfRangeException(propertyName),
				};
			};

			string Format(object value) => value?.ToString() ?? "<null>";
			void AddLog(string log)
			{
				//LogListView.Items.Add(log);
				EventLogs.Text += (string.IsNullOrEmpty(EventLogs.Text) ? null : "\n") + log;
			}
		}
		private void SetSelectIndexTo0(object sender, RoutedEventArgs e)
		{
			SampleListView.SelectedIndex = 0;
		}
		private void ClearLogs(object sender, RoutedEventArgs e)
		{
			//LogListView.Items.Clear();
			EventLogs.Text = string.Empty;
		}

		// not using ViewModelBase because it would fire the same event twice, once as $"{propertyName}" and once as $"Item[{propertyName}]"...
		private class CustomViewModel : global::System.ComponentModel.INotifyPropertyChanged
		{
			public event global::System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

			public string[] Source { get; } = Enumerable.Range(0, 5).Select(x => $"Item_{x}").ToArray();

			#region SelectedItem
			private object _selectedItem;
			public object SelectedItem
			{
				get => _selectedItem;
				set => RaiseAndSetIfChanged(ref _selectedItem, value);
			}
			#endregion
			#region SelectedValue
			private object _selectedValue;
			public object SelectedValue
			{
				get => _selectedValue;
				set => RaiseAndSetIfChanged(ref _selectedValue, value);
			}
			#endregion
			#region SelectedIndex
			private int _selectedIndex;
			public int SelectedIndex
			{
				get => _selectedIndex;
				set => RaiseAndSetIfChanged(ref _selectedIndex, value);
			}
			#endregion

			protected void RaiseAndSetIfChanged<T>(ref T backingField, T value, [CallerMemberName] string propertyName = null)
			{
				if (!EqualityComparer<T>.Default.Equals(backingField, value))
				{
					backingField = value;
					PropertyChanged?.Invoke(this, new global::System.ComponentModel.PropertyChangedEventArgs(propertyName));
				}
			}
		}
	}
}
