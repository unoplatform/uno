using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml;
using Windows.Storage.Pickers;

namespace UITests.Shared.Windows_Storage
{
	[SampleControlInfo("Windows.Storage")]
	public sealed partial class FolderPickerTests : Page
	{
		public FolderPickerTests()
		{
			this.InitializeComponent();
		}

		private async void PickFolder_Click(object sender, RoutedEventArgs args)
		{
			var picker = new FolderPicker();
			var result = await picker.PickSingleFolderAsync();
			if (result == null) {
				PickResult.Text = "No folder was picked";
			}
			else
			{
				PickResult.Text = result.Path;
			}
		}
	}
}
