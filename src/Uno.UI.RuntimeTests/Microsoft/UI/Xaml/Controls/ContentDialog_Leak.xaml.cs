using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls
{
	public partial class ContentDialog_Leak : ContentDialog, IExtendedLeakTest
	{
		public ContentDialog_Leak()
		{
			InitializeComponent();
			XamlRoot = TestServices.WindowHelper.XamlRoot;
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
