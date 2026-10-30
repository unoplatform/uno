using AwesomeAssertions;
using Uno.UI.DevServer.Cli.Helpers;

namespace Uno.UI.DevServer.Cli.Tests;

[TestClass]
public class Given_LocalPathGuard
{
	[TestMethod]
	[OSCondition(OperatingSystems.Windows)]
	[DataRow(@"C:\repo\App.slnx")]
	[DataRow(@"c:/repo/App.slnx")]
	[Description("A drive-rooted path is local")]
	public void IsLocalPath_WhenDriveRooted_ReturnsTrue(string path)
		=> LocalPathGuard.IsLocalPath(path).Should().BeTrue();

	[TestMethod]
	[OSCondition(OperatingSystems.Windows)]
	[DataRow(@"\\server\share\App.slnx")]
	[DataRow("//server/share/App.slnx")]
	[DataRow(@"/\server\share\App.slnx")]
	[DataRow(@"\\server@SSL\DavWWWRoot\App.slnx")]
	[DataRow(@"\\?\UNC\server\share\App.slnx")]
	[DataRow(@"\??\UNC\server\share\App.slnx")]
	[DataRow(@"\\?\C:\repo\App.slnx")]
	[DataRow(@"\\.\C:\repo\App.slnx")]
	[DataRow(@"\??\C:\repo\App.slnx")]
	[DataRow(@"C:repo\App.slnx")]
	[Description("Anything but a drive-rooted path is refused: UNC and WebDAV shares, and every device or NT namespace prefix that can reach one")]
	public void IsLocalPath_WhenNotDriveRooted_ReturnsFalse(string path)
		=> LocalPathGuard.IsLocalPath(path).Should().BeFalse();

	[TestMethod]
	[OSCondition(OperatingSystems.Linux | OperatingSystems.OSX)]
	[Description("On Unix a network mount is a local path")]
	public void IsLocalPath_OnUnix_ReturnsTrue()
		=> LocalPathGuard.IsLocalPath("/mnt/share/App.slnx").Should().BeTrue();
}
