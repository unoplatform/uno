#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Helpers;
using Windows.ApplicationModel;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.Storage.Helpers;
using Windows.Storage.Streams;

namespace Uno.UI.Tests.Windows_Storage;

// A host whose install folder is read-only (an MSIX package) and that shows another app's content keeps that
// content in a folder it can write to, and names it in WinRTFeatureConfiguration.Package.AdditionalContentRoots.
[TestClass]
public class Given_AdditionalContentRoots
{
	private string _root = null!;
	private string _installed = null!;
	private string _folder = null!;

	[TestInitialize]
	public void Initialize()
	{
		// A folder name of its own per test: resolved image paths are cached for the life of the process.
		_folder = "content-roots-" + Guid.NewGuid().ToString("N");
		_root = Path.Combine(Path.GetTempPath(), "uno-additional-root-" + Guid.NewGuid().ToString("N"));
		_installed = Path.Combine(Package.Current.InstalledPath, _folder);
		Directory.CreateDirectory(Path.Combine(_root, _folder));
		Directory.CreateDirectory(_installed);
		WinRTFeatureConfiguration.Package.AdditionalContentRoots = [_root];
	}

	[TestCleanup]
	public void Cleanup()
	{
		WinRTFeatureConfiguration.Package.AdditionalContentRoots = [];
		Directory.Delete(_root, recursive: true);
		Directory.Delete(_installed, recursive: true);
	}

	[TestMethod]
	public async Task When_FileIsOnlyInAnAdditionalRoot_Then_ApplicationUriFindsIt()
	{
		var path = WriteAdditional("only-there.txt", "additional");

		var file = await StorageFile.GetFileFromApplicationUriAsync(Uri("only-there.txt"));

		Assert.AreEqual(path, file.Path);
	}

	[TestMethod]
	public async Task When_FileIsInBoth_Then_TheInstallFolderWins()
	{
		WriteAdditional("both.txt", "additional");
		var installed = WriteInstalled("both.txt", "installed");

		var file = await StorageFile.GetFileFromApplicationUriAsync(Uri("both.txt"));

		Assert.AreEqual(installed, file.Path);
	}

	[TestMethod]
	public async Task When_FileIsNowhere_Then_ThePathStaysInTheInstallFolder()
	{
		// GetFileFromPathAsync does not check the file exists, so the path is what tells where it looked.
		var file = await StorageFile.GetFileFromApplicationUriAsync(Uri("missing.txt"));

		Assert.AreEqual(Path.Combine(_installed, "missing.txt"), file.Path);
	}

	[TestMethod]
	public async Task When_NoAdditionalRoot_Then_OnlyTheInstallFolderIsSearched()
	{
		WriteAdditional("ignored.txt", "additional");
		WinRTFeatureConfiguration.Package.AdditionalContentRoots = [];

		var file = await StorageFile.GetFileFromApplicationUriAsync(Uri("ignored.txt"));

		Assert.AreEqual(Path.Combine(_installed, "ignored.txt"), file.Path);
	}

	[TestMethod]
	public async Task When_ImageIsOnlyInAnAdditionalRoot_Then_ItsScaledFileIsFound()
	{
		WriteAdditional("picture.png", "100");
		var scaled = WriteAdditional("picture.scale-200.png", "200");

		var resolved = await PlatformImageHelpers.GetScaledPath(Uri("picture.png"), ResolutionScale.Scale200Percent);

		Assert.AreEqual(scaled, resolved);
	}

	[TestMethod]
	public async Task When_ImageHasNoScaledFile_Then_TheAdditionalRootsOwnFileIsFound()
	{
		var plain = WriteAdditional("plain.png", "100");

		var resolved = await PlatformImageHelpers.GetScaledPath(Uri("plain.png"), ResolutionScale.Scale100Percent);

		Assert.AreEqual(plain, resolved);
	}

	[TestMethod]
	public async Task When_ImageArrivesAfterItWasFirstAskedFor_Then_ItIsFound()
	{
		var missing = await PlatformImageHelpers.GetScaledPath(Uri("late.png"), ResolutionScale.Scale100Percent);
		var late = WriteAdditional("late.png", "100");

		var resolved = await PlatformImageHelpers.GetScaledPath(Uri("late.png"), ResolutionScale.Scale100Percent);

		Assert.AreEqual(Path.Combine(_installed, "late.png"), missing);
		Assert.AreEqual(late, resolved);
	}

	[TestMethod]
	public async Task When_ImageIsInBoth_Then_TheInstallFolderWins()
	{
		WriteAdditional("shared.png", "additional");
		var installed = WriteInstalled("shared.png", "installed");

		var resolved = await PlatformImageHelpers.GetScaledPath(Uri("shared.png"), ResolutionScale.Scale100Percent);

		Assert.AreEqual(installed, resolved);
	}

	[TestMethod]
	public async Task When_FileIsOnlyInAnAdditionalRoot_Then_ItExistsInThePackage()
	{
		WriteAdditional("font.manifest", "{}");

		Assert.IsTrue(await StorageFileHelper.ExistsInPackage(Path.Combine(_folder, "font.manifest")));
		Assert.IsFalse(await StorageFileHelper.ExistsInPackage(Path.Combine(_folder, "no-such.manifest")));
	}

	[TestMethod]
	public async Task When_FileIsOnlyInAnAdditionalRoot_Then_AStreamReferenceOpensIt()
	{
		WriteAdditional("stream.txt", "additional");

		using var stream = await RandomAccessStreamReference.CreateFromUri(Uri("stream.txt")).OpenReadAsync();
		using var reader = new StreamReader(stream.AsStreamForRead());

		Assert.AreEqual("additional", await reader.ReadToEndAsync());
	}

	private Uri Uri(string fileName) => new($"ms-appx:///{_folder}/{fileName}");

	private string WriteAdditional(string fileName, string content) => Write(Path.Combine(_root, _folder, fileName), content);

	private string WriteInstalled(string fileName, string content) => Write(Path.Combine(_installed, fileName), content);

	private static string Write(string path, string content)
	{
		File.WriteAllText(path, content);
		return path;
	}
}
