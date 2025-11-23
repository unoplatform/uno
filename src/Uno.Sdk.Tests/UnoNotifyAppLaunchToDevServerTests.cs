#nullable disable

using Microsoft.Build.Framework;
using Uno.Sdk.Tasks;

namespace Uno.Sdk.Tests;

[TestClass]
public class UnoNotifyAppLaunchToDevServerTests
{
	private sealed class TestBuildEngine : IBuildEngine
	{
		public List<BuildErrorEventArgs> Errors { get; } = new();
		public List<BuildWarningEventArgs> Warnings { get; } = new();
		public List<BuildMessageEventArgs> Messages { get; } = new();

		public bool ContinueOnError => false;
		public int LineNumberOfTaskNode => 0;
		public int ColumnNumberOfTaskNode => 0;
		public string ProjectFileOfTaskNode => string.Empty;

		public void LogErrorEvent(BuildErrorEventArgs e) => Errors.Add(e);
		public void LogWarningEvent(BuildWarningEventArgs e) => Warnings.Add(e);
		public void LogMessageEvent(BuildMessageEventArgs e) => Messages.Add(e);
		public void LogCustomEvent(CustomBuildEventArgs e) { }

		public bool BuildProjectFile(
			string projectFileName,
			string[] targetNames,
			System.Collections.IDictionary globalProperties,
			System.Collections.IDictionary targetOutputs) => true;
	}

	[TestMethod]
	public void Execute_WhenPortIsInvalid_LogsError()
	{
		// Arrange
		var buildEngine = new TestBuildEngine();
		var task = new UnoNotifyAppLaunchToDevServer_v0
		{
			Port = "invalid",
			TargetPath = "/some/path/app.dll",
			BuildEngine = buildEngine
		};

		// Act
		var result = task.Execute();

		// Assert
		result.Should().BeFalse("Task should return false for invalid port");
		task.Success.Should().BeFalse("Success property should be false");
		buildEngine.Errors.Should().HaveCount(1, "Should log exactly one error");
		buildEngine.Errors[0].Message.Should().Contain("UnoRemoteControlPort", "Error message should mention UnoRemoteControlPort");
	}

	[TestMethod]
	public void Execute_WhenPortIsZero_LogsError()
	{
		// Arrange
		var buildEngine = new TestBuildEngine();
		var task = new UnoNotifyAppLaunchToDevServer_v0
		{
			Port = "0",
			TargetPath = "/some/path/app.dll",
			BuildEngine = buildEngine
		};

		// Act
		var result = task.Execute();

		// Assert
		result.Should().BeFalse("Task should return false for port 0");
		task.Success.Should().BeFalse("Success property should be false");
		buildEngine.Errors.Should().HaveCount(1, "Should log exactly one error");
	}

	[TestMethod]
	public void Execute_WhenTargetPathIsEmpty_LogsError()
	{
		// Arrange
		var buildEngine = new TestBuildEngine();
		var task = new UnoNotifyAppLaunchToDevServer_v0
		{
			Port = "12345",
			TargetPath = "",
			BuildEngine = buildEngine
		};

		// Act
		var result = task.Execute();

		// Assert
		result.Should().BeFalse("Task should return false for empty TargetPath");
		task.Success.Should().BeFalse("Success property should be false");
		buildEngine.Errors.Should().HaveCount(1, "Should log exactly one error");
		buildEngine.Errors[0].Message.Should().Contain("TargetPath", "Error message should mention TargetPath");
	}

	[TestMethod]
	public void Execute_WhenServerIsNotAvailable_LogsError()
	{
		// Arrange - Use a port that's unlikely to have a server running
		var buildEngine = new TestBuildEngine();
		var task = new UnoNotifyAppLaunchToDevServer_v0
		{
			Port = "59999",
			TargetPath = "/some/path/app.dll",
			BuildEngine = buildEngine
		};

		// Act
		var result = task.Execute();

		// Assert
		result.Should().BeFalse("Task should return false when server is not available");
		task.Success.Should().BeFalse("Success property should be false");
		buildEngine.Errors.Should().HaveCount(1, "Should log exactly one error");
		buildEngine.Errors[0].Message.Should().Contain("NotifyDevServer", "Error message should contain NotifyDevServer prefix");
		buildEngine.Errors[0].Message.Should().Contain("Failed to notify dev server", "Error message should indicate failure to notify");
	}

	[TestMethod]
	public void Execute_WhenServerIsNotAvailable_ErrorMessageContainsUrl()
	{
		// Arrange
		var buildEngine = new TestBuildEngine();
		var task = new UnoNotifyAppLaunchToDevServer_v0
		{
			Port = "59999",
			TargetPath = "/some/path/app.dll",
			Ide = "VSCode",
			Plugin = "1.0.0",
			IsDebug = "true",
			BuildEngine = buildEngine
		};

		// Act
		var result = task.Execute();

		// Assert
		result.Should().BeFalse("Task should return false");
		buildEngine.Errors.Should().HaveCount(1, "Should log exactly one error");
		buildEngine.Errors[0].Message.Should().Contain("localhost:59999", "Error message should contain the server URL");
	}

	[TestMethod]
	public void Execute_OnException_LogsErrorNotWarning()
	{
		// Arrange - Invalid port number that will cause parse to fail, then execution to fail
		var buildEngine = new TestBuildEngine();
		var task = new UnoNotifyAppLaunchToDevServer_v0
		{
			Port = "70000", // Port number too high, will be out of ushort range
			TargetPath = "/some/path/app.dll",
			BuildEngine = buildEngine
		};

		// Act
		var result = task.Execute();

		// Assert
		result.Should().BeFalse("Task should return false");

		// Should log an error, not a warning
		buildEngine.Errors.Should().HaveCount(1, "Should log exactly one error");
		buildEngine.Warnings.Should().BeEmpty("Should not log any warnings");
	}
}
