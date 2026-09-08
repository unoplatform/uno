using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Data;
using Uno.UI.Samples.Controls;

namespace Uno.UI.Samples.Content.UITests.ContentPresenter
{
	[Sample("ContentPresenter", IsManualTest = true)]
	public sealed partial class ContentPresenter_NativeEmbedding_ZIndex : UserControl
	{
		public ContentPresenter_NativeEmbedding_ZIndex()
		{
			this.InitializeComponent();
			var state = new ToggleableState();
			sv.DataContext = state;
			var timer = new DispatcherTimer();
			timer.Interval = TimeSpan.FromSeconds(1);
			timer.Tick += (s, e) =>
			{
				state.State++;
			};
			timer.Start();
		}
	}

	public class ToggleableState : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
		{
			if (EqualityComparer<T>.Default.Equals(field, value)) return false;
			field = value;
			OnPropertyChanged(propertyName);
			return true;
		}

		private int _state;
		public int State { get => _state; set => SetField(ref _state, value); }
	}

	public class StateToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
			=> ((int)value) % 2 == 0 ? Visibility.Visible : Visibility.Collapsed;

		public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
	}

	public class StateToZIndexConverter : IValueConverter
	{
		private int _lastRet;
		public int Modulus { get; set; }
		public int Remainder { get; set; }
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return ((int)value) % Modulus == Remainder ? _lastRet = ((int)value) : _lastRet;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
	}
}
