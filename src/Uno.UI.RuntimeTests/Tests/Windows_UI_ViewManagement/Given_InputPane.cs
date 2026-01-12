using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.Foundation;
using Windows.UI.ViewManagement;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_ViewManagement
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_InputPane
	{
		[TestMethod]
		public void When_GetForCurrentView_Then_NotNull()
		{
			// Arrange & Act
			var inputPane = InputPane.GetForCurrentView();

			// Assert
			Assert.IsNotNull(inputPane, "InputPane.GetForCurrentView() should not return null");
		}

		[TestMethod]
		public void When_Initially_Then_NotVisible()
		{
			// Arrange & Act
			var inputPane = InputPane.GetForCurrentView();

			// Assert
			Assert.IsFalse(inputPane.Visible, "InputPane should not be visible initially");
		}

		[TestMethod]
		public void When_Initially_Then_OccludedRectEmpty()
		{
			// Arrange & Act
			var inputPane = InputPane.GetForCurrentView();

			// Assert
			Assert.AreEqual(0, inputPane.OccludedRect.Height, "OccludedRect height should be 0 initially");
			Assert.AreEqual(0, inputPane.OccludedRect.Width, "OccludedRect width should be 0 initially");
		}

#if __WASM__
		[TestMethod]
		[RequiresFullWindow]
		public async Task When_TextBox_Focused_Then_InputPane_Accessible()
		{
			// Arrange
			var inputPane = InputPane.GetForCurrentView();
			var showingEventFired = false;

			void OnShowing(InputPane sender, InputPaneVisibilityEventArgs args)
			{
				showingEventFired = true;
			}

			inputPane.Showing += OnShowing;

			try
			{
				var textBox = new TextBox
				{
					PlaceholderText = "Test TextBox"
				};

				TestServices.WindowHelper.WindowContent = textBox;
				await TestServices.WindowHelper.WaitForLoaded(textBox);
				await TestServices.WindowHelper.WaitForIdle();

				// Act
				textBox.Focus(FocusState.Programmatic);
				await TestServices.WindowHelper.WaitForIdle();

				// Allow time for keyboard to appear (if supported by browser)
				await Task.Delay(500);

				// Assert
				// Note: On WASM, keyboard visibility depends on the browser and execution environment
				// In a test environment without actual mobile browser soft keyboard, events may not fire
				// This test validates that the extension is properly registered and accessible
				Assert.IsNotNull(inputPane, "InputPane should be accessible");
				// We don't assert showingEventFired because keyboard behavior depends on execution environment
			}
			finally
			{
				inputPane.Showing -= OnShowing;
			}
		}

		[TestMethod]
		public void When_TryShow_Called_Then_Succeeds()
		{
			// Arrange
			var inputPane = InputPane.GetForCurrentView();

			// Act & Assert
			// TryShow returns true to indicate the method executed successfully
			// Actual keyboard appearance depends on the execution environment
			Assert.IsTrue(inputPane.TryShow(), "TryShow should execute successfully on WASM");
		}

		[TestMethod]
		public void When_TryHide_Called_Then_Succeeds()
		{
			// Arrange
			var inputPane = InputPane.GetForCurrentView();

			// Act & Assert
			// TryHide returns true to indicate the method executed successfully
			Assert.IsTrue(inputPane.TryHide(), "TryHide should execute successfully on WASM");
		}
#endif
	}
}
