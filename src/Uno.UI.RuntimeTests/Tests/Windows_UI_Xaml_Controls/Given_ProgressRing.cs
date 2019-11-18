using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	class Given_ProgressRing
	{
		private async Task Dispatch(DispatchedHandler p)
		{
#if !NETFX_CORE
			await CoreApplication.GetCurrentView().Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, p);
#else
			await CoreApplication.MainView.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, p);
#endif
		}

		[TestMethod]
		public async Task When_ProgressRing_Collapsed()
		{
			await Dispatch(() =>
			{
				var SUT = new ProgressRing
				{
					Visibility = Visibility.Collapsed
				};

				var spacerBorder = new Border
				{
					Width = 10,
					Height = 10,
					Margin = new Thickness(5)
				};

				var root = new Grid
				{
					Children =
					{
						spacerBorder,
						SUT
					}
				};

				root.Measure(new Size(1000, 1000));
				Assert.AreEqual(10d + 5d + 5d, root.DesiredSize.Height);
			});
		}
	}
}
