using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

namespace Uno.UI.RuntimeTests.Tests.Windows_System;

[TestClass]
public class Given_Launcher
{
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_LaunchFileAsync_Null_File()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => Launcher.LaunchFileAsync(null!));
		Assert.ThrowsExactly<ArgumentNullException>(() => Launcher.LaunchFileAsync(null!, null!));
	}

	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaWasm)]
	public async Task When_LaunchFileAsync_Deleted_File()
	{
		var file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync(
			$"{nameof(When_LaunchFileAsync_Deleted_File)}-{Guid.NewGuid():N}.txt");
		await file.DeleteAsync();

		Assert.IsFalse(await Launcher.LaunchFileAsync(file));
	}

#if HAS_UNO
	[TestMethod]
	[DataRow("/Users/test/file.txt", "file:///Users/test/file.txt")]
	[DataRow("/tmp/a b/report #1.pdf", "file:///tmp/a%20b/report%20%231.pdf")]
	[DataRow("/tmp/100%/what?.txt", "file:///tmp/100%25/what%3F.txt")]
	[DataRow("/tmp/čaj.png", "file:///tmp/%C4%8Daj.png")]
	public void When_GetFileUriString(string path, string expected)
	{
		Assert.AreEqual(expected, Launcher.GetFileUriString(path));
	}
#endif
}
