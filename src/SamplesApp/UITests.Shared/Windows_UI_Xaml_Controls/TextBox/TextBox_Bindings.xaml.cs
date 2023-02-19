using System.ComponentModel;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_Xaml_Controls.TextBox
{
	[Sample(IgnoreInSnapshotTests = true)]
	public sealed partial class TextBox_Bindings : Page
	{
		public TextBox_Bindings()
		{
			this.InitializeComponent();
			DataContext = new TextBox_Bindings_Context();
		}

	}

	[Windows.UI.Xaml.Data.Bindable]
	internal class TextBox_Bindings_Context : INotifyPropertyChanged
	{
		private string _text;

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
