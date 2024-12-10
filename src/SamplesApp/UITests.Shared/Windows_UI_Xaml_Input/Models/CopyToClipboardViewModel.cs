using System.Windows.Input;
using Windows.UI.Core;
using Windows.UI.Xaml.Data;
using Uno.UI.Samples.UITests.Helpers;
using Windows.ApplicationModel.DataTransfer;

using ICommand = System.Windows.Input.ICommand;
using EventHandler = System.EventHandler;

namespace UITests.Shared.Windows_UI_Xaml_Input.Models
{
	[Bindable]
	internal class CopyToClipboardViewModel : ViewModelBase
	{
		public CopyToClipboardViewModel(Private.Infrastructure.UnitTestDispatcherCompat dispatcher) : base(dispatcher)
		{
		}

		private string _text;

		public string Text
		{
			get => _text;
			set
			{
				_text = value;
				RaisePropertyChanged();
			}
		}

		public ICommand Copy => GetOrCreateCommand(ExecuteCopy);

		private void ExecuteCopy()
		{
			DataPackage dataPackage = null;
			dataPackage.SetText(Text);
			Clipboard.SetContent(dataPackage);
		}
	}
}
