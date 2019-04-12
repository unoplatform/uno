using System.Windows.Input;
using Windows.UI.Core;
using Windows.UI.Xaml.Data;
using Uno.UI.Samples.UITests.Helpers;
using Windows.ApplicationModel.DataTransfer;

namespace UITests.Shared.Windows_UI_Xaml_Input.Models
{
	[Bindable]
    public class CopyToClipboardViewModel : ViewModelBase
	{
		public CopyToClipboardViewModel(CoreDispatcher dispatcher) : base(dispatcher)
		{
			Copy = CreateCommand(ExecuteCopy);
		}

		private string _text;

		public string Text
		{
			get => _text;
			set {
				_text = value;
				RaisePropertyChanged();
			}
		}

		public ICommand Copy { get; }

		private void ExecuteCopy()
		{
			DataPackage dataPackage = null;
			dataPackage.SetText(Text);
			Clipboard.SetContent(dataPackage);
		}
	}
}
