#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.Tests.Windows_UI_Xaml;

[UITestClass]
public class Given_Application_OnLaunched
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24930")]
	public void When_OnLaunched_Throws_Then_UnhandledException_Is_Raised_And_It_Propagates()
	{
		RunWithThrowingApp(handle: false, app =>
		{
			var thrown = Assert.ThrowsExactly<InvalidOperationException>(app.InvokeOnLaunched);

			Assert.AreSame(thrown, GetRaised(app).Single(), "Application.UnhandledException must report the exception OnLaunched threw.");
		});
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24930")]
	public void When_OnLaunched_Throws_And_Is_Handled_Then_It_Does_Not_Propagate()
	{
		RunWithThrowingApp(handle: true, app =>
		{
			app.InvokeOnLaunched();

			Assert.HasCount(1, GetRaised(app));
			Assert.IsTrue(app.WasLaunched);
		});
	}

	private static IReadOnlyList<Exception> GetRaised(Application app)
		=> (IReadOnlyList<Exception>)app.GetType().GetProperty(nameof(OnLaunchedThrowingTestApplication.Raised))!.GetValue(app)!;

	/// <summary>
	/// The process already runs the unit-test app, so the throwing app is created in a secondary ALC,
	/// the way a secondary app is bootstrapped, rather than through <see cref="Application.Start"/>.
	/// </summary>
	private static void RunWithThrowingApp(bool handle, Action<Application> test)
	{
		var alc = new AssemblyLoadContext(nameof(Given_Application_OnLaunched), isCollectible: true);
		try
		{
			var assembly = alc.LoadFromAssemblyPath(typeof(Given_Application_OnLaunched).Assembly.Location);
			var appType = assembly.GetType(typeof(OnLaunchedThrowingTestApplication).FullName!, throwOnError: true)!;
			var app = (Application)Activator.CreateInstance(appType, [handle])!;
			try
			{
				test(app);
			}
			finally
			{
				Application.RemoveAlcApplication(alc);
			}
		}
		finally
		{
			alc.Unload();
		}
	}
}

public class OnLaunchedThrowingTestApplication : Application
{
	private readonly List<Exception> _raised = new();

	public OnLaunchedThrowingTestApplication(bool handle)
	{
		UnhandledException += (_, e) =>
		{
			_raised.Add(e.Exception);
			e.Handled = handle;
		};
	}

	public IReadOnlyList<Exception> Raised => _raised;

	protected override void OnLaunched(LaunchActivatedEventArgs args) => throw new InvalidOperationException("Thrown from OnLaunched.");
}
