using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.InteropServices.WindowsRuntime;
using Private.Infrastructure;
using Windows.Storage;
using static Private.Infrastructure.TestServices;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Extensions;
using Windows.Foundation.Metadata;
using Microsoft.UI.Xaml;
using System.Linq;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Imaging
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_WriteableBitmap
	{
		[TestMethod]
#if __APPLE_UIKIT__
		[Ignore("fails")]
#endif
		public async Task When_Invalidated()
		{
			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var seed = 42;
			var bitmap = new WriteableBitmap(200, 200);
			var border = new Border
			{
				Width = 200,
				Height = 200,
				Background = new ImageBrush { ImageSource = bitmap }
			};

			UpdateSource();

			await UITestHelper.Load(border);

			var snapshot1 = await UITestHelper.ScreenShot(border);

			UpdateSource();
			await WindowHelper.WaitForIdle();

			var snapshot2 = await UITestHelper.ScreenShot(border);
			await ImageAssert.AreNotEqualAsync(snapshot1, snapshot2);

			void UpdateSource()
			{
				var randomizer = new Random(seed++);
				var stream = bitmap.PixelBuffer.AsStream();
				var length = bitmap.PixelBuffer.Length;
				for (var i = 0; i < length; i++)
				{
					if (i % 4 == 3)
					{
						stream.WriteByte(255);
					}
					else
					{
						stream.WriteByte((byte)randomizer.Next(256));
					}
				}

				bitmap.Invalidate();
			}
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_WriteableBitmap_Assigned_With_Data_Present()
		{
			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var wb = new WriteableBitmap(20, 20);

			var parent = new Border()
			{
				Width = 50,
				Height = 50,
				Background = new SolidColorBrush(Colors.Blue),
			};

			var rect = new Rectangle
			{
				Width = 20,
				Height = 20,
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Center,
			};

			parent.Child = rect;

			WindowHelper.WindowContent = parent;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(rect);

			var bgraPixelData = Enumerable.Repeat<byte>(255, (int)wb.PixelBuffer.Length).ToArray();

			using (Stream stream = wb.PixelBuffer.AsStream())
			{
				stream.Write(bgraPixelData, 0, bgraPixelData.Length);
			}

			rect.Fill = new ImageBrush
			{
				ImageSource = wb
			};

			await WindowHelper.WaitForIdle();

			var snapshot = await UITestHelper.ScreenShot(parent);
			var coords = parent.GetRelativeCoords(rect);
			await WindowHelper.WaitForIdle();

			ImageAssert.DoesNotHaveColorInRectangle(
				snapshot, new System.Drawing.Rectangle((int)coords.X, (int)coords.Y, (int)coords.Width, (int)coords.Height), Colors.Blue);
		}

		[TestMethod]
		[RunsOnUIThread]
#if __WASM__
		[Ignore("https://github.com/unoplatform/uno/issues/12445")]
#endif
		public async Task When_WriteableBitmap_SetSource_Should_Update_PixelWidth_And_PixelHeight()
		{
			if (!ApiInformation.IsTypePresent("Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap"))
			{
				Assert.Inconclusive(); // System.NotImplementedException: RenderTargetBitmap is not supported on this platform.;
			}

			var writeableBitmap = new WriteableBitmap(1, 1);
			var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/ingredient3.png"));
			using (var stream = await file.OpenReadAsync())
			{
				await writeableBitmap.SetSourceAsync(stream);
			}

			Assert.AreEqual(147, writeableBitmap.PixelWidth);
			Assert.AreEqual(147, writeableBitmap.PixelHeight);

			var parent = new Border()
			{
				Width = 147,
				Height = 147,
			};

			var rect = new Rectangle
			{
				Width = 147,
				Height = 147,
			};

			parent.Child = rect;

			WindowHelper.WindowContent = parent;

			await WindowHelper.WaitForIdle();
			await WindowHelper.WaitForLoaded(rect);

			rect.Fill = new ImageBrush
			{
				ImageSource = writeableBitmap
			};

			await WindowHelper.WaitForIdle();

			var renderer = new RenderTargetBitmap();

			await renderer.RenderAsync(parent);
			var snapshot = await RawBitmap.From(renderer, rect);

			if (!OperatingSystem.IsOSPlatform("iOS")) // https://github.com/unoplatform/uno/issues/12705
			{
				ImageAssert.HasColorAt(snapshot, 1, 1, Color.FromArgb(0xFF, 0xFA, 0xB8, 0x63), tolerance: 5);
			}
		}
	}
}
