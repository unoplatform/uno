using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Media_Imaging
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_RenderTargetBitmap
	{
		private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush Background
			= new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 255));
		private static readonly Microsoft.UI.Xaml.Media.SolidColorBrush BorderBrush
			= new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(125, 125, 0, 0));
		private static readonly System.Numerics.Vector2 BorderSize
			= new(10, 10);

		static readonly char[] map = { 'B', 'G', 'R', 'A' };

		[TestMethod]
#if __WASM__
		[Ignore("Not implemented yet.")]
#endif
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_Render_Border_GetPixelsAsync()
		{
			var border = new Border()
			{
				Name = "TestBorder",
				Width = 10,
				Height = 10,
				BorderThickness = new Thickness(1),
				Background = Background,
				BorderBrush = BorderBrush,
			};

			var resurcename = GetType().Assembly
				.GetManifestResourceNames()
				.FirstOrDefault(name => name.EndsWith("Border_Snapshot.bgra8"));

			Assert.IsNotNull(resurcename, "Do not find resorce named Border_Snapshot.bgra8");

			var rawBorderSnapshot = GetType().Assembly.GetManifestResourceStream(resurcename)
				.ReadAllBytes();

			TestServices.WindowHelper.WindowContent = border;

			await TestServices.WindowHelper.WaitForLoaded(border);
			//Ensure Layouted
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(BorderSize, border.ActualSize, "Invalid Layouted.");

			var renderer = new RenderTargetBitmap();

			await renderer.RenderAsync(border);

			var pixels = await renderer.GetPixelsAsync();

			//Using of the loop instead of IsSameData method to get more information in case of failure.
			var pixelsArray = pixels.ToArray();
			Assert.AreEqual(rawBorderSnapshot.Length, pixelsArray.Length, $"Invalid length. Expected {rawBorderSnapshot.Length} found {pixelsArray.Length}.");

			for (int i = 0; i < pixelsArray.Length; i++)
			{
				var pixel = i / 4;
				var component = i % 4;
				Assert.AreEqual(rawBorderSnapshot[i], pixelsArray[i], $"The {map[component]} channel of pixel {pixel} is not same. Expected {rawBorderSnapshot[i]:x2} found {pixelsArray[i]:x2}.");
			}
		}
	}
}
