#nullable enable

using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.ApplicationModel.DataTransfer;

namespace Uno.UI.Runtime.Wayland.Tests;

[TestClass]
public sealed class DragFormatsTests
{
	[TestMethod]
	public void FileListDragOffersUriListFirstAndTextForTerminals()
	{
		var package = new DataPackage();
		package.SetData(WaylandFileUris.UriListMimeType, "file:///tmp/a%20b.txt\r\nfile:///tmp/c\r\n");
		var mimeTypes = WaylandDataFormats.GetDragMimeTypes(package.GetView());
		Assert.AreEqual(WaylandFileUris.UriListMimeType, mimeTypes[0]);
		CollectionAssert.IsSubsetOf(WaylandDataFormats.TextMimeTypes, mimeTypes);
		// The clipboard verb format is not part of a drag, and the format is offered once.
		CollectionAssert.DoesNotContain(mimeTypes, WaylandFileUris.GnomeCopiedFilesMimeType);
		Assert.AreEqual(1, mimeTypes.Count(mime => mime == WaylandFileUris.UriListMimeType));
	}

	[TestMethod]
	public async Task FileListIsSentAsUriListAndAsUriText()
	{
		var package = new DataPackage();
		package.SetData(WaylandFileUris.UriListMimeType, "file:///tmp/a%20b.txt\r\nfile:///tmp/c\r\n");
		var view = package.GetView();
		var uriList = await WaylandDataFormats.GetBytesAsync(view, WaylandFileUris.UriListMimeType);
		Assert.AreEqual("file:///tmp/a%20b.txt\r\nfile:///tmp/c\r\n", Encoding.UTF8.GetString(uriList!));
		var text = await WaylandDataFormats.GetBytesAsync(view, "text/plain;charset=utf-8");
		Assert.AreEqual("file:///tmp/a%20b.txt\nfile:///tmp/c", Encoding.UTF8.GetString(text!));
	}

	[TestMethod]
	public async Task TextContentWinsOverTheFileListAsText()
	{
		var package = new DataPackage();
		package.SetData(WaylandFileUris.UriListMimeType, "file:///tmp/c\r\n");
		package.SetText("hello");
		var text = await WaylandDataFormats.GetBytesAsync(package.GetView(), "text/plain");
		Assert.AreEqual("hello", Encoding.UTF8.GetString(text!));
	}

	[TestMethod]
	public void TextOnlyDragOffersNoFileList()
	{
		var package = new DataPackage();
		package.SetText("hello");
		var mimeTypes = WaylandDataFormats.GetDragMimeTypes(package.GetView());
		CollectionAssert.DoesNotContain(mimeTypes, WaylandFileUris.UriListMimeType);
		CollectionAssert.AreEqual(WaylandDataFormats.TextMimeTypes, mimeTypes);
	}

	[TestMethod]
	public void PrivateOnlyContentOffersNothing()
	{
		var package = new DataPackage();
		package.Properties["Files_ActionBinder"] = "x";
		Assert.AreEqual(0, WaylandDataFormats.GetDragMimeTypes(package.GetView()).Count);
	}

	[TestMethod]
	public void OperationsMapToDndActionsWithoutLink()
	{
		Assert.AreEqual(3u, WaylandDataFormats.ToDndActions(DataPackageOperation.Copy | DataPackageOperation.Move | DataPackageOperation.Link));
		Assert.AreEqual(0u, WaylandDataFormats.ToDndActions(DataPackageOperation.Link));
		Assert.AreEqual(DataPackageOperation.Move, WaylandDataFormats.ToOperations(WaylandDragSourceState.ActionMove));
		Assert.AreEqual(DataPackageOperation.None, WaylandDataFormats.ToOperations(0));
	}
}
