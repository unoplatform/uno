#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;

namespace Uno.UI.RuntimeTests.Tests.Windows_ApplicationModel;

[TestClass]
public class Given_AppInstance
{
	[TestMethod]
	public void When_GetActivatedEventArgs()
	{
		var args = AppInstance.GetCurrent().GetActivatedEventArgs();

		Assert.IsNotNull(args);
		Assert.AreEqual(ExtendedActivationKind.Launch, args.Kind);

		var launchArgs = args.Data as LaunchActivatedEventArgs;
		Assert.IsNotNull(launchArgs, $"A Launch activation must carry a LaunchActivatedEventArgs, got {args.Data?.GetType().FullName ?? "<null>"}.");

		// WinAppSDK reports the whole command line here, executable included, and deliberately not
		// the executable-stripped form that Application.OnLaunched receives.
		Assert.AreEqual(Environment.CommandLine, launchArgs!.Arguments);
	}

	[TestMethod]
	public void When_GetActivatedEventArgs_Called_Twice()
	{
		var instance = AppInstance.GetCurrent();

		var first = instance.GetActivatedEventArgs();
		var second = instance.GetActivatedEventArgs();

		Assert.IsNotNull(first);
		Assert.IsNotNull(second);
		Assert.AreEqual(first.Kind, second.Kind);
	}

	[TestMethod]
	public void When_SetOrRaiseActivation_After_Launch()
	{
		var instance = AppInstance.GetCurrent();
		var expected = CreateProtocolActivation("web+unotest://raised-once");

		var received = new List<AppActivationArguments>();
		var senders = new List<object?>();
		void OnActivated(object? sender, AppActivationArguments args)
		{
			senders.Add(sender);
			received.Add(args);
		}

		instance.Activated += OnActivated;
		try
		{
			instance.SetOrRaiseActivation(expected);
		}
		finally
		{
			// AppInstance is a process singleton, so a leaked handler would corrupt every later test.
			instance.Activated -= OnActivated;
		}

		Assert.AreEqual(1, received.Count);
		Assert.AreSame(expected, received[0]);
		Assert.AreSame(instance, senders[0]);
	}

	[TestMethod]
	public void When_Activation_Raised_Then_GetActivatedEventArgs_Unchanged()
	{
		var instance = AppInstance.GetCurrent();
		var before = instance.GetActivatedEventArgs();
		var raised = CreateProtocolActivation("web+unotest://not-sticky");

		var count = 0;
		void OnActivated(object? sender, AppActivationArguments args) => count++;

		instance.Activated += OnActivated;
		try
		{
			instance.SetOrRaiseActivation(raised);
		}
		finally
		{
			instance.Activated -= OnActivated;
		}

		Assert.AreEqual(1, count, "The app is already launched, so the activation must be raised rather than stored.");

		var after = instance.GetActivatedEventArgs();
		Assert.AreEqual(before.Kind, after.Kind);
		Assert.AreNotSame(raised, after);
	}

	[TestMethod]
	public void When_GetCurrent()
	{
		var instance = AppInstance.GetCurrent();

		Assert.AreSame(instance, AppInstance.GetCurrent());
		Assert.IsTrue(instance.IsCurrent);
		Assert.AreEqual((uint)Environment.ProcessId, instance.ProcessId);

		var instances = AppInstance.GetInstances();
		Assert.AreEqual(1, instances.Count);
		Assert.AreSame(instance, instances[0]);
	}

	[TestMethod]
	public void When_FindOrRegisterForKey()
	{
		var instance = AppInstance.GetCurrent();
		var originalKey = instance.Key;

		try
		{
			var registered = AppInstance.FindOrRegisterForKey("uno-runtime-tests");

			Assert.AreSame(instance, registered);
			Assert.AreEqual("uno-runtime-tests", instance.Key);

			instance.UnregisterKey();

			Assert.AreEqual(string.Empty, instance.Key);
		}
		finally
		{
			if (originalKey.Length > 0)
			{
				AppInstance.FindOrRegisterForKey(originalKey);
			}
			else
			{
				instance.UnregisterKey();
			}
		}
	}

	[TestMethod]
	public async Task When_RedirectActivationToAsync()
	{
		var instance = AppInstance.GetCurrent();

		var action = instance.RedirectActivationToAsync(instance.GetActivatedEventArgs());
		Assert.IsNotNull(action);

		await action;

		Assert.AreEqual(AsyncStatus.Completed, action.Status);
	}

	[TestMethod]
	public void When_FromActivatedEventArgs_With_Protocol()
	{
		var protocolArgs = new ProtocolActivatedEventArgs(
			new Uri("web+unotest://mapped"),
			ApplicationExecutionState.NotRunning);

		var args = AppActivationArguments.FromActivatedEventArgs(protocolArgs);

		Assert.AreEqual(ExtendedActivationKind.Protocol, args.Kind);
		Assert.AreSame(protocolArgs, args.Data);
	}

	private static AppActivationArguments CreateProtocolActivation(string uri)
		=> AppActivationArguments.CreateProtocol(
			new ProtocolActivatedEventArgs(new Uri(uri), ApplicationExecutionState.NotRunning));
}
