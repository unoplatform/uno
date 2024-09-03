using System;
using System.Threading.Tasks;
using FluentAssertions;
using Private.Infrastructure;
using Windows.Foundation;
using Windows.Storage;

namespace Uno.UI.RuntimeTests.Tests.Windows_Storage;

[TestClass]
public class Given_ApplicationData
{
	[TestMethod]
	public async Task When_SetVersion()
	{
		// Arrange
		var appData = ApplicationData.Current;
		var currentVersion = appData.Version;
		var desiredVersion = currentVersion + 5;
		// Act
		await appData.SetVersionAsync(desiredVersion, (e) => { });

		// Assert
		appData.Version.Should().Be(desiredVersion);
	}

	[TestMethod]
	public async Task When_SetVersion_Handler()
	{
		// Arrange
		var appData = ApplicationData.Current;
		var currentVersion = appData.Version;
		var desiredVersion = currentVersion + 5;

		// Act
		await appData.SetVersionAsync(desiredVersion, new ApplicationDataSetVersionHandler((setVersionRequest) =>
		{
			// Assert
			setVersionRequest.CurrentVersion.Should().Be(currentVersion);
			setVersionRequest.DesiredVersion.Should().Be(desiredVersion);
		}));

		await TestServices.WindowHelper.WaitFor(() => ApplicationData.Current.Version == desiredVersion);
	}

	[TestMethod]
	public async Task When_SetVersion_Deferred()
	{
		// Arrange
		var appData = ApplicationData.Current;
		var currentVersion = appData.Version;
		var desiredVersion = currentVersion + 5;

		SetVersionDeferral deferral = null;
		// Act
		var task = appData.SetVersionAsync(desiredVersion, new ApplicationDataSetVersionHandler((setVersionRequest) =>
		{
			// Assert
			setVersionRequest.CurrentVersion.Should().Be(currentVersion);
			setVersionRequest.DesiredVersion.Should().Be(desiredVersion);

			deferral = setVersionRequest.GetDeferral();
		})).AsTask();

		await TestServices.WindowHelper.WaitFor(() => deferral != null);

		Assert.IsFalse(task.IsCompleted);

		deferral.Complete();

		await TestServices.WindowHelper.WaitFor(() => task.IsCompleted);
	}
}
