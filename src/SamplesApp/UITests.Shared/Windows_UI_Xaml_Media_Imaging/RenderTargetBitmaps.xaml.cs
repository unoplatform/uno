using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Uno.UI.Samples.Controls;
using System.IO;
using Windows.Graphics.Display;
using Private.Infrastructure;

#if __IOS__
using UIKit;
#endif

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace UITests.Windows_UI_Xaml_Media_Imaging
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	[Sample("Windows.UI.Xaml.Media", IgnoreInSnapshotTests = false)]
	public sealed partial class RenderTargetBitmaps : Page
	{
		public static DependencyProperty RunTestProperty { get; } =
			DependencyProperty.Register(nameof(RunTest),
				typeof(string),
				typeof(RenderTargetBitmaps),
				new PropertyMetadata(default(string)
					, propertyChangedCallback: (s, e) =>
						{
							if (!string.IsNullOrEmpty((string)e.NewValue))
								((RenderTargetBitmaps)s).RenderBoder();
						}));

		public string RunTest
		{
			get => (string)GetValue(RunTestProperty);
			set => SetValue(RunTestProperty, value);
		}
		public static DependencyProperty TestResultProperty { get; } = DependencyProperty.Register(nameof(TestResult)
			, typeof(string)
			, typeof(RenderTargetBitmaps)
			, new PropertyMetadata(default(string)));

		public string TestResult
		{
			get { return (string)GetValue(TestResultProperty); }
			set { SetValue(TestResultProperty, value); }
		}
		public RenderTargetBitmaps()
		{
			this.InitializeComponent();
		}

		private async void SaveAs(object sender, RoutedEventArgs e)
		{
#if __SKIA__
			// Workaround to avoid issue #7829
			await UnitTestDispatcherCompat.From(this).RunAsync(UnitTestDispatcherCompat.Priority.Normal, GenerateScreenshots);
#else
			await GenerateScreenshots();
#endif
		}

		private async
#if __SKIA__
		void
#else
		System.Threading.Tasks.Task
#endif
		GenerateScreenshots()
		{
			var folder = await new FolderPicker { FileTypeFilter = { "*" } }.PickSingleFolderAsync();
			if (folder == null)
			{
				return;
			}

			var fileName = "BorderRender.png";
			var renderer = new RenderTargetBitmap();
			await renderer.RenderAsync(this.border);

			var file = await folder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);
			using (var output = await file.OpenAsync(FileAccessMode.ReadWrite))
			{
#if WINAPPSDK || __SKIA__ || __ANDROID__ || __IOS__ || __MACOS__
				var pixels = await renderer.GetPixelsAsync();
				var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, output);
				encoder.SetPixelData(BitmapPixelFormat.Bgra8
					, BitmapAlphaMode.Premultiplied
					, (uint)renderer.PixelWidth
					, (uint)renderer.PixelHeight
					, XamlRoot.RasterizationScale
					, XamlRoot.RasterizationScale
					, pixels.ToArray()
					);
				await encoder.FlushAsync();
				await output.FlushAsync();
#endif
			}

		}

		private async void RenderBoder()
		{
			await UnitTestDispatcherCompat.From(this).RunAsync(UnitTestDispatcherCompat.Priority.Normal, async () =>
				{
					var result = new System.Text.StringBuilder();
					try
					{
						var renderer = new RenderTargetBitmap();
						await renderer.RenderAsync(this.border); ;
						var bgraPixels = await renderer.GetPixelsAsync();
						byte[] testResult;

						using (var @output = new MemoryStream())
						using (var ms = @output.AsRandomAccessStream())
						{
							var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, ms);
							encoder.SetPixelData(BitmapPixelFormat.Bgra8,
								BitmapAlphaMode.Premultiplied,
								(uint)renderer.PixelWidth,
								(uint)renderer.PixelHeight,
								XamlRoot.RasterizationScale,
								XamlRoot.RasterizationScale,
								bgraPixels.ToArray()
								);
							await encoder.FlushAsync();
							@output.Position = 0;
							testResult = output.ToArray();
						}

						result.Append("SUCCESS;");
						result.Append(Convert.ToBase64String(testResult));

					}
					catch (Exception e)
					{
						result.Append("ERROR;");
						result.Append(Convert.ToBase64String(Encoding.UTF8.GetBytes(e.ToString())));

					}
					TestResult = result.ToString();
				});
		}
	}
}
