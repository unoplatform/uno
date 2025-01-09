using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class xLoad_Back_And_Forth : Page
	{
		public xLoad_Back_And_Forth()
		{
			this.InitializeComponent();
			NavView.MenuItemsSource = new List<xLoadBackAndForthVM> { new(), new() };
		}
	}

	public class xLoadBackAndForthVM : INotifyPropertyChanged
	{
		private bool _isSelected;
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

		public bool IsSelected
		{
			get => _isSelected;
			set => SetField(ref _isSelected, value);
		}
	}
}
