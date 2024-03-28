using System.Threading.Tasks;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;

namespace UITests.Windows_UI_ViewManagement
{
	[SampleControlInfo(category: "Windows.UI.ViewManagement")]

	public sealed partial class ApplicationViewMode : Page
	{
		public ApplicationViewMode()
		{
			this.InitializeComponent();

			SizeChanged += async (o, args) =>
			{
				await Task.Delay(1200);
				CheckMode();
			};

			CheckMode();
		}

		private void CheckMode()
		{
			mode.Text = ApplicationView.GetForCurrentView().ViewMode.ToString();

		}
	}
}
