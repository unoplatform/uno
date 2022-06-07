using System.Threading.Tasks;
using Private.Infrastructure;
using Windows.UI.Xaml;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	public class Given_DispatcherTimer
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_No_Interval_Set()
		{
			var dispatcherTimer = new DispatcherTimer();
			try
			{
				int tickCounter = 0;
				dispatcherTimer.Tick += (s, e) =>
				{
					tickCounter++;
				};
				dispatcherTimer.Start();
				await TestServices.WindowHelper.WaitFor(() => tickCounter > 0);
				await TestServices.WindowHelper.WaitFor(() => tickCounter > 5);
			}
			finally
			{
				dispatcherTimer.Stop();
			}
		}
	}
}
