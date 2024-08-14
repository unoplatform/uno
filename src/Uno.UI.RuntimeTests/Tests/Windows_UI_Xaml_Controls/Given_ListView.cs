using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System.Linq;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls
{
	[TestClass]
	public class Given_ListView
	{
		[TestMethod]
		[RunsOnUIThread]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is only supported on Skia #14948")]
#endif
		public async Task When_ItemClicked_SelectsCorrectIndex()
		{
			var items = new object[] { "Item 1", "Item 2", "Item 3" };
			var loggingSelectionInfo = new LoggingSelectionInfo();
			var listViewBase = new ListViewBase
			{
				ItemsSource = items,
			};

			TestServices.WindowHelper.WindowContent = listViewBase;
			await TestServices.WindowHelper.WaitForLoaded(listViewBase);

			// We don't use ActualWidth because of https://github.com/unoplatform/uno/issues/15982
			var tapTarget = listViewBase.TransformToVisual(null).TransformPoint(new Point(112 * 0.9, listViewBase.ActualHeight / 2));
			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			finger.Press(tapTarget);
			finger.Release();

			Assert.AreEqual(0, listViewBase.SelectedIndex);
			Assert.IsTrue(loggingSelectionInfo.MethodLog.All(m => !m.Contains("MoveCurrent", StringComparison.OrdinalIgnoreCase)));
		}
	}
}
