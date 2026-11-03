using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Shapes;
using static Private.Infrastructure.TestServices;
using ImageBrush = Microsoft.UI.Xaml.Media.ImageBrush;


namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_ImageBrush
	{
		[DataRow(Stretch.Fill, false)]
		[DataRow(Stretch.Fill, true)]
#if !__APPLE_UIKIT__
		// See https://github.com/unoplatform/uno/issues/9080
		[DataRow(Stretch.UniformToFill, false)]
#endif
		[DataRow(Stretch.UniformToFill, true)]
#if !__ANDROID__
		// Stretch.None is broken on Android.
		// See https://github.com/unoplatform/uno/pull/7238#issuecomment-937667565
		[DataRow(Stretch.None, false)]
		[DataRow(Stretch.None, true)]
#endif
#if !__APPLE_UIKIT__
		// See https://github.com/unoplatform/uno/issues/9080
		[DataRow(Stretch.Uniform, false)]
#endif
		[DataRow(Stretch.Uniform, true)]
		[TestMethod]
		[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeUIKit)] // Flaky on UIKit - #9080
		public async Task When_Stretch(Stretch stretch, bool useRectangle)
		{
			const string Redish = "#FFEB1C24";
			const string Yellowish = "#FFFEF200";
			const string Greenish = "#FF0ED145";
			const string Transparent = "#00000000";

			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap, Uno.UI"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var brush = new ImageBrush
			{
				ImageSource = new BitmapImage(new Uri("ms-appx:///Assets/colored-ellipse.jpg")),
				Stretch = stretch,
			};

			FrameworkElement SUT;
			if (useRectangle)
			{
				SUT = new Rectangle
				{
					Width = 100,
					Height = 100,
					StrokeThickness = 2,
					Stroke = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 0, 0)),
					Fill = brush,
				};
			}
			else
			{
				SUT = new Border
				{
					Width = 100,
					Height = 100,
					BorderThickness = new Thickness(2),
					BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 0, 0)),
					Background = brush,
				};
			}
			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForLoaded(SUT);

			// Retry is required because the brush even is not always raised
			await TestHelper.RetryAssert(async () =>
			{
				float BorderOffset =
#if __APPLE_UIKIT__ || __SKIA__
					6;
#else
					3;
#endif
				float width = (float)SUT.Width, height = (float)SUT.Height;
				float centerX = width / 2, centerY = height / 2;
				var expectations = stretch switch
				{
					// All edges are red-ish
					Stretch.Fill => (Top: Redish, Bottom: Redish, Left: Redish, Right: Redish),
					// Top and bottom are red-ish. Left and right are yellow-ish
					Stretch.UniformToFill => (Top: Redish, Bottom: Redish, Left: Yellowish, Right: Yellowish),
					// Top and bottom are same as background. Left and right are red-ish
					Stretch.Uniform => (Top: Transparent, Bottom: Transparent, Left: Redish, Right: Redish),
					// Everything is green-ish
					Stretch.None => (Top: Greenish, Bottom: Greenish, Left: Greenish, Right: Greenish),

					_ => throw new ArgumentOutOfRangeException($"unexpected stretch: {stretch}"),
				};

				var bitmap = await UITestHelper.ScreenShot(SUT);

				ImageAssert.HasColorAt(bitmap, centerX, BorderOffset, expectations.Top, tolerance: 28);
				ImageAssert.HasColorAt(bitmap, centerX, height - BorderOffset, expectations.Bottom, tolerance: 28);
				ImageAssert.HasColorAt(bitmap, BorderOffset, centerY, expectations.Left, tolerance: 28);
				ImageAssert.HasColorAt(bitmap, width - BorderOffset, centerY, expectations.Right, tolerance: 28);
			}, 10);
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25596")]
		[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.Skia)]
		public async Task When_Source_Outlives_Brushes()
		{
			// Like an icon held in a static field or in the application resources.
			var source = new BitmapImage(new Uri("ms-appx:///Assets/square100.png"));

			var (firstBorder, secondBorder, firstBrush, secondBrush) = await ShowAndRemoveBorders(source);

			Assert.IsTrue(await TestHelper.TryWaitUntilCollected(firstBorder), "The first border should be collected once removed, even though its brush's source is alive.");
			Assert.IsTrue(await TestHelper.TryWaitUntilCollected(secondBorder), "The second border should be collected once removed, even though its brush's source is alive.");
			Assert.IsTrue(await TestHelper.TryWaitUntilCollected(firstBrush), "The first brush should be collected once its border is, even though its source is alive.");
			Assert.IsTrue(await TestHelper.TryWaitUntilCollected(secondBrush), "The second brush should be collected once its border is, even though its source is alive.");
			GC.KeepAlive(source);
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static async Task<(WeakReference, WeakReference, WeakReference, WeakReference)> ShowAndRemoveBorders(ImageSource source)
		{
			// Two brushes share the source, as they would an icon. A source used by a single brush also
			// references that brush as its inheritance context, which is not covered here.
			var firstBrush = new ImageBrush { ImageSource = source };
			var secondBrush = new ImageBrush { ImageSource = source };
			var opened = 0;
			firstBrush.ImageOpened += (_, _) => opened++;
			secondBrush.ImageOpened += (_, _) => opened++;

			var firstBorder = new Border { Width = 50, Height = 50, Background = firstBrush };
			var secondBorder = new Border { Width = 50, Height = 50, Background = secondBrush };

			try
			{
				await UITestHelper.Load(new StackPanel { Children = { firstBorder, secondBorder } });
				await WindowHelper.WaitFor(() => opened == 2, timeoutMS: 5000, message: "Both brushes should open the source.");

				return (new(firstBorder), new(secondBorder), new(firstBrush), new(secondBrush));
			}
			finally
			{
				WindowHelper.WindowContent = null;
				await WindowHelper.WaitForIdle();
			}
		}

#if __SKIA__
		[TestMethod]
		[Ignore("Flaky. https://github.com/unoplatform/uno/issues/9080")]
		public async Task When_DownSampling()
		{
			var border = new Border
			{
				Width = 100,
				Height = 100,
				Background = new ImageBrush
				{
					ImageSource = new Uri("ms-appx:/Assets/LargeWisteria.jpg")
				}
			};

			var image = new Image
			{
				Width = 100,
				Height = 100,
				Source = new Uri("ms-appx:/Assets/ResizedLargeWisteria.png")
			};

			await UITestHelper.Load(new StackPanel
			{
				Children =
				{
					border,
					image
				}
			});
			await Task.Delay(1000); // wait for idle might not be enough here

			var bitmap = await UITestHelper.ScreenShot(border);
			var bitmap2 = await UITestHelper.ScreenShot(image);
			await ImageAssert.AreEqualAsync(bitmap, bitmap2);
		}
#endif
	}
}
