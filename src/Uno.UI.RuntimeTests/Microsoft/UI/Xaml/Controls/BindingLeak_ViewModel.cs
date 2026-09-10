using System.ComponentModel;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls
{
	public class BindingLeak_ViewModel : INotifyPropertyChanged
	{
		private string _text = "Test";

		public string Text
		{
			get => _text;
			set
			{
				_text = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Text)));
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
