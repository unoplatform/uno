using System.Threading.Tasks;
using Windows.Foundation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml.Controls;
using Private.Infrastructure;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Shapes;
using System;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
    public partial class Given_UIElement
	{
#if HAS_UNO // Tests use IsArrangeDirty, which is an internal property
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Visible_InvalidateArrange()
		{
			var sut = new Border()
			{
				Width = 100,
				Height = 10
			};

			TestServices.WindowHelper.WindowContent = sut;
			await TestServices.WindowHelper.WaitForIdle();
			sut.InvalidateArrange();
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsFalse(sut.IsArrangeDirty);
		}

#if !__ANDROID__ && !__IOS__ // Fails on Android & iOS (issue #5002)
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Collapsed_InvalidateArrange()
		{
			var sut = new Border()
			{
				Width = 100,
				Height = 10,
				Visibility = Visibility.Collapsed
			};

			TestServices.WindowHelper.WindowContent = sut;
			await TestServices.WindowHelper.WaitForIdle();
			sut.InvalidateArrange();
			await TestServices.WindowHelper.WaitForIdle();

			await TestServices.WindowHelper.WaitFor(() => !sut.IsArrangeDirty);
		}
#endif
#endif

#if HAS_UNO // Issue #2840 - ActualSize is available since 18362
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_TextBlock_ActualSize()
		{
			Border border = new Border();
			TextBlock text = new TextBlock()
			{
				HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center,
				VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center,
				Text = "Short text"
			};
			border.Child = text;

			TestServices.WindowHelper.WindowContent = border;
			await TestServices.WindowHelper.WaitForIdle();

			border.UpdateLayout();

			await TestServices.WindowHelper.WaitFor(()=>Math.Abs(text.ActualWidth - text.ActualSize.X) < 0.01);
			await TestServices.WindowHelper.WaitFor(() => Math.Abs(text.ActualHeight - text.ActualSize.Y) < 0.01);

			text.Text = "This is a longer text";
			border.UpdateLayout();

			await TestServices.WindowHelper.WaitFor(() => Math.Abs(text.ActualWidth - text.ActualSize.X) < 0.01);
			await TestServices.WindowHelper.WaitFor(() => Math.Abs(text.ActualHeight - text.ActualSize.Y) < 0.01);
		}

		public async Task When_Rectangle_Set_ActualSize()
		{
			Border border = new Border();

			Rectangle rectangle = new Rectangle()
			{
				HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center,
				VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center,
				Width = 42,
				Height = 24,
			};
			border.Child = rectangle;

			TestServices.WindowHelper.WindowContent = border;
			await TestServices.WindowHelper.WaitForIdle();

			border.UpdateLayout();

			await TestServices.WindowHelper.WaitFor(() => Math.Abs(rectangle.ActualWidth - rectangle.ActualSize.X) < 0.01);
			await TestServices.WindowHelper.WaitFor(() => Math.Abs(rectangle.ActualHeight - rectangle.ActualSize.Y) < 0.01);

			rectangle.Width = 16;
			rectangle.Height = 32;
			border.UpdateLayout();

			await TestServices.WindowHelper.WaitFor(() => Math.Abs(rectangle.ActualWidth - rectangle.ActualSize.X) < 0.01);
			await TestServices.WindowHelper.WaitFor(() => Math.Abs(rectangle.ActualHeight - rectangle.ActualSize.Y) < 0.01);
		}
#endif

#if HAS_UNO // Cannot Set the LayoutInformation on UWP
		[TestMethod]
		[RunsOnUIThread]
		public void When_UpdateLayout_Then_TreeNotMeasuredUsingCachedValue()
		{
			if (Window.Current.RootElement is Panel root)
			{
				var sut = new Grid();

				var originalRootAvailableSize = LayoutInformation.GetAvailableSize(root);
				var originalRootDesiredSize = LayoutInformation.GetDesiredSize(root);
				var originalRootLayoutSlot = LayoutInformation.GetLayoutSlot(root);

				Size availableSize, desiredSize;
				Rect layoutSlot;
				try
				{
					LayoutInformation.SetAvailableSize(root, default);
					LayoutInformation.SetDesiredSize(root, default);
					LayoutInformation.SetLayoutSlot(root, default);

					root.Children.Add(sut);
					sut.UpdateLayout();

					availableSize = LayoutInformation.GetAvailableSize(sut);
					desiredSize = LayoutInformation.GetDesiredSize(sut);
					layoutSlot = LayoutInformation.GetLayoutSlot(sut);
				}
				finally
				{
					LayoutInformation.SetAvailableSize(root, originalRootAvailableSize);
					LayoutInformation.SetDesiredSize(root, originalRootDesiredSize);
					LayoutInformation.SetLayoutSlot(root, originalRootLayoutSlot);

					root.Children.Remove(sut);
					try { root.UpdateLayout(); } catch { } // Make sure to restore visual tree if test has failed!
				}

				Assert.AreNotEqual(default, availableSize);
#if !__IOS__ // Measure arrange are async on iOS!
				Assert.AreNotEqual(default, desiredSize);
				Assert.AreNotEqual(default, layoutSlot);
#endif
			}
			else
			{
				Assert.Inconclusive("The RootElement is not a Panel");
			}
		}
#endif
	}
}
