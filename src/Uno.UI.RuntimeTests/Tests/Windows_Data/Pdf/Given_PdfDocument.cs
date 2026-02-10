#nullable enable
#if !HAS_UNO || XAMARIN_ANDROID
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Data.Pdf;
using Windows.Storage;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Uno.UI.RuntimeTests.Tests.Windows_Data.Pdf;

[TestClass]
[RunsOnUIThread]
#if HAS_UNO && !XAMARIN_ANDROID
[Ignore("Not implemented yet.")]
#endif
public class Given_PdfDocument
{
	private const string PdfDocument_Password_Valid = "uno";
	private const string PdfDocument_Password_Invalid = "1";
	private const string PdfDocument_Name = "UnoA4.pdf";
	private const string PdfDocument_Name_Protected = "UnoA4_Protected.pdf";
	private const string ReferencePageImageUri = "UnoA4.png";
	private const string ReferencePageImage_ProtectedUri = "UnoA4_Protected.png";

	private static Stream? GetSreamFromResource(string resourceName)
	{
		var assembly = typeof(Given_PdfDocument).Assembly;
		var resourceFullName = assembly
			.GetManifestResourceNames()
			.FirstOrDefault(name => name.EndsWith(resourceName));

		Assert.IsNotNull(resourceFullName, $"Unable to find resource named {resourceName}.");

		return assembly.GetManifestResourceStream(resourceFullName);
	}

	private static async Task CheckDocumentAsync(PdfDocument? document
		, string referencePageImage
		, PdfPageRenderOptions? options = default
		, bool hasPassword = false)
	{
		// Check Common
		Assert.IsNotNull(document);
		Assert.AreEqual(1u, document.PageCount);
		Assert.AreEqual(hasPassword, document.IsPasswordProtected);

		// Got Page and check size
		var page = document.GetPage(0);
		Assert.IsNotNull(page);
		options ??= new PdfPageRenderOptions()
		{
			DestinationHeight = (uint)page.Size.Height,
			DestinationWidth = (uint)page.Size.Width,
		};

		// Render Page onto stream
		var pageStream = new InMemoryRandomAccessStream();
		await page.RenderToStreamAsync(pageStream, options);
		Assert.IsGreaterThan((uint)0, (uint)pageStream.Size, "Invalid size of page stream.");

		// Comparing rendered PdfPage with reference Page Image

		var SUT = new ScrollViewer()
		{
			HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
			VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
		};

		var renderHost = new StackPanel();

		SUT.Content = renderHost;

		var pdfImageSource = new BitmapImage();
		await pdfImageSource.SetSourceAsync(pageStream);
		var pdfImageHost = new Image()
		{
			Stretch = Microsoft.UI.Xaml.Media.Stretch.None,
			Source = pdfImageSource,
		};
		var refImageSource = new BitmapImage();
		await refImageSource.SetSourceAsync(GetSreamFromResource(referencePageImage)?.AsRandomAccessStream());
		var refImageHost = new Image()
		{
			Stretch = Microsoft.UI.Xaml.Media.Stretch.None,
			Source = refImageSource,
		};

		renderHost.Children.Add(pdfImageHost);
		renderHost.Children.Add(refImageHost);

		TestServices.WindowHelper.WindowContent = SUT;

		await TestServices.WindowHelper.WaitForLoaded(renderHost);
		await TestServices.WindowHelper.WaitForIdle();

		var rendererPdf = new RenderTargetBitmap();
		await rendererPdf.RenderAsync(pdfImageHost);
		var pdfImage = await RawBitmap.From(rendererPdf, pdfImageHost);

		var rendererRef = new RenderTargetBitmap();
		await rendererRef.RenderAsync(refImageHost);
		var refImage = await RawBitmap.From(rendererRef, pdfImageHost);

		await ImageAssert.AreSimilarAsync(pdfImage, refImage);
	}

	[TestMethod]
	public async Task When_LoadFromStreamAsync()
	{
		var stream = GetSreamFromResource(PdfDocument_Name)?.AsRandomAccessStream();
		Assert.IsNotNull(stream, "Not valid stream");

		PdfDocument? pdfDocument = await PdfDocument.LoadFromStreamAsync(stream);
		await CheckDocumentAsync(pdfDocument, ReferencePageImageUri);
	}

	[TestMethod]
	public async Task When_LoadFromStreamAsync_With_Valid_Password()
	{
		var stream = GetSreamFromResource(PdfDocument_Name_Protected)?.AsRandomAccessStream();
		Assert.IsNotNull(stream, "Not valid stream");

#if __ANDROID__
		await Assert.ThrowsAsync<NotImplementedException>(async () => await PdfDocument.LoadFromStreamAsync(stream, PdfDocument_Password_Valid));
#else
		var pdfDocument = await PdfDocument.LoadFromStreamAsync(stream, PdfDocument_Password_Valid);
		await CheckDocumentAsync(pdfDocument, ReferencePageImage_ProtectedUri, hasPassword: true);
#endif
	}

	[TestMethod]
	public async Task When_LoadFromStreamAsync_With_Wrong_Password()
	{
		var stream = GetSreamFromResource(PdfDocument_Name_Protected)?.AsRandomAccessStream();
		Assert.IsNotNull(stream, "Not valid stream");

#if __ANDROID__
		await Assert.ThrowsAsync<NotImplementedException>(async () => await PdfDocument.LoadFromStreamAsync(stream, PdfDocument_Password_Invalid));
#elif !HAS_UNO
		await Assert.ThrowsAsync<Exception>(async () => await PdfDocument.LoadFromStreamAsync(stream, PdfDocument_Password_Invalid));
#else
		var pdfDocument = await PdfDocument.LoadFromStreamAsync(stream, PdfDocument_Password_Valid);
		await CheckDocumentAsync(pdfDocument, ReferencePageImage_ProtectedUri, hasPassword: true);
#endif
	}


	[TestMethod]
	public async Task When_LoadFromFileAsync()
	{
		var testFolderName = Guid.NewGuid().ToString();
		var rootFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(testFolderName);
		var file = await rootFolder.CreateFileAsync(PdfDocument_Name);
		{
			using var ws = await file.OpenStreamForWriteAsync();
			var source = GetSreamFromResource(PdfDocument_Name);
			Assert.IsNotNull(source);
			await source.CopyToAsync(ws);
		}
		var pdfDocument = await PdfDocument.LoadFromFileAsync(file);
		await CheckDocumentAsync(pdfDocument, ReferencePageImageUri);
	}

	[TestMethod]
	public async Task When_LoadFromFileAsync_Valid_Password()
	{
		var testFolderName = Guid.NewGuid().ToString();
		var rootFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(testFolderName);
		var file = await rootFolder.CreateFileAsync(PdfDocument_Name_Protected);
		{
			using var ws = await file.OpenStreamForWriteAsync();
			var source = GetSreamFromResource(PdfDocument_Name_Protected);
			Assert.IsNotNull(source);
			await source.CopyToAsync(ws);
		}

#if __ANDROID__
		await Assert.ThrowsAsync<NotImplementedException>(async () => await PdfDocument.LoadFromStreamAsync(stream, PdfDocument_Password_Valid));
#else
		var pdfDocument = await PdfDocument.LoadFromFileAsync(file, PdfDocument_Password_Valid);
		await CheckDocumentAsync(pdfDocument, ReferencePageImage_ProtectedUri, hasPassword: true);
#endif
	}

	[TestMethod]
	public async Task When_LoadFromFileAsync_Wrong_Password()
	{
		var testFolderName = Guid.NewGuid().ToString();
		var rootFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(testFolderName);
		var file = await rootFolder.CreateFileAsync(PdfDocument_Name_Protected);
		{
			using var ws = await file.OpenStreamForWriteAsync();
			var source = GetSreamFromResource(PdfDocument_Name_Protected);
			Assert.IsNotNull(source);
			await source.CopyToAsync(ws);
		}

#if __ANDROID__
		await Assert.ThrowsAsync<NotImplementedException>(async () => await PdfDocument.LoadFromFileAsync(file, PdfDocument_Password_Invalid));
#elif !HAS_UNO
		await Assert.ThrowsAsync<Exception>(async () => await PdfDocument.LoadFromFileAsync(file, PdfDocument_Password_Invalid));
#else
		var pdfDocument = await PdfDocument.LoadFromFileAsync(file, PdfDocument_Password_Invalid);
		await CheckDocumentAsync(pdfDocument, ReferencePageImage_ProtectedUri, hasPassword: true);
#endif
	}

	[TestMethod]
	public async Task When_Crop()
	{
		var stream = GetSreamFromResource(PdfDocument_Name)?.AsRandomAccessStream();
		Assert.IsNotNull(stream, "Not valid stream");

		PdfDocument? pdfDocument = await PdfDocument.LoadFromStreamAsync(stream);
		await CheckDocumentAsync(pdfDocument, "UnoA4_Crop.png", new PdfPageRenderOptions()
		{
			BackgroundColor = Microsoft.UI.Colors.Beige,
			SourceRect = new Windows.Foundation.Rect(20, 16, 135, 48),
		});
	}

	[TestMethod]
	public async Task When_Arrange()
	{
		var stream = GetSreamFromResource(PdfDocument_Name)?.AsRandomAccessStream();
		Assert.IsNotNull(stream, "Not valid stream");

		PdfDocument? pdfDocument = await PdfDocument.LoadFromStreamAsync(stream);
		await CheckDocumentAsync(pdfDocument, "UnoA4_100x141.png", new()
		{
			DestinationWidth = 100,
		});
	}
}
#endif
