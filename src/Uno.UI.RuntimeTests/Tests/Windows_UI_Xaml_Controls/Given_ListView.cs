using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using Microsoft.UI.Xaml.Controls;
using System.Linq;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls
{
#if HAS_UNO
	[TestClass]
	public class Given_ListView
	{
		[TestMethod]
		[RunsOnUIThread]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_ItemClicked_SelectsCorrectIndex()
		{
			var items = new object[] { "Item 1", "Item 2", "Item 3" };
			var loggingSelectionInfo = new LoggingSelectionInfo(items);
			var listViewBase = new ListView
			{
				ItemsSource = loggingSelectionInfo,
			};

			TestServices.WindowHelper.WindowContent = listViewBase;
			await TestServices.WindowHelper.WaitForIdle();
			var tapTarget = listViewBase.TransformToVisual(null).TransformPoint(new Point(listViewBase.ActualWidth / 2, listViewBase.ActualHeight / 6));
			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			finger.Press(tapTarget);
			finger.Release();

			// Confirm that selection has been pushed to the source using the ISelectionInfo
			Assert.AreEqual(loggingSelectionInfo.IsSelected(0), true, "Item 0 should be selected now.");

			// Confirm that the selection HAS NOT been pushed using the `ICollectionView`
			Assert.AreEqual(loggingSelectionInfo.CurrentPosition, -1, "CurrentPosition should not have been updated.");
			Assert.AreEqual(loggingSelectionInfo.CurrentItem, null, "CurrentItem should be not have been updated.");
		}
	}
#endif
}
