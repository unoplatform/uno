#if __SKIA__
#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
public class Given_Win32ProviderLifetime
{
	[TestMethod]
	public async Task When_Cleanup_Reenters_Live_Provider_Lookup()
	{
		var host = new StackPanel();
		var child = new Button { Content = "Connected sibling" };
		host.Children.Add(child);
		try
		{
			await UITestHelper.Load(host, x => x.IsLoaded);
			var accessibility = ResolveAccessibility(host);
			var provider = GetOrCreateProvider(accessibility, child);
			var type = accessibility.GetType();
			// The real re-entry (UiaDisconnectProvider → Navigate(Parent)) hands its result to UIA only,
			// so enter the cleanup state directly and make the same lookups.
			var cleanupDepth = type.GetField("_providerCleanupDepth", BindingFlags.Instance | BindingFlags.NonPublic)!;
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(child);
			var newChild = new Button();
			var getProviderForPeer = type.GetMethod("GetProviderForPeer", BindingFlags.Instance | BindingFlags.NonPublic)!;
			// Keyed by its peer and the peer's owner only, like a provider UIA reached through peer navigation.
			var redirected = new PeerRedirectingControl();
			var peerKeyedProvider = getProviderForPeer.Invoke(accessibility, new object[] { FrameworkElementAutomationPeer.CreatePeerForElement(redirected), false });
			Assert.IsNotNull(peerKeyedProvider);
			Assert.AreEqual(0, cleanupDepth.GetValue(accessibility));
			cleanupDepth.SetValue(accessibility, 1);
			try
			{
				foreach (var name in new[] { "GetProvider", "GetOrCreateProvider" })
				{
					var method = type.GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!;
					Assert.AreSame(provider, method.Invoke(accessibility, new object[] { child }),
						"Native disconnect callbacks must still find connected sibling providers.");
					Assert.AreSame(peerKeyedProvider, method.Invoke(accessibility, new object[] { redirected }),
						"Native disconnect callbacks must find a provider registered only by its peer.");
					Assert.IsNull(method.Invoke(accessibility, new object[] { newChild }),
						"Cleanup must prevent provider creation during native callbacks.");
				}
				Assert.AreSame(provider, getProviderForPeer.Invoke(accessibility, new object[] { peer, false }));
			}
			finally
			{
				cleanupDepth.SetValue(accessibility, 0);
			}
			Assert.IsFalse(IsInvalidated(provider));
			Assert.AreSame(provider, GetOrCreateProvider(accessibility, child));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Removed_Element_Uses_Another_Elements_Peer_Then_Provider_Is_Invalidated()
	{
		var host = new StackPanel();
		try
		{
			await UITestHelper.Load(host, x => x.IsLoaded);
			var child = new PeerRedirectingControl { Width = 100, Height = 100 };
			host.Children.Add(child);
			await WindowHelper.WaitForIdle();
			var accessibility = ResolveAccessibility(host);
			var provider = GetOrCreateProvider(accessibility, child);
			Assert.AreSame(child.PeerOwner, GetProviderOwner(provider));

			host.Children.Clear();
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(IsInvalidated(provider), "A provider keyed by the peer's owner must be disconnected too.");
			AssertDisconnected(provider);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Removed_Subtrees_Have_Referenced_Providers_Then_All_Owners_Are_Collected()
	{
		var host = new StackPanel();
		AutomationProperties.SetName(host, "View host");
		var providers = new List<object>();
		var unknowns = new List<nint>();
		var owners = new List<WeakReference>();
		try
		{
			await UITestHelper.Load(host, x => x.IsLoaded);
			GetOrCreateProvider(ResolveAccessibility(host), host);
			for (var i = 0; i < 18; i++)
			{
				owners.Add(AddNamedUserControl(host));
				await WindowHelper.WaitForIdle();
				var (unknown, provider) = HoldProvider(host);
				unknowns.Add(unknown);
				providers.Add(provider);
				var descendant = HoldDescendantProvider(host);
				unknowns.Add(descendant.Unknown);
				providers.Add(descendant.Provider);
				owners.Add(descendant.Owner);
				host.Children.Clear();
				await WindowHelper.WaitForIdle();
				Assert.IsTrue(IsInvalidated(provider), $"Provider {i} was left connected.");
				Assert.IsTrue(IsInvalidated(descendant.Provider), $"Descendant provider {i} was left connected.");
			}

			foreach (var owner in owners)
			{
				await AssertCollected(owner);
			}
			GC.KeepAlive(providers);
		}
		finally
		{
			foreach (var unknown in unknowns)
			{
				Marshal.Release(unknown);
			}
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Removed_Element_Is_Readded_Then_It_Gets_A_Connected_Provider()
	{
		var host = new StackPanel();
		try
		{
			await UITestHelper.Load(host, x => x.IsLoaded);
			AddNamedUserControl(host);
			await WindowHelper.WaitForIdle();
			var child = host.Children[0];
			var accessibility = ResolveAccessibility(host);
			var oldProvider = GetOrCreateProvider(accessibility, child);
			host.Children.Clear();
			await WindowHelper.WaitForIdle();
			Assert.IsTrue(IsInvalidated(oldProvider));

			host.Children.Add(child);
			await WindowHelper.WaitForIdle();
			var newProvider = GetOrCreateProvider(accessibility, child);
			Assert.AreNotSame(oldProvider, newProvider);
			Assert.IsFalse(IsInvalidated(newProvider));
			Assert.AreSame(child, GetProviderOwner(newProvider));
			AssertDisconnected(oldProvider);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference AddNamedUserControl(Panel host)
	{
		var item = new UserControl { Content = new Button { Content = "Action" } };
		AutomationProperties.SetName(item, "Retired view");
		host.Children.Add(item);
		return new WeakReference(item);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static (nint Unknown, object Provider) HoldProvider(Panel host)
	{
		var provider = GetOrCreateProvider(ResolveAccessibility(host), host.Children[0]);
		return (Marshal.GetIUnknownForObject(provider), provider);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static (nint Unknown, object Provider, WeakReference Owner) HoldDescendantProvider(Panel host)
	{
		var child = (Button)((UserControl)host.Children[0]).Content;
		var provider = GetOrCreateProvider(ResolveAccessibility(host), child);
		return (Marshal.GetIUnknownForObject(provider), provider, new WeakReference(child));
	}

	private static async Task AssertCollected(WeakReference owner)
	{
		for (var i = 0; i < 10 && owner.IsAlive; i++)
		{
			GC.Collect(2);
			GC.WaitForPendingFinalizers();
			GC.Collect(2);
			await WindowHelper.WaitForIdle();
		}
		Assert.IsFalse(owner.IsAlive, "A disconnected provider must release its removed view even while COM references remain.");
	}

	private static bool IsInvalidated(object provider)
		=> (bool)provider.GetType().GetProperty("IsInvalidated", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(provider)!;

	private static object? GetProviderOwner(object provider)
		=> provider.GetType().GetProperty("Owner", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(provider);

	private static void AssertDisconnected(object provider)
	{
		var error = Assert.ThrowsExactly<TargetInvocationException>(() =>
			provider.GetType().GetMethod("GetRuntimeId")!.Invoke(provider, null));
		Assert.AreEqual(unchecked((int)0x80040201), error.InnerException!.HResult);
	}

	private static object ResolveAccessibility(UIElement element)
		=> FindType("Uno.UI.Runtime.Skia.AccessibilityRouter")!
			.GetMethod("Resolve", BindingFlags.Static | BindingFlags.Public, null, new[] { typeof(UIElement) }, null)!
			.Invoke(null, new object[] { element }) ?? throw new InvalidOperationException("Win32 accessibility instance not found.");

	private static object GetOrCreateProvider(object accessibility, UIElement element)
		=> accessibility.GetType().GetMethod("GetOrCreateProvider", BindingFlags.Instance | BindingFlags.NonPublic)!
			.Invoke(accessibility, new object[] { element }) ?? throw new InvalidOperationException("UIA provider not found.");

	private sealed partial class PeerRedirectingControl : ContentControl
	{
		public Button PeerOwner { get; } = new Button();

		protected override AutomationPeer OnCreateAutomationPeer()
			=> FrameworkElementAutomationPeer.CreatePeerForElement(PeerOwner);
	}

	private static Type? FindType(string fullName)
	{
		foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			var type = assembly.GetType(fullName, throwOnError: false);
			if (type is not null)
			{
				return type;
			}
		}

		return null;
	}
}
#endif
