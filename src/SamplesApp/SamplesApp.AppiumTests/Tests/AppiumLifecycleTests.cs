#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using SamplesApp.AppiumTests.Infrastructure;

namespace SamplesApp.AppiumTests.Tests;

[TestClass]
[DoNotParallelize]
[TestCategory(TestCategories.HostIndependent)]
public sealed class AppiumLifecycleTests
{
	public TestContext TestContext { get; set; } = null!;

	[TestMethod]
	public void Dispose_Attempts_All_Cleanup_And_Reports_Every_NonCritical_Failure()
	{
		var calls = new List<string>();
		var quitError = new System.IO.IOException("Quit transport failure");
		var disposeError = new WebDriverException("Driver disposal failure");
		var adapterError = new UnauthorizedAccessException("Adapter cleanup failure");
		var driver = CreateDriver(calls, quitError, disposeError);
		var adapter = CreateAdapter(calls, driver, adapterError);
		var session = new AppiumTestSession(TestContext, CreateOptions(), adapter, driver, "sample=test");

		var dispose = () => session.Dispose();
		var failure = dispose.Should().Throw<AggregateException>().Which;

		calls.Should().Equal("Quit", "Driver.Dispose", "Adapter.Dispose");
		failure.InnerExceptions.Should().HaveCount(3);
		failure.InnerExceptions[0].InnerException.Should().BeSameAs(quitError);
		failure.InnerExceptions[1].InnerException.Should().BeSameAs(disposeError);
		failure.InnerExceptions[2].InnerException.Should().BeSameAs(adapterError);
		session.Dispose();
		calls.Should().HaveCount(3);
	}

	[TestMethod]
	public void Create_Preserves_Startup_And_All_Partial_Cleanup_Failures()
	{
		var calls = new List<string>();
		var startupError = new InvalidOperationException("Timeout configuration failed");
		var quitError = new WebDriverException("Quit failed");
		var disposeError = new ObjectDisposedException("driver");
		var adapterError = new System.IO.IOException("Artifact cleanup failed");
		var driver = CreateDriver(calls, quitError, disposeError, startupError);
		var adapter = CreateAdapter(calls, driver, adapterError);

		var create = () => AppiumTestSession.Create(TestContext, CreateOptions(), adapter, "sample=test");
		var failure = create.Should().Throw<AggregateException>().Which;

		calls.Should().Equal("Quit", "Driver.Dispose", "Adapter.Dispose");
		failure.InnerExceptions.Should().Equal(startupError, quitError, disposeError, adapterError);
	}

	[TestMethod]
	public void Create_Disposes_Adapter_When_No_Driver_Was_Returned()
	{
		var calls = new List<string>();
		var startupError = new WebDriverException("Startup failed");
		var adapter = CreateProxy<IPlatformAdapter>(method =>
		{
			if (method.Name == nameof(IPlatformAdapter.CreateDriver))
			{
				throw startupError;
			}
			if (method.Name == nameof(IDisposable.Dispose))
			{
				calls.Add("Adapter.Dispose");
				return null;
			}
			throw new NotSupportedException(method.Name);
		});

		var create = () => AppiumTestSession.Create(TestContext, CreateOptions(), adapter, "sample=test");
		create.Should().Throw<InvalidOperationException>().Which.InnerException.Should().BeSameAs(startupError);
		calls.Should().Equal("Adapter.Dispose");
	}

	[TestMethod]
	[DataRow(UnitTestOutcome.Failed)]
	[DataRow(UnitTestOutcome.Passed)]
	public void Fixture_Propagates_Cleanup_Failure_Regardless_Of_Test_Outcome(UnitTestOutcome outcome)
	{
		var calls = new List<string>();
		var quitError = new WebDriverException("Quit failed");
		var driver = CreateDriver(calls, quitError);
		var adapter = CreateAdapter(calls, driver);
		var context = new OutcomeTestContext(outcome);
		var session = new AppiumTestSession(context, CreateOptions(), adapter, driver, "sample=test");
		var fixture = new TestFixture { TestContext = context };
		var sessionField = typeof(AppiumFixtureBase).GetField("_session", BindingFlags.NonPublic | BindingFlags.Instance)!;
		sessionField.SetValue(fixture, session);

		var cleanup = () => fixture.CleanupTest();
		cleanup.Should().Throw<InvalidOperationException>().Which.InnerException.Should().BeSameAs(quitError);

		calls.Should().Equal("Quit", "Driver.Dispose", "Adapter.Dispose");
		sessionField.GetValue(fixture).Should().BeNull();
		fixture.CleanupTest();
		calls.Should().HaveCount(3);
	}

	[TestMethod]
	public void Create_Does_Not_Wrap_Critical_Failures()
	{
		var calls = new List<string>();
		var critical = new OutOfMemoryException("Synthetic critical failure");
		var driver = CreateDriver(calls, startupError: critical);
		var adapter = CreateAdapter(calls, driver);

		var create = () => AppiumTestSession.Create(TestContext, CreateOptions(), adapter, "sample=test");
		create.Should().Throw<OutOfMemoryException>().Which.Should().BeSameAs(critical);
		calls.Should().BeEmpty();
	}

	[TestMethod]
	public void Dispose_Does_Not_Aggregate_Critical_Failures()
	{
		var calls = new List<string>();
		var critical = new OutOfMemoryException("Synthetic critical failure");
		var driver = CreateDriver(calls, quitError: critical);
		var adapter = CreateAdapter(calls, driver);
		var session = new AppiumTestSession(TestContext, CreateOptions(), adapter, driver, "sample=test");

		var dispose = () => session.Dispose();
		dispose.Should().Throw<OutOfMemoryException>().Which.Should().BeSameAs(critical);
		calls.Should().Equal("Quit");
	}

	private static IWebDriver CreateDriver(List<string> calls, Exception? quitError = null, Exception? disposeError = null, Exception? startupError = null)
		=> CreateProxy<IWebDriver>(method =>
		{
			switch (method.Name)
			{
				case nameof(IWebDriver.Manage):
					throw startupError ?? new NotSupportedException("Only failure-path initialization is expected.");
				case nameof(IWebDriver.Quit):
					calls.Add("Quit");
					if (quitError is not null)
					{
						throw quitError;
					}
					return null;
				case nameof(IDisposable.Dispose):
					calls.Add("Driver.Dispose");
					if (disposeError is not null)
					{
						throw disposeError;
					}
					return null;
				default:
					throw new NotSupportedException(method.Name);
			}
		});

	private static IPlatformAdapter CreateAdapter(List<string> calls, IWebDriver driver, Exception? disposeError = null)
		=> CreateProxy<IPlatformAdapter>(method =>
		{
			if (method.Name == nameof(IPlatformAdapter.CreateDriver))
			{
				return driver;
			}
			if (method.Name == nameof(IDisposable.Dispose))
			{
				calls.Add("Adapter.Dispose");
				if (disposeError is not null)
				{
					throw disposeError;
				}
				return null;
			}
			throw new NotSupportedException(method.Name);
		});

	private static T CreateProxy<T>(Func<MethodInfo, object?> invoke) where T : class
	{
		var proxy = DispatchProxy.Create<T, OperationProxy>();
		((OperationProxy)(object)proxy).Handler = invoke;
		return proxy;
	}

	private static AppiumTestOptions CreateOptions()
	{
		using var scope = new EnvironmentVariableScope();
		scope.Set(AppiumTestOptions.EnvVarPlatform, "wasm");
		scope.Set(AppiumTestOptions.EnvVarAppPath, "http://127.0.0.1:8000/");
		foreach (var variable in new[]
		{
			AppiumTestOptions.EnvVarAppiumServer, AppiumTestOptions.EnvVarArtifactsDir,
			AppiumTestOptions.EnvVarTimeoutSeconds, AppiumTestOptions.EnvVarPollIntervalMilliseconds,
			AppiumTestOptions.EnvVarRecordSnapshots, AppiumTestOptions.EnvVarKeepBundle,
			AppiumTestOptions.EnvVarChromeBinary, AppiumTestOptions.EnvVarChromeArguments,
		})
		{
			scope.Set(variable, null);
		}
		return AppiumTestOptions.LoadRequired("AppiumArtifacts");
	}

	public class OperationProxy : DispatchProxy
	{
		public Func<MethodInfo, object?> Handler { get; set; } = null!;
		protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Handler(targetMethod!);
	}

	private sealed class TestFixture : AppiumFixtureBase
	{
		protected override string SampleQuery => "sample=test";
	}

	private sealed class OutcomeTestContext(UnitTestOutcome outcome) : TestContext
	{
		public override IDictionary<string, object?> Properties { get; } = new Dictionary<string, object?>();
		public override UnitTestOutcome CurrentTestOutcome => outcome;
		public override void AddResultFile(string fileName) { }
		public override void Write(string? message) { }
		public override void Write(string format, params object?[] args) { }
		public override void WriteLine(string? message) { }
		public override void WriteLine(string format, params object?[] args) { }
		public override void DisplayMessage(MessageLevel messageLevel, string message) { }
	}
}
