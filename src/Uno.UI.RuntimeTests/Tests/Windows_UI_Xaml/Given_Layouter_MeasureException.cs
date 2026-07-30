using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	// An element whose MeasureOverride throws must not be permanently prevented from being measured
	// again. The layouter sets the per-element MeasuringSelf flag before calling MeasureCore; if that
	// flag is not cleared when MeasureCore throws, InvalidateMeasure() becomes a no-op for that element
	// (and the dirtiness never propagates to its ancestors), so its DesiredSize can never be updated
	// again — a transient measure exception turns into a permanent layout freeze for the subtree.
	[TestClass]
	[RunsOnUIThread]
	public partial class Given_Layouter_MeasureException
	{
		private partial class ThrowingMeasureControl : FrameworkElement
		{
			public bool ThrowOnMeasure { get; set; }
			public Size MeasureResult { get; set; } = new Size(100, 100);

			protected override Size MeasureOverride(Size availableSize)
			{
				if (ThrowOnMeasure)
				{
					throw new InvalidOperationException("Simulated MeasureOverride failure");
				}

				return MeasureResult;
			}
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
		public void When_MeasureThrowsOnce_Then_Recovers_On_Reinvalidate()
		{
			var sut = new ThrowingMeasureControl { MeasureResult = new Size(100, 100) };

			sut.Measure(new Size(1000, 1000));
			Assert.AreEqual(new Size(100, 100), sut.DesiredSize, "baseline measure should produce 100x100");

			// A measure that throws must not leave the element permanently un-measurable.
			sut.ThrowOnMeasure = true;
			sut.InvalidateMeasure();
			Assert.ThrowsExactly<InvalidOperationException>(() => sut.Measure(new Size(1000, 1000)));

			// Stop throwing, request a new size, re-invalidate, measure again.
			sut.ThrowOnMeasure = false;
			sut.MeasureResult = new Size(300, 300);
			sut.InvalidateMeasure();
			sut.Measure(new Size(1000, 1000));

			Assert.AreEqual(new Size(300, 300), sut.DesiredSize, "element must re-measure after the throwing pass");
		}

		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.Native)]
		public async System.Threading.Tasks.Task When_ChildThrows_DuringResize_Then_Parent_Recovers()
		{
			var child = new ThrowingMeasureControl { MeasureResult = new Size(50, 50) };
			var parent = new Grid { Width = 1920, Height = 1080 };
			parent.Children.Add(child);

			try
			{
				await UITestHelper.Load(parent);
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(1920d, parent.ActualWidth, 1d, "baseline parent width");

				// Resize down while the child throws, forcing a synchronous layout pass so the
				// exception is deterministically observed rather than swallowed by the dispatcher.
				child.ThrowOnMeasure = true;
				parent.Width = 390;
				parent.Height = 844;
				Assert.ThrowsExactly<InvalidOperationException>(() => parent.UpdateLayout());

				// The child stops throwing and the parent is resized again; it must re-measure.
				child.ThrowOnMeasure = false;
				parent.Width = 768;
				parent.Height = 1024;
				parent.InvalidateMeasure();
				await WindowHelper.WaitForIdle();

				Assert.AreEqual(768d, parent.ActualWidth, 1d, "parent must re-measure to the new width after the child stops throwing");
			}
			finally
			{
				WindowHelper.WindowContent = null;
			}
		}
	}
}
