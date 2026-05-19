#nullable enable

using System;
using System.Threading;
using NUnit.Framework;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace SamplesApp.AppiumTests.Infrastructure;

/// <summary>
/// Spins up an Appium session for the platform selected by the
/// <c>UNO_APPIUM_PLATFORM</c> environment variable. Tests inherit and write
/// platform-agnostic assertions against <see cref="Adapter"/>.
/// </summary>
public abstract class AppiumFixtureBase
{
	private IWebDriver? _driver;

	protected IPlatformAdapter Adapter { get; private set; } = null!;

	protected IWebDriver Driver
		=> _driver ?? throw new InvalidOperationException("Driver not initialized.");

	/// <summary>
	/// Override to point the fixture at a specific sample. Format is the same
	/// string SamplesApp's App.Tests.TryNavigateToLaunchSample expects:
	/// <c>sample=Category/SampleName</c>.
	/// </summary>
	protected abstract string SampleQuery { get; }

	[OneTimeSetUp]
	public void Initialize()
	{
		var platform = AppiumPlatformResolver.TryResolve();
		if (platform is null)
		{
			Assert.Ignore(
				$"Set {AppiumPlatformResolver.EnvVarPlatform}=windows|mac|wasm and " +
				$"{AppiumPlatformResolver.EnvVarAppPath} to enable Appium fixtures.");
			return;
		}

		Adapter = platform switch
		{
			AppiumPlatform.Windows => new WindowsAdapter(),
			AppiumPlatform.Mac => new MacAdapter(),
			AppiumPlatform.Wasm => new WasmAdapter(),
			_ => throw new NotSupportedException(),
		};

		_driver = Adapter.CreateDriver(SampleQuery);
		_driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2);
	}

	[OneTimeTearDown]
	public void Cleanup()
	{
		try
		{
			_driver?.Quit();
		}
		catch
		{
			// best-effort teardown
		}
		finally
		{
			_driver?.Dispose();
			_driver = null;
			Adapter?.Dispose();
		}
	}

	protected IWebElement WaitForAutomationId(string automationId, TimeSpan? timeout = null)
	{
		var wait = new WebDriverWait(Driver, timeout ?? TimeSpan.FromSeconds(15))
		{
			PollingInterval = TimeSpan.FromMilliseconds(200),
		};
		wait.IgnoreExceptionTypes(typeof(NoSuchElementException), typeof(StaleElementReferenceException));

		return wait.Until(d =>
		{
			var element = d.FindElement(Adapter.ByAutomationId(automationId));
			return element.Displayed ? element : null;
		}) ?? throw new InvalidOperationException($"Element '{automationId}' never became visible.");
	}

	protected static void WaitFor(Func<bool> condition, TimeSpan? timeout = null, string? because = null)
	{
		var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
		while (DateTime.UtcNow < deadline)
		{
			if (condition())
			{
				return;
			}

			Thread.Sleep(100);
		}

		Assert.Fail(because ?? "Wait condition never became true.");
	}
}
