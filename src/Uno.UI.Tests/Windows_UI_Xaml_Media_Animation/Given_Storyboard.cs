using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace Uno.UI.Tests.Windows_UI_Xaml_Media_Animation
{
	[TestClass]
	public class Given_Storyboard
	{
		[TestMethod]
		public void When_UsingCompletedCallback()
		{
			var completedCount = 0;

			void OnCompleted(object sender, object e)
			{
				completedCount++;
			}

			var sut = new Storyboard();
			sut.Completed += OnCompleted;

			completedCount.Should().Be(0);
			sut.Begin();

			sut.Dispatcher.ProcessEvents(CoreProcessEventsOption.ProcessAllIfPresent);

			sut.GetCurrentState().Should().Be(ClockState.Stopped);
			completedCount.Should().Be(1);
		}
	}
}
