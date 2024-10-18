using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;

namespace Uno.UI.Tests.Windows_UI_Xaml.FrameworkElementTests
{
	[TestClass]
	[RunsOnUIThread]
	public partial class Given_FrameworkElement
	{
		[TestMethod]
		public void When_LayoutUpdated()
		{
			var SUT = new Grid();

			var item1 = new Border();

			var sutLayoutUpdatedCount = 0;

			SUT.LayoutUpdated += delegate
			{
				sutLayoutUpdatedCount++;
			};

			var item1LayoutUpdatedCount = 0;
			item1.LayoutUpdated += delegate
			{
				item1LayoutUpdatedCount++;
			};

			SUT.Children.Add(item1);

			SUT.Measure(new Size(1, 1));
			SUT.Arrange(new Rect(0, 0, 1, 1));

			var sutLayoutUpdate1 = sutLayoutUpdatedCount;
			var item1LayoutUpdate1 = item1LayoutUpdatedCount;

			SUT.Measure(new Size(2, 2));
			SUT.Arrange(new Rect(0, 0, 2, 2));

			var sutLayoutUpdate2 = sutLayoutUpdatedCount;
			var item1LayoutUpdate2 = item1LayoutUpdatedCount;

			SUT.Arrange(new Rect(0, 0, 2, 2));

#if UNO_HAS_ENHANCED_LIFECYCLE || WINAPPSDK
			var sutLayoutUpdate3 = sutLayoutUpdatedCount;
			var item1LayoutUpdate3 = item1LayoutUpdatedCount;

			TestServices.WindowHelper.EmbeddedTestRoot.control.InvalidateMeasure();
			TestServices.WindowHelper.EmbeddedTestRoot.control.UpdateLayout();
			var sutLayoutUpdate4 = sutLayoutUpdatedCount;

			TestServices.WindowHelper.EmbeddedTestRoot.control.UpdateLayout();
			var sutLayoutUpdate5 = sutLayoutUpdatedCount;
#endif

			using (new AssertionScope())
			{
#if __ANDROID__
				// Android has an issue where LayoutUpdate is called twice, caused by the presence
				// of two calls to arrange (Arrange, ArrangeElement(this)) in FrameworkElement.
				// Failing to call the first Arrange makes some elements fail to have a proper size in
				// some yet unknown conditions.
				// Issue: https://github.com/unoplatform/uno/issues/2769
				sutLayoutUpdate1.Should().Be(2, "sut-before");
				sutLayoutUpdate2.Should().Be(4, "sut-after");
#elif UNO_HAS_ENHANCED_LIFECYCLE || WINAPPSDK
				sutLayoutUpdate1.Should().Be(0, "sut-1");
				sutLayoutUpdate2.Should().Be(0, "sut-2");
				sutLayoutUpdate3.Should().Be(0, "sut-3");
				sutLayoutUpdate4.Should().Be(1, "sut-4");
				sutLayoutUpdate5.Should().Be(1, "sut-5");
#else
				sutLayoutUpdate1.Should().Be(1, "sut-before");
				sutLayoutUpdate2.Should().Be(2, "sut-after");
#endif

#if __ANDROID__
				item1LayoutUpdate1.Should().Be(1, "item1-before");
				item1LayoutUpdate2.Should().Be(2, "item1-after");
#endif
			}
		}

		[TestMethod]
#if !UNO_HAS_ENHANCED_LIFECYCLE
		[Ignore("Properly works only with enhanced lifecycle")]
#endif
		public async Task When_LayoutUpdated_Not_In_Visual_Tree()
		{
			string s = "";
			var button = new Button();
			button.LayoutUpdated += (sender, args) =>
			{
				Assert.IsNull(sender);
				Assert.IsNull(args);
				s += "button1 ";
			};
			button.LayoutUpdated += (sender, args) =>
			{
				Assert.IsNull(sender);
				Assert.IsNull(args);
				s += "button2 ";
			};

			var border = new Border
			{
				Width = 100,
				Height = 100,
				Background = new SolidColorBrush(Windows.UI.Colors.Red),
			};

			border.LayoutUpdated += (sender, args) =>
			{
				Assert.IsNull(sender);
				Assert.IsNull(args);
				s += "border1 ";
			};

			border.LayoutUpdated += (sender, args) =>
			{
				Assert.IsNull(sender);
				Assert.IsNull(args);
				s += "border2 ";
			};

			await UITestHelper.Load(border);

			// On WinUI, running the test randomly produces one of these outputs.
			if (s is not ("button1 button2 border1 border2 " or "border1 border2 button1 button2 "))
			{
				Assert.Fail($"Test failed. Actual: {s}");
			}
		}

		[TestMethod]
#if __WASM__
		[Ignore("Fails for unknown reason")]
#endif
		public async Task When_LayoutUpdated_Should_Not_Keep_Elements_Alive()
		{
			var wr = GetWeakReference();

			for (int i = 0; i < 10; i++)
			{
				GC.Collect();
				GC.WaitForPendingFinalizers();
			}

			await TestServices.WindowHelper.WaitForIdle();
			Assert.IsFalse(wr.TryGetTarget(out _));

			[MethodImpl(MethodImplOptions.NoInlining)]
			static WeakReference<Button> GetWeakReference()
			{
				var x = new Button();
				x.LayoutUpdated += (_, _) => x.Content = "Hello";
				return new(x);
			}
		}

		[TestMethod]
		public void When_MaxWidth_NaN()
		{
			Assert.ThrowsException<ArgumentException>(() => new ContentControl
			{
				MaxWidth = double.NaN,
				MaxHeight = double.NaN,
				Content = new Border { Width = 10, Height = 15 }
			});
		}

#if HAS_UNO
		[TestMethod]
		public void When_SuppressIsEnabled()
		{
			var SUT = new MyEnabledTestControl();

			SUT.IsEnabled = true;

			SUT.PublicSuppressIsEnabled(true);
			Assert.IsFalse(SUT.IsEnabled);

			SUT.IsEnabled = false;
			Assert.IsFalse(SUT.IsEnabled);

			SUT.IsEnabled = true;
			Assert.IsFalse(SUT.IsEnabled);

			SUT.PublicSuppressIsEnabled(false);
			Assert.IsTrue(SUT.IsEnabled);
		}
#endif

		[TestMethod]
		public void When_DP_IsEnabled_Null()
		{
			var grid = new UserControl();

			grid.SetValue(Control.IsEnabledProperty, null);
		}
	}

#if HAS_UNO
	public partial class MyEnabledTestControl : ContentControl
	{
		public void PublicSuppressIsEnabled(bool suppress) => SuppressIsEnabled(suppress);
	}
#endif
}
