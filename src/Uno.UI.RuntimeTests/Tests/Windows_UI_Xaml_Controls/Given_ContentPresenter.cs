using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
[RequiresFullWindow]
#if __MACOS__
[Ignore("Currently fails on macOS, part of #9282! epic")]
#endif
public class Given_ContentPresenter
{
	[TestMethod]
	public void When_Content_Alignment_Set_Default_Alignment_Not_Overriden()
	{
		var contentPresenter = new ContentPresenter()
		{
			HorizontalContentAlignment = HorizontalAlignment.Center
		};
		var border = new Border();
		contentPresenter.Content = border;

		Assert.AreEqual(HorizontalAlignment.Center, contentPresenter.HorizontalContentAlignment);
		Assert.AreEqual(HorizontalAlignment.Stretch, border.HorizontalAlignment);
	}

	public static IEnumerable<object[]> GetAlignments()
	{
		var configurations = new List<AlignmentTestConfiguration>();
		// Centered content

		foreach (var outerHorizontalAlignment in Enum.GetValues(typeof(HorizontalAlignment)).OfType<HorizontalAlignment>())
		{
			foreach (var outerVerticalAlignment in Enum.GetValues(typeof(VerticalAlignment)).OfType<VerticalAlignment>())
			{
				foreach (var innerHorizontalAlignment in Enum.GetValues(typeof(HorizontalAlignment)).OfType<HorizontalAlignment>())
				{
					foreach (var innerVerticalAlignment in Enum.GetValues(typeof(VerticalAlignment)).OfType<VerticalAlignment>())
					{
						double expectedX = 0;
						if (outerHorizontalAlignment == HorizontalAlignment.Center)
						{
							expectedX = 50;
						}
						else if (outerHorizontalAlignment == HorizontalAlignment.Right)
						{
							expectedX = 100;
						}
						else if (outerHorizontalAlignment == HorizontalAlignment.Stretch)
						{
							// Inner alignment matters only now
							if (innerHorizontalAlignment == HorizontalAlignment.Center)
							{
								expectedX = 50;
							}
							else if (innerHorizontalAlignment == HorizontalAlignment.Right)
							{
								expectedX = 100;
							}
						}

						double expectedY = 0;
						if (outerVerticalAlignment == VerticalAlignment.Center)
						{
							expectedY = 50;
						}
						else if (outerVerticalAlignment == VerticalAlignment.Bottom)
						{
							expectedY = 100;
						}
						else if (outerVerticalAlignment == VerticalAlignment.Stretch)
						{
							// Inner alignment matters only now
							if (innerVerticalAlignment == VerticalAlignment.Center)
							{
								expectedY = 50;
							}
							else if (innerVerticalAlignment == VerticalAlignment.Bottom)
							{
								expectedY = 100;
							}
						}

						double expectedWidth = 100;
						if (outerHorizontalAlignment == HorizontalAlignment.Stretch &&
							innerHorizontalAlignment == HorizontalAlignment.Stretch)
						{
							expectedWidth = 200;
						}

						double expectedHeight = 100;
						if (outerVerticalAlignment == VerticalAlignment.Stretch &&
							innerVerticalAlignment == VerticalAlignment.Stretch)
						{
							expectedHeight = 200;
						}

						configurations.Add(new AlignmentTestConfiguration(
							outerHorizontalAlignment,
							outerVerticalAlignment,
							innerHorizontalAlignment,
							innerVerticalAlignment,
							new Point(expectedX, expectedY),
							new Size(expectedWidth, expectedHeight)
						));
					}
				}
			}
		}

		return configurations.Select(c => new object[] { c });
	}

	[TestMethod]
	[DynamicData(nameof(GetAlignments), DynamicDataSourceType.Method)]
	public async Task When_Content_Aligned_Position_And_Size(AlignmentTestConfiguration configuration)
	{
		var contentPresenter = new ContentPresenter()
		{
			HorizontalContentAlignment = configuration.OuterHorizontal,
			VerticalContentAlignment = configuration.OuterVertical,
			Width = 200,
			Height = 200,
			Background = new SolidColorBrush(Colors.Red)
		};
		var border = new Border()
		{
			HorizontalAlignment = configuration.InnerHorizontal,
			VerticalAlignment = configuration.InnerVertical,
			Background = new SolidColorBrush(Colors.Blue),
			MinWidth = 100,
			MinHeight = 100
		};
		contentPresenter.Content = border;
		TestServices.WindowHelper.WindowContent = contentPresenter;

		await TestServices.WindowHelper.WaitForLoaded(contentPresenter);
		await TestServices.WindowHelper.WaitForIdle();

		var transform = border.TransformToVisual(contentPresenter);
		var point = transform.TransformPoint(new Point());
		Assert.AreEqual(configuration.ExpectedPosition, point);
		Assert.AreEqual(configuration.ExpectedSize, new Size(border.ActualWidth, border.ActualHeight));
	}

	[TestMethod]
	public async Task When_Content_Unset_Release()
	{
		var SUT = new ContentPresenter();

		TestServices.WindowHelper.WindowContent = SUT;

		var wref = SetContent();
		Assert.AreEqual(wref.Target, SUT.DataContext);

		SUT.Content = null;

		await AssertCollectedReference(wref);

		WeakReference SetContent()
		{
			var o = new object();
			SUT.Content = o;
			return new(o);
		}
	}

	private async Task AssertCollectedReference(WeakReference reference)
	{
		var sw = Stopwatch.StartNew();
		while (sw.Elapsed < TimeSpan.FromSeconds(3))
		{
			GC.Collect(2);
			GC.WaitForPendingFinalizers();

			if (!reference.IsAlive)
			{
				return;
			}

			await Task.Delay(100);
		}

		Assert.IsFalse(reference.IsAlive);
	}
	public class AlignmentTestConfiguration
	{
		public AlignmentTestConfiguration(HorizontalAlignment outerHorizontal, VerticalAlignment outerVertical, HorizontalAlignment innerHorizontal, VerticalAlignment innerVertical, Point expectedPosition, Size expectedSize)
		{
			OuterHorizontal = outerHorizontal;
			OuterVertical = outerVertical;
			InnerHorizontal = innerHorizontal;
			InnerVertical = innerVertical;
			ExpectedPosition = expectedPosition;
			ExpectedSize = expectedSize;
		}

		public HorizontalAlignment OuterHorizontal { get; }

		public VerticalAlignment OuterVertical { get; }

		public HorizontalAlignment InnerHorizontal { get; }

		public VerticalAlignment InnerVertical { get; }

		public Point ExpectedPosition { get; }

		public Size ExpectedSize { get; }

		public override string ToString()
		{
			return $"{OuterHorizontal}/{OuterVertical}/{InnerHorizontal}/{InnerVertical}/{ExpectedPosition}/{ExpectedSize}";
		}
	}
}
