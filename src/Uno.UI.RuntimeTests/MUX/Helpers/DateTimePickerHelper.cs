using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Windows.UI.Xaml;

namespace Uno.UI.RuntimeTests.MUX.Helpers
{
	internal static class DateTimePickerHelper
	{
		internal static async Task OpenDateTimePicker(FrameworkElement dateTimePicker)
		{
			Button button = default;

			await RunOnUIThread.ExecuteAsync(() =>
			{
				button = TreeHelper.GetVisualChildByName(dateTimePicker, "FlyoutButton") as Button;
			});

			await ControlHelper.DoClickUsingTap(button);
			await TestServices.WindowHelper.WaitForIdle();
		}
	}
}
