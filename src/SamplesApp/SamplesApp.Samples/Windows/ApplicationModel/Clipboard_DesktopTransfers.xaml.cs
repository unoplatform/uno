#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;

namespace UITests.Windows_ApplicationModel;

[Sample("Windows.ApplicationModel", IsManualTest = true, IgnoreInSnapshotTests = true, Description = "Desktop clipboard formats, portal file selection and drag/drop negotiation.")]
public sealed partial class Clipboard_DesktopTransfers : Page
{
	public Clipboard_DesktopTransfers() => this.InitializeComponent();

	private void CopyText(object sender, RoutedEventArgs e)
	{
		try
		{
			var package = new DataPackage();
			package.SetText(Input.Text);
			package.SetHtmlFormat($"<p>{System.Net.WebUtility.HtmlEncode(Input.Text)}</p>");
			Clipboard.SetContent(package);
			Result.Text = "Text and HTML submitted to clipboard";
		}
		catch (Exception error)
		{
			Result.Text = error.Message;
		}
	}

	private async void CopyFiles(object sender, RoutedEventArgs e)
	{
		try
		{
			var picker = new FileOpenPicker();
			picker.FileTypeFilter.Add("*");
			var files = await picker.PickMultipleFilesAsync();
			if (files.Count > 0)
			{
				var package = new DataPackage();
				package.SetStorageItems(files);
				Clipboard.SetContent(package);
				Result.Text = $"Submitted {files.Count} files";
			}
		}
		catch (Exception error)
		{
			Result.Text = error.Message;
		}
	}

	private async void CopyImage(object sender, RoutedEventArgs e)
	{
		try
		{
			var picker = new FileOpenPicker();
			picker.FileTypeFilter.Add(".png");
			var file = await picker.PickSingleFileAsync();
			if (file is not null)
			{
				var package = new DataPackage();
				package.SetBitmap(RandomAccessStreamReference.CreateFromFile(file));
				Clipboard.SetContent(package);
				Result.Text = "Image submitted to clipboard";
			}
		}
		catch (Exception error)
		{
			Result.Text = error.Message;
		}
	}

	private async void Paste(object sender, RoutedEventArgs e)
	{
		try
		{
			Result.Text = Clipboard.GetContent() is { } content ? await Describe(content) : "The clipboard is empty";
		}
		catch (Exception error)
		{
			Result.Text = error.Message;
		}
	}

	private void DragOverTarget(object sender, DragEventArgs e)
	{
		e.AcceptedOperation = e.AllowedOperations & (AcceptMove.IsOn
			? DataPackageOperation.Copy | DataPackageOperation.Move : DataPackageOperation.Copy);
	}

	private async void DropTarget(object sender, DragEventArgs e)
	{
		var deferral = e.GetDeferral();
		try
		{
			Result.Text = await Describe(e.DataView);
		}
		catch (Exception error)
		{
			Result.Text = error.Message;
		}
		finally
		{
			deferral.Complete();
		}
	}

	private static async Task<string> Describe(DataPackageView view)
	{
		var result = $"Formats: {string.Join(", ", view.AvailableFormats)}";
		if (view.Contains(StandardDataFormats.Text))
		{
			result += $"\nText: {await view.GetTextAsync()}";
		}
		if (view.Contains(StandardDataFormats.Html))
		{
			result += $"\nHTML: {await view.GetHtmlFormatAsync()}";
		}
		if (view.Contains(StandardDataFormats.StorageItems))
		{
			result += $"\nFiles: {string.Join(", ", (await view.GetStorageItemsAsync()).Select(item => item.Path))}";
		}
		if (view.Contains(StandardDataFormats.Bitmap))
		{
			using var stream = await (await view.GetBitmapAsync()).OpenReadAsync();
			result += $"\nImage: {stream.Size} bytes";
		}
		return result;
	}
}
