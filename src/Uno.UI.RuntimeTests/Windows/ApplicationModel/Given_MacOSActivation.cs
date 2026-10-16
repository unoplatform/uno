#if HAS_UNO
#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;

namespace Uno.UI.RuntimeTests.Tests.Windows_ApplicationModel;

[TestClass]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaMacOS)]
public class Given_MacOSActivation
{
	[TestMethod]
	[RunsOnUIThread]
	public void When_Url_Opened_While_Running()
	{
		var instance = AppInstance.GetCurrent();
		var startupKind = instance.GetActivatedEventArgs().Kind;

		List<AppActivationArguments> received = new();
		void OnActivated(object? sender, AppActivationArguments args) => received.Add(args);

		instance.Activated += OnActivated;
		try
		{
			Assert.IsTrue(TryReportUrl("web+unotest://macos/path?x=1"));
		}
		finally
		{
			instance.Activated -= OnActivated;
		}

		Assert.AreEqual(1, received.Count);
		Assert.AreEqual(ExtendedActivationKind.Protocol, received[0].Kind);

		var protocol = received[0].Data as ProtocolActivatedEventArgs;
		Assert.IsNotNull(protocol);
		Assert.AreEqual(new Uri("web+unotest://macos/path?x=1"), protocol!.Uri);
		Assert.AreEqual(ApplicationExecutionState.Running, protocol.PreviousExecutionState);

		// A URL opened into a running app is not the activation the app started with.
		Assert.AreEqual(startupKind, instance.GetActivatedEventArgs().Kind);
	}

	[TestMethod]
	[RunsOnUIThread]
	[DataRow(null)]
	[DataRow("")]
	[DataRow("not a uri")]
	public void When_Url_Is_Invalid(string? url)
	{
		var instance = AppInstance.GetCurrent();

		var raised = false;
		void OnActivated(object? sender, AppActivationArguments args) => raised = true;

		instance.Activated += OnActivated;
		try
		{
			Assert.IsFalse(TryReportUrl(url));
		}
		finally
		{
			instance.Activated -= OnActivated;
		}

		Assert.IsFalse(raised);
	}

	private static bool TryReportUrl(string? url)
	{
		var activationType = Type.GetType("Uno.UI.Runtime.MacOS.MacOSActivation, Uno.UI.Runtime.MacOS", throwOnError: false);
		Assert.IsNotNull(activationType, "MacOSActivation type was not found in Uno.UI.Runtime.MacOS.");

		var tryReportUrl = activationType.GetMethod("TryReportUrl", BindingFlags.NonPublic | BindingFlags.Static);
		Assert.IsNotNull(tryReportUrl, "MacOSActivation.TryReportUrl was not found.");

		return (bool)tryReportUrl.Invoke(null, new object?[] { url })!;
	}
}
#endif
