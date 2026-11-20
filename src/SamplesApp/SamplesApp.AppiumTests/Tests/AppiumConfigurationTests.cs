#nullable enable

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SamplesApp.AppiumTests.Infrastructure;

namespace SamplesApp.AppiumTests.Tests;

[TestClass]
public sealed class AppiumConfigurationTests
{
	[TestMethod]
	[TestCategory(TestCategories.HostIndependent)]
	public void LoadRequired_ThrowsClearError_WhenPlatformIsMissing()
	{
		using var scope = new EnvironmentVariableScope();
		scope.Set(AppiumTestOptions.EnvVarPlatform, null);
		scope.Set(AppiumTestOptions.EnvVarAppPath, null);

		var exception = CaptureFailure(() => AppiumTestOptions.LoadRequired(@"C:\AppiumArtifacts"));

		exception.Message.Should().Contain(AppiumTestOptions.EnvVarPlatform);
	}

	[TestMethod]
	[TestCategory(TestCategories.HostIndependent)]
	public void LoadRequired_ValidatesWasmAppUrl()
	{
		using var scope = new EnvironmentVariableScope();
		scope.Set(AppiumTestOptions.EnvVarPlatform, "wasm");
		scope.Set(AppiumTestOptions.EnvVarAppPath, @"C:\not-a-url");

		var exception = CaptureFailure(() => AppiumTestOptions.LoadRequired(@"C:\AppiumArtifacts"));

		exception.Message.Should().Contain("http(s) URL");
	}

	[TestMethod]
	[TestCategory(TestCategories.HostIndependent)]
	public void LoadRequired_ParsesBooleanAndTimingOverrides()
	{
		using var scope = new EnvironmentVariableScope();
		var chromeBinary = Environment.ProcessPath
			?? throw new AssertFailedException("The current process path is unavailable.");
		scope.Set(AppiumTestOptions.EnvVarPlatform, "wasm");
		scope.Set(AppiumTestOptions.EnvVarAppPath, "https://localhost:8123/");
		scope.Set(AppiumTestOptions.EnvVarAppiumServer, "http://127.0.0.1:4723/wd/hub");
		scope.Set(AppiumTestOptions.EnvVarRecordSnapshots, "true");
		scope.Set(AppiumTestOptions.EnvVarKeepBundle, "yes");
		scope.Set(AppiumTestOptions.EnvVarTimeoutSeconds, "15");
		scope.Set(AppiumTestOptions.EnvVarPollIntervalMilliseconds, "125");
		scope.Set(AppiumTestOptions.EnvVarArtifactsDir, @"C:\AppiumArtifacts\Run01");
		scope.Set(AppiumTestOptions.EnvVarChromeBinary, chromeBinary);
		scope.Set(AppiumTestOptions.EnvVarChromeArguments, "--headless=new|--disable-gpu");

		var options = AppiumTestOptions.LoadRequired(@"C:\ignored");

		options.Platform.Should().Be(AppiumPlatform.Wasm);
		options.ServerUri.AbsoluteUri.Should().Be("http://127.0.0.1:4723/wd/hub");
		options.RecordSnapshots.Should().BeTrue();
		options.KeepMacBundle.Should().BeTrue();
		options.Timeout.Should().Be(TimeSpan.FromSeconds(15));
		options.PollInterval.Should().Be(TimeSpan.FromMilliseconds(125));
		options.ArtifactsDirectory.Should().Be(@"C:\AppiumArtifacts\Run01");
		options.ChromeBinaryPath.Should().Be(chromeBinary);
		options.ChromeArguments.Should().Equal("--headless=new", "--disable-gpu");
	}

	private static InvalidOperationException CaptureFailure(Action action)
	{
		try
		{
			action();
		}
		catch (InvalidOperationException ex)
		{
			return ex;
		}

		throw new AssertFailedException("Expected InvalidOperationException to be thrown.");
	}
}
