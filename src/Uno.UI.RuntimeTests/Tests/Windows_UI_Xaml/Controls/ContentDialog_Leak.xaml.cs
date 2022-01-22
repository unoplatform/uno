using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls
{
	public partial class ContentDialog_Leak : ContentDialog, IExtendedLeakTest
	{
		public ContentDialog_Leak()
		{
			InitializeComponent();
		}


		public async Task WaitForTestToComplete()
		{
			var t = ShowAsync();

			await Task.Yield();
			await Task.Yield();

			t.Cancel();

			Hide();

			await Task.Yield();
		}
	}
}
