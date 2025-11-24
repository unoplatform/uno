using System.Reflection;
using Microsoft.Build.Framework;
using BuildTask = Microsoft.Build.Utilities.Task;

namespace Uno.Sdk.Tests;

[TestClass]
public class UnoNotifyAppLaunchToDevServerTests
{
	private sealed class TestBuildEngine : IBuildEngine
	{
		public List<BuildErrorEventArgs> Errors { get; } = [];
		public List<BuildWarningEventArgs> Warnings { get; } = [];
		public List<BuildMessageEventArgs> Messages { get; } = [];

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
		var task = CreateTaskInstance(buildEngine, port: "invalid", targetPath: "/some/path/app.dll");

		// Act
		var result = (bool)task.Execute();

		// Assert
		result.Should().BeFalse("Task should return false for invalid port");
		((bool)task.Success).Should().BeFalse("Success property should be false");
		buildEngine.Errors.Should().HaveCount(1, "Should log exactly one error");
		buildEngine.Errors[0].Message.Should().Contain("UnoRemoteControlPort", "Error message should mention UnoRemoteControlPort");
	}

	[TestMethod]
	public void Execute_WhenPortIsZero_LogsError()
	{
		// Arrange
		var buildEngine = new TestBuildEngine();
		var task = CreateTaskInstance(buildEngine, port: "0", targetPath: "/some/path/app.dll");

		// Act
		var result = (bool)task.Execute();

		// Assert
		result.Should().BeFalse("Task should return false for port 0");
		((bool)task.Success).Should().BeFalse("Success property should be false");
		buildEngine.Errors.Should().HaveCount(1, "Should log exactly one error");
	}

	[TestMethod]
	public void Execute_WhenTargetPathIsEmpty_LogsError()
	{
		// Arrange
		var buildEngine = new TestBuildEngine();
		var task = CreateTaskInstance(buildEngine, port: "12345", targetPath: "");

		// Act
		var result = (bool)task.Execute();

		// Assert
		result.Should().BeFalse("Task should return false for empty TargetPath");
		((bool)task.Success).Should().BeFalse("Success property should be false");
		buildEngine.Errors.Should().HaveCount(1, "Should log exactly one error");
		buildEngine.Errors[0].Message.Should().Contain("TargetPath", "Error message should mention TargetPath");
	}

	[TestMethod]
	public void Execute_WhenServerIsNotAvailable_LogsError()
	{
		// Arrange - Use a port that's unlikely to have a server running
		var buildEngine = new TestBuildEngine();
		var task = CreateTaskInstance(buildEngine, port: "59999", targetPath: "/some/path/app.dll");

		// Act
		var result = (bool)task.Execute();

		// Assert
		result.Should().BeFalse("Task should return false when server is not available");
		((bool)task.Success).Should().BeFalse("Success property should be false");
		buildEngine.Errors.Should().HaveCount(1, "Should log exactly one error");
		buildEngine.Errors[0].Message.Should().Contain("NotifyDevServer", "Error message should contain NotifyDevServer prefix");
		buildEngine.Errors[0].Message.Should().Contain("Failed to notify dev server", "Error message should indicate failure to notify");
	}

	[TestMethod]
	public void Execute_WhenServerIsNotAvailable_ErrorMessageContainsUrl()
	{
		// Arrange
		var buildEngine = new TestBuildEngine();
		var task = CreateTaskInstance(buildEngine, port: "59999", targetPath: "/some/path/app.dll", ide: "VSCode", plugin: "1.0.0", isDebug: "true");

		// Act
		var result = (bool)task.Execute();

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
		var task = CreateTaskInstance(buildEngine, port: "70000", targetPath: "/some/path/app.dll");

		// Act
		var result = (bool)task.Execute();

		// Assert
		result.Should().BeFalse("Task should return false");

		// Should log an error, not a warning
		buildEngine.Errors.Should().HaveCount(1, "Should log exactly one error");
		buildEngine.Warnings.Should().BeEmpty("Should not log any warnings");
	}

	private static readonly Lazy<Assembly> UnoSdkAssembly = new(LoadUnoSdkAssembly);

	private static dynamic CreateTaskInstance(TestBuildEngine buildEngine, string port, string targetPath, string ide = "", string plugin = "", string isDebug = "")
	{
		var task = CreateTaskCore();

		task.Port = port;
		task.TargetPath = targetPath;
		task.Ide = ide;
		task.Plugin = plugin;
		task.IsDebug = isDebug;

		((BuildTask)task).BuildEngine = buildEngine;
		return task;
	}

	private static dynamic CreateTaskCore()
	{
		var taskType = UnoSdkAssembly.Value
			.GetTypes()
			.FirstOrDefault(t => t.FullName?.StartsWith("Uno.Sdk.Tasks.UnoNotifyAppLaunchToDevServer_", StringComparison.Ordinal) == true)
			?? throw new InvalidOperationException("Unable to locate UnoNotifyAppLaunchToDevServer task type.");

		return Activator.CreateInstance(taskType)
			?? throw new InvalidOperationException("Unable to instantiate UnoNotifyAppLaunchToDevServer task type.");
	}

	private static Assembly LoadUnoSdkAssembly()
	{
		var loadedAssembly = AppDomain.CurrentDomain
			.GetAssemblies()
			.FirstOrDefault(a => a.GetName().Name?.StartsWith("Uno.Sdk_", StringComparison.OrdinalIgnoreCase) is true);

		if (loadedAssembly is not null)
		{
			return loadedAssembly;
		}

		var baseDirectory = Path.GetDirectoryName(typeof(UnoNotifyAppLaunchToDevServerTests).Assembly.Location)
			?? throw new InvalidOperationException("Unable to determine test assembly directory.");

		var sdkBinary = Directory.EnumerateFiles(baseDirectory, "Uno.Sdk_*.dll").FirstOrDefault()
			?? Directory.EnumerateFiles(baseDirectory, "Uno.Sdk_v*.dll").FirstOrDefault();

		if (sdkBinary is null)
		{
			throw new InvalidOperationException("Unable to locate Uno.Sdk assembly. Ensure Uno.Sdk.Tests references Uno.Sdk.");
		}

		return Assembly.LoadFrom(sdkBinary);
	}
}
