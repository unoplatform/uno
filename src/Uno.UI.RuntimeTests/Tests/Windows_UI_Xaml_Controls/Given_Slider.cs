using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Shapes;
using MUXControlsTestApp.Utilities;
using Uno.Extensions;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Toolkit.DevTools.Input;
using Windows.UI.Input.Preview.Injection;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_Slider
	{
		[TestMethod]
		public async Task When_Value_At_Maximum()
		{
			var slider = new Slider { Minimum = 0, Maximum = 100, Value = 100, Orientation = Orientation.Horizontal, Width = 320, VerticalAlignment = VerticalAlignment.Stretch };
			WindowHelper.WindowContent = slider;

			await WindowHelper.WaitForIdle();

			var thumb = slider.FindFirstChild<Thumb>();

			Assert.IsNotNull(thumb);

			Assert.IsTrue(thumb.ActualWidth > 0);
			Assert.IsTrue(thumb.ActualHeight > 0);
		}

		[TestMethod]
		public async Task When_Slider_Constrained_Horizontal()
		{
			var slider = new MySlider { Minimum = 0, Maximum = 100, Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
			var container = new Grid { Width = 0, Height = 80 };
			container.Children.Add(slider);
			WindowHelper.WindowContent = container;

			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, slider.ActualWidth);
			Assert.AreEqual(0, slider.HorizontalDecreaseRect.Width);
			Assert.AreEqual(0, slider.HorizontalDecreaseRect.ActualWidth);
		}

		[TestMethod]
		public async Task When_Slider_Constrained_Vertical()
		{
			var slider = new MySlider { Minimum = 0, Maximum = 100, Orientation = Orientation.Vertical, HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
			var container = new Grid { Width = 80, Height = 0 };
			container.Children.Add(slider);
			WindowHelper.WindowContent = container;

			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, slider.ActualHeight);
			Assert.AreEqual(0, slider.VerticalDecreaseRect.Height);
			Assert.AreEqual(0, slider.VerticalDecreaseRect.ActualHeight);
		}

#if HAS_UNO
		[TestMethod]
		public async Task When_Value_Decimal()
		{
			var slider = new Slider()
			{
				Value = 0.5,
				Minimum = 0,
				Maximum = 1,
				StepFrequency = 0.01,
				Orientation = Orientation.Horizontal,
			};
			WindowHelper.WindowContent = slider;
			await WindowHelper.WaitForLoaded(slider);

			var thumb = VisualTreeUtils.FindVisualChildByName(slider, "HorizontalThumb");
			var toolTip = ToolTipService.GetToolTipReference(thumb);
			var tb = (TextBlock)toolTip.Content;
			var text = tb.Text;
			Assert.AreEqual("0.50", text);
		}
#endif

#if HAS_UNO
		[TestMethod]
#if !HAS_INPUT_INJECTOR
		[Ignore("InputInjector is not supported on this platform.")]
#endif
		public async Task When_Slider_Dragged()
		{
			var slider = new MySlider
			{
				Minimum = 0,
				Maximum = 100,
				Orientation = Orientation.Horizontal,
				HorizontalAlignment = HorizontalAlignment.Stretch,
				VerticalAlignment = VerticalAlignment.Stretch
			};
			var container = new Grid
			{
				Width = 80,
				Height = 80
			};
			container.Children.Add(slider);

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var mouse = injector.GetMouse();

			WindowHelper.WindowContent = container;
			await WindowHelper.WaitForIdle();

			mouse.Press(slider.GetAbsoluteBounds().GetCenter());
			await WindowHelper.WaitForIdle();

			slider.Value.Should().BeInRange(49, 52, "we dragged the thumb at the center of the slider");

			var clickableLength = slider.ActualWidth - slider.FindVisualChildByType<Thumb>().ActualWidth;

			mouse.MoveBy(clickableLength / 4, 0);

			slider.Value.Should().BeInRange(74, 76, "we dragged the thumb 1/4 of width on right");

			mouse.Release();
		}
#endif

		[TestMethod]
		public async Task When_Reloaded_With_Value_Binding()
		{
			var slider = new Slider
			{
				Minimum = 0,
				Maximum = 20,
				Orientation = Orientation.Horizontal,
			};
			var container = new Border
			{
				Width = 100,
				Height = 50,
				Child = slider
			};

			// cant repro with explicit value. we need to use binding,
			// with the dc flowing from the container, so it can "attach/detach" on load/unload
			slider.SetBinding(Slider.ValueProperty, new Binding());
			container.DataContext = 10;

			await UITestHelper.Load(container);
			var thumb = slider.FindFirstDescendantOrThrow<Thumb>("HorizontalThumb");

			// record thumb position
			await UITestHelper.WaitForIdle();
			var dx0 = thumb.TransformToVisual(slider).TransformPoint(default).X;

			// reload the slider
			container.Child = null;
			await UITestHelper.WaitForIdle();
			container.Child = slider;
			await UITestHelper.WaitForIdle();

			// check that thumb position is still the same
			var dx1 = thumb.TransformToVisual(slider).TransformPoint(default).X;
			Assert.AreEqual(dx0, dx1, delta: 1, message: "Thumb#HorizontalThumb position is no longer the same after reloading the slider");
		}
	}

	public partial class MySlider : Slider
	{
		public Rectangle HorizontalDecreaseRect { get; private set; }
		public Rectangle VerticalDecreaseRect { get; private set; }
		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			HorizontalDecreaseRect = GetTemplateChild("HorizontalDecreaseRect") as Rectangle;
			VerticalDecreaseRect = GetTemplateChild("VerticalDecreaseRect") as Rectangle;
		}
	}
}
