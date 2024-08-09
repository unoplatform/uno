using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using static Private.Infrastructure.TestServices;

#if __SKIA__
using SkiaSharp;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public class Given_Image
	{
		[TestMethod]
		[RunsOnUIThread]
#if !__SKIA__
		[Ignore("TODO: Fix on other platforms")]
#endif
		public async Task When_Parent_Has_BorderThickness()
		{
			var image = new Image()
			{
				Width = 100,
				Height = 100,
				Source = new BitmapImage(new Uri("ms-appx:///Assets/my500x200.jpg")),
			};

			var grid = new Grid()
			{
				Width = 100,
				Height = 100,
				Background = new SolidColorBrush(Windows.UI.Colors.Orange),
				BorderBrush = new SolidColorBrush(Windows.UI.Colors.Gray),
				BorderThickness = new(20),
				Children =
				{
					image,
				},
			};

			var imageOpened = false;
			image.ImageOpened += (_, _) => imageOpened = true;

			await UITestHelper.Load(grid);

			await WindowHelper.WaitFor(() => imageOpened == true);

			var screenshot = await UITestHelper.ScreenShot(grid);

			var orangeBounds = ImageAssert.GetColorBounds(screenshot, Colors.Orange, tolerance: 10); // 60x60
			var redBounds = ImageAssert.GetColorBounds(screenshot, Colors.Red, tolerance: 10); // 60x20
			var greenBounds = ImageAssert.GetColorBounds(screenshot, Color.FromArgb(255, 75, 255, 0), tolerance: 10); // 20x20
			var yellowBounds = ImageAssert.GetColorBounds(screenshot, Color.FromArgb(255, 255, 249, 75), tolerance: 10); // 20x20
			var pinkBounds = ImageAssert.GetColorBounds(screenshot, Color.FromArgb(255, 255, 35, 233), tolerance: 10); // 12x20

			Assert.AreEqual(new Rect(20, 20, 59, 59), orangeBounds);
			Assert.AreEqual(new Rect(20, 30, 59, 18), redBounds);
			Assert.AreEqual(new Rect(20, 41, 19, 17), greenBounds);
			Assert.AreEqual(new Rect(44, 41, 19, 17), yellowBounds);
			Assert.AreEqual(new Rect(68, 41, 11, 17), pinkBounds);
		}

#if !__IOS__ // Currently fails on iOS
		[TestMethod]
#endif
		[RunsOnUIThread]
		public async Task When_Fixed_Height_And_Stretch_Uniform()
		{
			var imageLoaded = new TaskCompletionSource<bool>();

			var image = new Image { Height = 30, Stretch = Stretch.Uniform, Source = new BitmapImage(new Uri("ms-appx:///Assets/StoreLogo.png")) };
			image.Loaded += (s, e) => imageLoaded.TrySetResult(true);
			image.ImageFailed += (s, e) => imageLoaded.TrySetException(new Exception(e.ErrorMessage));

			var innerGrid = new Grid { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Center };
			var outerGrid = new Grid { Height = 750, Width = 430 };
			innerGrid.Children.Add(image);
			outerGrid.Children.Add(innerGrid);

			TestServices.WindowHelper.WindowContent = outerGrid;
			await TestServices.WindowHelper.WaitForIdle();

			await imageLoaded.Task;

			image.InvalidateMeasure();

			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.WindowHelper.WaitForIdle();

			outerGrid.Measure(new Size(1000, 1000));
			var desiredContainer = innerGrid.DesiredSize;

			// Workaround for image.Loaded being raised too early on WebAssembly
			var sw = Stopwatch.StartNew();
			do
			{
				await TestServices.WindowHelper.WaitForIdle();

				if (Math.Round(desiredContainer.Width) != 0 && Math.Round(desiredContainer.Height) != 0)
				{
					break;
				}

				desiredContainer = innerGrid.DesiredSize;
			}
			while (sw.Elapsed < TimeSpan.FromSeconds(5));

			await TestServices.WindowHelper.WaitForIdle();

			using var _ = new AssertionScope();

			Math.Round(desiredContainer.Width).Should().Be(30, "desiredContainer.Width");
			Math.Round(desiredContainer.Height).Should().Be(30, "desiredContainer.Width");

			TestServices.WindowHelper.WindowContent = null;
		}

#if __WASM__
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Resource_Has_Scale_Qualifier()
		{
			var scales = new List<ResolutionScale>()
			{
				(ResolutionScale)80,
				ResolutionScale.Scale100Percent,
				ResolutionScale.Scale150Percent,
				ResolutionScale.Scale200Percent,
				ResolutionScale.Scale300Percent,
				ResolutionScale.Scale400Percent,
				ResolutionScale.Scale500Percent,
			};

			try
			{
				foreach (var scale in scales)
				{
					var imageOpened = new TaskCompletionSource<bool>();

					var source = new BitmapImage(new Uri("ms-appx:///Assets/Icons/FluentIcon_Medium.png"));
					source.ScaleOverride = scale;

					var image = new Image { Height = 24, Width = 24, Stretch = Stretch.Uniform, Source = source };
					image.ImageOpened += (s, e) => imageOpened.TrySetResult(true);
					image.ImageFailed += (s, e) => imageOpened.TrySetResult(false);

					TestServices.WindowHelper.WindowContent = image;

					await TestServices.WindowHelper.WaitForIdle();
					await Task.Delay(200);

					var result = await imageOpened.Task;

					Assert.IsTrue(result);
				}
			}
			finally
			{
				TestServices.WindowHelper.WindowContent = null;
			}
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
		public void TargetNullValue_Is_Correctly_Applied()
		{
			var SUT = new ImageSource_TargetNullValue();

			var nameIsAppliedSource = SUT.NameIsApplied.Source as BitmapImage;
			Assert.AreEqual("ms-appx:///mypanel", nameIsAppliedSource.UriSource.ToString());

			var targetNullValueSource = SUT.TargetNullValueIsApplied.Source as BitmapImage;
			Assert.AreEqual("ms-appx:///Assets/StoreLogo.png", targetNullValueSource.UriSource.ToString());
		}

		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Fails on macOS for resising and assets locations https://github.com/unoplatform/uno/issues/6261")]
#endif
		public async Task When_Transitive_Asset_Loaded()
		{
			string url = "ms-appx:///Uno.UI.RuntimeTests/Assets/Transitive-ingredient01.png";
			var img = new Image();
			var SUT = new BitmapImage(new Uri(url));
			img.Source = SUT;

			TestServices.WindowHelper.WindowContent = img;
			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.WindowHelper.WaitFor(() => img.ActualHeight > 0, 3000);

			Assert.IsTrue(img.ActualHeight > 0);
		}

		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Fails on macOS for resising and assets locations https://github.com/unoplatform/uno/issues/6261")]
#endif
		public async Task When_Transitive_Asset_With_Link_Loaded()
		{
			string url = "ms-appx:///Uno.UI.RuntimeTests/Assets/TransitiveTest/colors300.png";
			var img = new Image();
			var SUT = new BitmapImage(new Uri(url));
			img.Source = SUT;

			TestServices.WindowHelper.WindowContent = img;
			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.WindowHelper.WaitFor(() => img.ActualHeight > 0, 3000);

			Assert.IsTrue(img.ActualHeight > 0);
		}

		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Fails on macOS for resising and assets locations https://github.com/unoplatform/uno/issues/6261")]
#endif
		public async Task When_Explicit_BitmapImage_Relative_NonRooted()
		{
			ImageControls.When_Image SUT = new();

			TestServices.WindowHelper.WindowContent = SUT;
			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.WindowHelper.WaitFor(() => SUT.explicitRelativeNonRooted.ActualHeight > 0, 3000);

			Assert.IsTrue(SUT.explicitRelativeNonRooted.ActualHeight > 0);
		}

		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Fails on macOS for resising and assets locations https://github.com/unoplatform/uno/issues/6261")]
#endif
		public async Task When_Relative_NonRooted()
		{
			ImageControls.When_Image SUT = new();

			TestServices.WindowHelper.WindowContent = SUT;
			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.WindowHelper.WaitFor(() => SUT.relativeNonRooted.ActualHeight > 0, 3000);

			Assert.IsTrue(SUT.relativeNonRooted.ActualHeight > 0);
		}

		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Fails on macOS for resising and assets locations https://github.com/unoplatform/uno/issues/6261")]
#endif
		public async Task When_Relative_Rooted()
		{
			ImageControls.When_Image SUT = new();

			TestServices.WindowHelper.WindowContent = SUT;
			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.WindowHelper.WaitFor(() => SUT.relativeRooted.ActualHeight > 0, 3000);

			Assert.IsTrue(SUT.relativeRooted.ActualHeight > 0);
		}


		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Fails on macOS for resising and assets locations https://github.com/unoplatform/uno/issues/6261")]
#endif
		public async Task When_AbsoluteLocal()
		{
			ImageControls.When_Image SUT = new();

			TestServices.WindowHelper.WindowContent = SUT;
			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.WindowHelper.WaitFor(() => SUT.absoluteLocal.ActualHeight > 0, 3000);

			Assert.IsTrue(SUT.absoluteLocal.ActualHeight > 0);
		}

		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Fails on macOS for resising and assets locations https://github.com/unoplatform/uno/issues/6261")]
#endif
		public async Task When_AbsoluteMain()
		{
			ImageControls.When_Image SUT = new();

			TestServices.WindowHelper.WindowContent = SUT;
			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.WindowHelper.WaitFor(() => SUT.absoluteMain.ActualHeight > 0, 3000);

			Assert.IsTrue(SUT.absoluteMain.ActualHeight > 0);
		}


		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Randomly fails on macOS")]
#endif
		public async Task When_Image_Is_Loaded_From_URL()
		{
			string decoded_url = "https://uno-assets.platform.uno/tests/images/image with spaces.jpg";
			var img = new Image();
			var SUT = new BitmapImage(new Uri(decoded_url));
			img.Source = SUT;

			TestServices.WindowHelper.WindowContent = img;
			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.WindowHelper.WaitFor(() => img.ActualHeight > 0, 3000);

			Assert.IsTrue(img.ActualHeight > 0);
		}

		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282! epic")]
#endif
		public async Task When_Image_Source_Nullify()
		{
			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var parent = new Border()
			{
				Width = 100,
				Height = 100,
				Background = new SolidColorBrush(Colors.White)
			};

			var SUT = new Image()
			{
				Width = 100,
				Height = 100,
				Source = new BitmapImage(new Uri("ms-appx:///Assets/square100.png")),
				Stretch = Stretch.Fill
			};

			parent.Child = SUT;
			WindowHelper.WindowContent = parent;
			await WindowHelper.WaitForLoaded(parent);
			var result = await TakeScreenshot(parent);

			var sample = parent.GetRelativeCoords(SUT);
			var centerX = sample.X + sample.Width / 2;
			var centerY = sample.Y + sample.Height / 2;

			ImageAssert.HasPixels(
				result,
				ExpectedPixels.At(centerX, centerY).Named("center with image").Pixel(Colors.Blue));

			SUT.Source = null;
			await WindowHelper.WaitForIdle();
			result = await TakeScreenshot(parent);

			ImageAssert.HasPixels(
				result,
				ExpectedPixels.At(centerX, centerY).Named("center without image").Pixel(Colors.White));
		}

#if !WINAPPSDK
		[TestMethod]
		[RunsOnUIThread]
#if IS_UNIT_TESTS || __MACOS__ || __SKIA__
		[Ignore("Currently fails on macOS, part of #9282! epic and Monochromatic Image not supported for IS_UNIT_TESTS and SKIA")]
#endif
		public async Task When_Image_Is_Monochromatic()
		{
			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var red = Colors.Red;

			var parent = new Border()
			{
				Width = 100,
				Height = 150,
				Background = new SolidColorBrush(Colors.Blue)
			};

			var SUT = new Image()
			{
				Width = 100,
				Height = 150,
				Source = new BitmapImage(new Uri("ms-appx:///Assets/test_image_100_150.png")),
				Stretch = Stretch.UniformToFill,
				MonochromeColor = Colors.Red
			};

			parent.Child = SUT;

			TestServices.WindowHelper.WindowContent = parent;
			await WindowHelper.WaitForLoaded(parent);

			var snapshot = await TakeScreenshot(parent);

			var sample = parent.GetRelativeCoords(SUT);
			var centerX = sample.X + sample.Width / 2;
			var centerY = sample.Y + sample.Height / 2;

			ImageAssert.HasPixels(
				snapshot,
				ExpectedPixels
					.At(centerX, centerY)
					.Named("center")
					.WithPixelTolerance(1, 1)
					.Pixel(red),
				ExpectedPixels
					.At(sample.X, sample.Y)
					.Named("top-left")
					.WithPixelTolerance(1, 1)
					.Pixel(red),
				ExpectedPixels
					.At(sample.X + sample.Width - 1f, sample.Y)
					.Named("top-right")
					.WithPixelTolerance(1, 1)
					.Pixel(red),
				ExpectedPixels
					.At(sample.X, sample.Y + sample.Height - 1f)
					.Named("bottom-left")
					.WithPixelTolerance(1, 1)
					.Pixel(red),
				ExpectedPixels
					.At(sample.X + sample.Width - 1f, sample.Y + sample.Height - 1f)
					.Named("bottom-right")
					.WithPixelTolerance(1, 1)
					.Pixel(red));
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_BitmapImage_Should_Have_Correct_Event_Sequence()
		{
			var logs = new List<string>();
			var image = new Image();

			var bitmapImage = new BitmapImage(new Uri("ms-appx:///Assets/test_image_100_150.png"));
			bitmapImage.ImageFailed += BitmapImage_ImageFailed;
			bitmapImage.DownloadProgress += BitmapImage_DownloadProgress;
			bitmapImage.ImageOpened += BitmapImage_ImageOpened;
			image.ImageFailed += Image_ImageFailed;
			image.ImageOpened += Image_ImageOpened;
			var imageOpened = false;

			void BitmapImage_ImageFailed(object sender, ExceptionRoutedEventArgs e) => logs.Add("BitmapImage_ImageFailed");
			void BitmapImage_DownloadProgress(object sender, DownloadProgressEventArgs e) => logs.Add("BitmapImage_DownloadProgress");
			void BitmapImage_ImageOpened(object sender, RoutedEventArgs e) => logs.Add($"BitmapImage_ImageOpened. {bitmapImage.PixelWidth}x{bitmapImage.PixelHeight}");
			void Image_ImageFailed(object sender, ExceptionRoutedEventArgs e) => logs.Add("Image_ImageFailed");
			void Image_ImageOpened(object sender, RoutedEventArgs e)
			{
				logs.Add("Image_ImageOpened");
				imageOpened = true;
			}

			image.Source = bitmapImage;

			TestServices.WindowHelper.WindowContent = image;
			await WindowHelper.WaitForLoaded(image);
			await TestServices.WindowHelper.WaitFor(() => imageOpened);

			Assert.AreEqual(2, logs.Count, string.Join(Environment.NewLine, logs));
			Assert.AreEqual("BitmapImage_ImageOpened. 100x150", logs[0]);
			Assert.AreEqual("Image_ImageOpened", logs[1]);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Loaded_From_AppData_LocalFolder()
		{
			var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/test_image_100_150.png"));
			await file.CopyAsync(ApplicationData.Current.LocalFolder, "newfile.png", NameCollisionOption.ReplaceExisting);
			var uri = new Uri($"ms-appdata:///Local/newfile.png");
			var bitmapImage = new BitmapImage(uri);
			var image = new Image() { Source = bitmapImage };
			TestServices.WindowHelper.WindowContent = image;
			await WindowHelper.WaitForLoaded(image);
		}

		[TestMethod]
		[RunsOnUIThread]
		[DataRow("ms-appx:///Assets/couch.svg")]
		[DataRow("ms-appx:///Uno.UI.RuntimeTests/Assets/couch.svg")]
		[DataRow("ms-appx:///Uno.UI.RuntimeTests/Assets/help.svg")]
		public async Task When_SVGImageSource(string imagePath)
		{
			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive("RenderTargetBitmap is not supported on this platform");
			}

			var svgImageSource = new SvgImageSource(new Uri(imagePath));
			var image = new Image() { Source = svgImageSource, Width = 100, Height = 100 };
			TestServices.WindowHelper.WindowContent = image;
			await WindowHelper.WaitForLoaded(image);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_SVGImageSource_Uri_Is_Null()
		{
			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var svgImageSource = new SvgImageSource(null);
			var image = new Image() { Source = svgImageSource, Width = 100, Height = 100 };
			TestServices.WindowHelper.WindowContent = image;
			await WindowHelper.WaitForLoaded(image);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_SVGImageSource_Uri_Is_Set_Null()
		{
			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var svgImageSource = new SvgImageSource(new Uri("ms-appx:///Assets/couch.svg"));
			svgImageSource.UriSource = null;
			var image = new Image() { Source = svgImageSource, Width = 100, Height = 100 };
			TestServices.WindowHelper.WindowContent = image;
			await WindowHelper.WaitForLoaded(image);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ImageFailed()
		{
			var image = new Image() { Width = 100, Height = 100 };
			TestServices.WindowHelper.WindowContent = image;
			await WindowHelper.WaitForLoaded(image);
			bool imageFailedRaised = false;
			image.ImageFailed += (s, e) =>
			{
				imageFailedRaised = true;
			};
			image.Source = new BitmapImage(new Uri("ms-appx:///image/definitely/does/not/exist.png"));

			await WindowHelper.WaitFor(() => imageFailedRaised);
		}

#if __SKIA__
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Png_Should_Have_High_Quality()
		{
			var image = new Image() { Width = 100, Height = 100 };
			await UITestHelper.Load(image);
			bool imageOpenedRaised = false;
			image.ImageOpened += (s, e) =>
			{
				imageOpenedRaised = true;
			};

			image.Source = new BitmapImage(new Uri("ms-appx:///Assets/Icons/star_empty.png"));

			await WindowHelper.WaitFor(() => imageOpenedRaised);

			var storageFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Icons/star_empty.png"));
			var buffer = await FileIO.ReadBufferAsync(storageFile);
			var data = buffer.ToArray();

			var skBitmap = SKBitmap.FromImage(SKImage.FromEncodedData(data));

			var bitmap = await UITestHelper.ScreenShot(image);

			Assert.AreEqual(72, skBitmap.Width);
			Assert.AreEqual(72, skBitmap.Height);


			var skBitmapScaled = new SKBitmap(skBitmap.Info with { Width = 100, Height = 100 });

			Assert.IsTrue(skBitmap.ScalePixels(skBitmapScaled, SKFilterQuality.High));

			for (int x = 0; x < 100; x++)
			{
				for (int y = 0; y < 100; y++)
				{
					var skColor = skBitmapScaled.GetPixel(x, y);
					var color1 = new Color(skColor.Alpha, skColor.Red, skColor.Green, skColor.Blue);
					var color2 = bitmap.GetPixel(x, y);
					if (color1 != color2)
					{
						Assert.Fail($"Color mismatch at {x}, {y}: {color1} != {color2}");
					}
				}
			}
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_Exif_Rotated_MsAppx()
		{
			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			await When_Exif_Rotated_Common(new Uri("ms-appx:///Assets/testimage_exif_rotated.jpg"));
		}

		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_Exif_Rotated_MsAppx_Unequal_Dimensions()
		{
			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var uri = new Uri("ms-appx:///Assets/testimage_exif_rotated_different_dimensions.jpg");
			var image = new Image();
			var bitmapImage = new BitmapImage(uri);
			var imageOpened = false;
			image.ImageOpened += (_, _) => imageOpened = true;
			image.Source = bitmapImage;
			WindowHelper.WindowContent = image;
			await WindowHelper.WaitForLoaded(image);
			await WindowHelper.WaitFor(() => imageOpened);
			var screenshot = await TakeScreenshot(image);
			ImageAssert.HasColorAt(screenshot, 5, screenshot.Height / 2, Color.FromArgb(0xFF, 0xED, 0x1B, 0x24), tolerance: 5);
			ImageAssert.HasColorAt(screenshot, screenshot.Width / 2 - 10, screenshot.Height / 2, Color.FromArgb(0xFF, 0xED, 0x1B, 0x24), tolerance: 5);
			ImageAssert.HasColorAt(screenshot, screenshot.Width / 2 + 10, screenshot.Height / 2, Color.FromArgb(0xFF, 0x23, 0xB1, 0x4D), tolerance: 5);
			ImageAssert.HasColorAt(screenshot, screenshot.Width - 5, screenshot.Height / 2, Color.FromArgb(0xFF, 0x23, 0xB1, 0x4D), tolerance: 5);

		}

		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_Exif_Rotated_MsAppData()
		{
			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/testimage_exif_rotated.jpg"));
			var fileName = $"{Guid.NewGuid()}.jpg";
			await file.CopyAsync(ApplicationData.Current.LocalFolder, fileName);
			var uri = new Uri($"ms-appdata:///Local/{fileName}");
			await When_Exif_Rotated_Common(uri);
		}

#if __ANDROID__
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Exif_Rotated_Target_Is_Saf()
		{
			var directory = new Java.IO.File(ApplicationData.Current.LocalCacheFolder.Path);
			var documentFile = AndroidX.DocumentFile.Provider.DocumentFile.FromFile(directory);
			var safFolder = StorageFolder.GetFromSafDocument(documentFile);

			var file1 = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/testimage_exif_rotated.jpg"));
			var file2 = await file1.CopyAsync(safFolder, "testimage_exif_rotated.jpg", NameCollisionOption.ReplaceExisting);
			await file2.CopyAsync(ApplicationData.Current.LocalFolder, "testimage_exif_rotated.jpg", NameCollisionOption.ReplaceExisting);
			await When_Exif_Rotated_Common(new Uri($"ms-appdata:///Local/testimage_exif_rotated.jpg"));
		}
#endif

		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_Exif_Rotated_From_Stream()
		{
			if (!ApiInformation.IsTypePresent("Windows.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/testimage_exif_rotated.jpg"));
			var image = new Image();
			var bitmapImage = new BitmapImage();
			await bitmapImage.SetSourceAsync(await file.OpenReadAsync());

			var imageOpened = false;
			image.ImageOpened += (_, _) => imageOpened = true;
			image.Source = bitmapImage;
			WindowHelper.WindowContent = image;
			await WindowHelper.WaitForLoaded(image);
			await WindowHelper.WaitFor(() => imageOpened);
			var screenshot = await TakeScreenshot(image);
			ImageAssert.HasColorAt(screenshot, screenshot.Width / 2, 5, Color.FromArgb(0xFF, 0x23, 0xB1, 0x4D), tolerance: 5);
			ImageAssert.HasColorAt(screenshot, screenshot.Width / 2, screenshot.Height - 5, Color.FromArgb(0xFF, 0xED, 0x1B, 0x24), tolerance: 5);
		}

		private async Task When_Exif_Rotated_Common(Uri uri)
		{
			var image = new Image();
			var bitmapImage = new BitmapImage(uri);
			var imageOpened = false;
			image.ImageOpened += (_, _) => imageOpened = true;
			image.Source = bitmapImage;
			WindowHelper.WindowContent = image;
			await WindowHelper.WaitForLoaded(image);
			await WindowHelper.WaitFor(() => imageOpened);
			var screenshot = await TakeScreenshot(image);
			ImageAssert.HasColorAt(screenshot, screenshot.Width / 2, 5, Color.FromArgb(0xFF, 0x23, 0xB1, 0x4D), tolerance: 5);
			ImageAssert.HasColorAt(screenshot, screenshot.Width / 2, screenshot.Height - 5, Color.FromArgb(0xFF, 0xED, 0x1B, 0x24), tolerance: 5);
		}

		private Task<RawBitmap> TakeScreenshot(FrameworkElement SUT)
			=> UITestHelper.ScreenShot(SUT);
	}
}
