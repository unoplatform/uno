using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Extensions;
using Windows.Foundation;
using Windows.Graphics.Display;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	[RequiresFullWindow]
#if __MACOS__
	[Ignore("Currently fails on macOS, part of #9282! epic")]
#endif
	public class Given_RelativePanel
	{
		[TestMethod]
		public async Task When_Child_Aligns_Horizontal_Center_With_Panel()
		{
			var SUT = new RelativePanel()
			{
				Name = "test",
				Width = 300,
				Height = 300
			};
			var border = new RelativePanelMeasuredControl(new Size(100, 100));
			border.SetValue(RelativePanel.AlignHorizontalCenterWithPanelProperty, true);

			SUT.Children.Add(border);
			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(new Rect(100, 0, 100, 100), border.GetRelativeBounds(SUT));
		}

		[TestMethod]
		public async Task When_Child_Aligns_Vertical_Center_With_Panel()
		{
			var SUT = new RelativePanel()
			{
				Name = "test",
				Width = 300,
				Height = 300
			};
			var border = new RelativePanelMeasuredControl(new Size(100, 100));
			border.SetValue(RelativePanel.AlignVerticalCenterWithPanelProperty, true);

			SUT.Children.Add(border);
			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(new Rect(0, 100, 100, 100), border.GetRelativeBounds(SUT));
		}

		[TestMethod]
		public async Task When_Child_Aligns_Two_Directions_Center_With_Panel()
		{
			var SUT = new RelativePanel()
			{
				Name = "test",
				Width = 300,
				Height = 300
			};
			var border = new RelativePanelMeasuredControl(new Size(100, 100));
			border.SetValue(RelativePanel.AlignVerticalCenterWithPanelProperty, true);
			border.SetValue(RelativePanel.AlignHorizontalCenterWithPanelProperty, true);

			SUT.Children.Add(border);
			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(new Rect(100, 100, 100, 100), border.GetRelativeBounds(SUT));
		}

		[TestMethod]
		public async Task When_NativeElement()
		{
			var SUT = new RelativePanel()
			{
				Name = "test",
				Width = 300,
				Height = 300
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForLoaded(SUT);
			await WindowHelper.WaitForIdle();

#if __ANDROID__
			var button = new Android.Widget.Button(ContextHelper.Current) { Text = "test" };
			SUT.Children.Add(button);

			await WindowHelper.WaitForIdle();

			Assert.AreEqual(48.0, button.MeasuredHeight / DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel);
			Assert.AreEqual(88.0, button.MeasuredWidth / DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel);
#elif __IOS__
			var button = new UIKit.UIButton();
			button.SetTitle("Test", UIKit.UIControlState.Normal);
			SUT.Children.Add(button);

			await WindowHelper.WaitForIdle();

			Assert.AreEqual(17.0, button.Frame.Width / DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel);
			Assert.AreEqual(17.0, button.Frame.Height / DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel);
#endif
		}
	}

	public partial class RelativePanelMeasuredControl : Control
	{
		private readonly Size _measureSize;

		public RelativePanelMeasuredControl(Size measureSize)
		{
			_measureSize = measureSize;
		}

		protected override Size MeasureOverride(Size availableSize) => _measureSize;
	}
}
