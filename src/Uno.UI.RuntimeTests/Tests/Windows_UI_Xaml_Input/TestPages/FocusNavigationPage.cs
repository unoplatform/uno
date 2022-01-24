using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Input.TestPages
{
	public class FocusNavigationPage : Page
	{
		private TaskCompletionSource<object> _loadingTaskCompletionSource =
			new TaskCompletionSource<object>();

		public FocusNavigationPage()
		{
			Loaded += Page_Loaded;
		}

		private void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			_loadingTaskCompletionSource.SetResult(null);
		}

		public Task FinishedLoadingTask => _loadingTaskCompletionSource.Task;
	}
}
