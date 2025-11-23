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
		Assert.IsFalse(result, "Task should return false for invalid port");
		Assert.IsFalse(task.Success, "Success property should be false");
		Assert.AreEqual(1, buildEngine.Errors.Count, "Should log exactly one error");
		var errorMessage = buildEngine.Errors.Count > 0 ? buildEngine.Errors[0]!.Message : string.Empty;
		Assert.IsTrue(
			errorMessage.Contains("UnoRemoteControlPort"),
			"Error message should mention UnoRemoteControlPort");
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
		Assert.IsFalse(result, "Task should return false for port 0");
		Assert.IsFalse(task.Success, "Success property should be false");
		Assert.AreEqual(1, buildEngine.Errors.Count, "Should log exactly one error");
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
		Assert.IsFalse(result, "Task should return false for empty TargetPath");
		Assert.IsFalse(task.Success, "Success property should be false");
		Assert.AreEqual(1, buildEngine.Errors.Count, "Should log exactly one error");
		var errorMessage = buildEngine.Errors.Count > 0 ? buildEngine.Errors[0]!.Message : string.Empty;
		Assert.IsTrue(
			errorMessage.Contains("TargetPath"),
			"Error message should mention TargetPath");
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
		Assert.IsFalse(result, "Task should return false when server is not available");
		Assert.IsFalse(task.Success, "Success property should be false");
		Assert.AreEqual(1, buildEngine.Errors.Count, "Should log exactly one error");
		var errorMessage = buildEngine.Errors.Count > 0 ? buildEngine.Errors[0]!.Message : string.Empty;
		Assert.IsTrue(
			errorMessage.Contains("NotifyDevServer"),
			"Error message should contain NotifyDevServer prefix");
		Assert.IsTrue(
			errorMessage.Contains("Failed to notify dev server"),
			"Error message should indicate failure to notify");
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
		Assert.IsFalse(result, "Task should return false");
		Assert.AreEqual(1, buildEngine.Errors.Count, "Should log exactly one error");

		var errorMessage = buildEngine.Errors.Count > 0 ? buildEngine.Errors[0]!.Message : string.Empty;
		Assert.IsTrue(
			errorMessage.Contains("localhost:59999"),
			"Error message should contain the server URL");
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
		Assert.IsFalse(result, "Task should return false");

		// Should log an error, not a warning
		Assert.AreEqual(1, buildEngine.Errors.Count, "Should log exactly one error");
		Assert.AreEqual(0, buildEngine.Warnings.Count, "Should not log any warnings");
	}
}
