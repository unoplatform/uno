using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Input.TestPages
{
	public partial class FocusNavigationPage : Page
	{
		private TaskCompletionSource<object> _loadingTaskCompletionSource =
			new TaskCompletionSource<object>();

		public FocusNavigationPage()
		{
			Loaded += Page_Loaded;
			Unloaded += Page_Unloaded;
		}

		public Task FinishedLoadingTask => _loadingTaskCompletionSource.Task;

		private void Page_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			_loadingTaskCompletionSource.SetResult(null);
		}

		private void Page_Unloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			// Reset loading task for next navigation
			_loadingTaskCompletionSource = new TaskCompletionSource<object>();
		}
	}
}
