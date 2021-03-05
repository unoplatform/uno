using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.MUX.Helpers
{
	internal static class DateTimePickerHelper
	{
		internal static async Task OpenDateTimePicker(DatePicker dateTimePicker)
		{
			Button button = default;

			await RunOnUIThread.ExecuteAsync(() =>
			{
				button = TreeHelper.GetVisualChildByName(dateTimePicker, "FlyoutButton") as Button;
			});

			ControlHelper.DoClickUsingTap(button);
			await TestServices.WindowHelper.WaitForIdle();
		}
	}
}
