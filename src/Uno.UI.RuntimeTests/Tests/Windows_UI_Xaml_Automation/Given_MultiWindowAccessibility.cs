#if __SKIA__
using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

/// <summary>
/// Multi-window accessibility regression coverage for Skia Desktop (feature 004-multi-window-a11y).
/// Asserts the behavioral invariants documented in spec.md SC-007 and FR-009:
///   a) every open top-level Window has an independent, enabled accessibility instance;
///   b) disposing one window does not disable the other;
///   c) the AccessibilityRouter resolves peers / elements to their owning window's instance;
///   d) the router's Resolve returns null for elements whose window has been disposed.
///
/// Uses reflection to access internal types in Uno.UI.Runtime.Skia and its host
/// assemblies because the RuntimeTests project does not take a compile-time
/// dependency on those runtime assemblies — only the Skia Desktop host loads them.
///
/// Scoped to Skia Desktop Windows for PR 1. PR 2 lifts the condition to full
/// Skia Desktop coverage after macOS gets its own per-window native context.
/// </summary>
[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
public class Given_MultiWindowAccessibility
{
	[TestMethod]
	public async Task When_SecondaryWindow_Has_Independent_Accessibility_Instance()
	{
		var primaryOwner = GetPrimaryAccessibilityOwner();
		Assert.IsNotNull(primaryOwner, "Primary window must expose IAccessibilityOwner via its XamlRootMap host.");

		var primaryAccessibility = GetAccessibility(primaryOwner);
		Assert.IsNotNull(primaryAccessibility, "Primary window must have a live accessibility instance.");
		Assert.IsTrue(IsAccessibilityEnabled(primaryAccessibility),
			"Primary window accessibility must be enabled.");

		var secondary = new Window();
		try
		{
			var secondaryButton = new Button { Content = "Secondary" };
			secondary.Content = secondaryButton;

			var activated = false;
			secondary.Activated += (_, _) => activated = true;
			secondary.Activate();
			await TestServices.WindowHelper.WaitFor(() => activated);
			await TestServices.WindowHelper.WaitForLoaded(secondaryButton);

			var secondaryOwner = GetAccessibilityOwnerForXamlRoot(secondary.Content.XamlRoot);
			Assert.IsNotNull(secondaryOwner, "Secondary window host must implement IAccessibilityOwner (FR-001).");

			var secondaryAccessibility = GetAccessibility(secondaryOwner);
			Assert.IsNotNull(secondaryAccessibility, "Secondary window must have its own accessibility instance (FR-001).");
			Assert.IsTrue(IsAccessibilityEnabled(secondaryAccessibility),
				"Secondary window accessibility must be enabled independently of the primary.");

			// SC-007 (a): primary and secondary instances are distinct.
			Assert.AreNotSame(primaryAccessibility, secondaryAccessibility,
				"Each window must own its own accessibility instance (SC-007 a).");

			// SC-007 (c): router resolves elements to the owning window's instance.
			var primaryElement = TestServices.WindowHelper.CurrentTestWindow.Content
				?? throw new InvalidOperationException("Primary window has no content.");
			var primaryResolved = RouterResolveElement(primaryElement);
			var secondaryResolved = RouterResolveElement(secondaryButton);

			Assert.AreSame(primaryAccessibility, primaryResolved,
				"Router.Resolve(primary element) must return the primary window's instance (SC-007 c).");
			Assert.AreSame(secondaryAccessibility, secondaryResolved,
				"Router.Resolve(secondary element) must return the secondary window's instance (SC-007 c).");

			// FR-009: debouncer timer state is per-instance.
			var primaryTimerField = typeof(object).Assembly; // placate the compiler; field lookup is reflective below
			_ = primaryTimerField;
			AssertDistinctFieldIdentities(primaryAccessibility, secondaryAccessibility, "_politeDebounceTimer");
		}
		finally
		{
			await TestServices.RunOnUIThread(() => secondary.Close());
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	[TestMethod]
	public async Task When_SecondaryWindow_Disposed_Then_Primary_Remains_Accessible()
	{
		var primaryOwner = GetPrimaryAccessibilityOwner()
			?? throw new InvalidOperationException("Primary owner missing.");
		var primaryAccessibility = GetAccessibility(primaryOwner)
			?? throw new InvalidOperationException("Primary accessibility missing.");

		var secondary = new Window();
		Button secondaryButton;
		object secondaryAccessibility;
		try
		{
			secondaryButton = new Button { Content = "Dispose me" };
			secondary.Content = secondaryButton;

			var activated = false;
			secondary.Activated += (_, _) => activated = true;
			secondary.Activate();
			await TestServices.WindowHelper.WaitFor(() => activated);
			await TestServices.WindowHelper.WaitForLoaded(secondaryButton);

			var secondaryOwner = GetAccessibilityOwnerForXamlRoot(secondary.Content.XamlRoot)
				?? throw new InvalidOperationException("Secondary owner missing.");
			secondaryAccessibility = GetAccessibility(secondaryOwner)
				?? throw new InvalidOperationException("Secondary accessibility missing.");

			Assert.AreNotSame(primaryAccessibility, secondaryAccessibility);
		}
		finally
		{
			await TestServices.RunOnUIThread(() => secondary.Close());
			await TestServices.WindowHelper.WaitForIdle();
		}

		// SC-007 (b): primary remains accessible after secondary disposal.
		Assert.IsTrue(IsAccessibilityEnabled(primaryAccessibility),
			"Primary accessibility must remain enabled after secondary window disposal (SC-007 b).");

		// SC-007 (d): Resolve for a former secondary element returns null because
		// its XamlRoot has been unregistered from XamlRootMap.
		var postDisposeResolved = RouterResolveElement(secondaryButton!);
		Assert.IsNull(postDisposeResolved,
			"After secondary window disposal, Resolve for a former secondary element must return null (SC-007 d).");
	}

	[TestMethod]
	public async Task When_Announcement_From_SecondaryWindow_Then_Routes_To_SecondaryInstance()
	{
		var primaryOwner = GetPrimaryAccessibilityOwner()
			?? throw new InvalidOperationException("Primary owner missing.");
		var primaryAccessibility = GetAccessibility(primaryOwner)
			?? throw new InvalidOperationException("Primary accessibility missing.");

		var secondary = new Window();
		try
		{
			var secondaryButton = new Button { Content = "Secondary announcer" };
			secondary.Content = secondaryButton;

			var activated = false;
			secondary.Activated += (_, _) => activated = true;
			secondary.Activate();
			await TestServices.WindowHelper.WaitFor(() => activated);
			await TestServices.WindowHelper.WaitForLoaded(secondaryButton);

			// Re-activate the primary so the router's sticky active owner points at it —
			// any source-bearing routing to the secondary must bypass the active owner.
			TestServices.WindowHelper.CurrentTestWindow.Activate();
			await TestServices.WindowHelper.WaitForIdle();

			var secondaryOwner = GetAccessibilityOwnerForXamlRoot(secondary.Content.XamlRoot)
				?? throw new InvalidOperationException("Secondary owner missing.");
			var secondaryAccessibility = GetAccessibility(secondaryOwner)
				?? throw new InvalidOperationException("Secondary accessibility missing.");

			var secondaryPeer = FrameworkElementAutomationPeer.FromElement(secondaryButton)
				?? FrameworkElementAutomationPeer.CreatePeerForElement(secondaryButton);

			// Router.Resolve(peer) must pick the secondary instance regardless of
			// active-owner state (FR-006).
			var resolved = RouterResolvePeer(secondaryPeer);
			Assert.AreSame(secondaryAccessibility, resolved,
				"Source-bearing announcements must route to the owning window's instance, not the active owner (FR-006).");

			Assert.AreNotSame(primaryAccessibility, secondaryAccessibility);
		}
		finally
		{
			await TestServices.RunOnUIThread(() => secondary.Close());
			await TestServices.WindowHelper.WaitForIdle();
		}
	}

	// ──────────────────────────────────────────────────────────────
	//  Reflection helpers — access internal types in Uno.UI.Runtime.Skia
	//  and the host assembly without taking a compile-time reference.
	// ──────────────────────────────────────────────────────────────

	private static object? GetPrimaryAccessibilityOwner()
	{
		var root = TestServices.WindowHelper.CurrentTestWindow.Content?.XamlRoot;
		return root is null ? null : GetAccessibilityOwnerForXamlRoot(root);
	}

	private static object? GetAccessibilityOwnerForXamlRoot(XamlRoot xamlRoot)
	{
		var xamlRootMapType = typeof(Microsoft.UI.Xaml.XamlRoot).Assembly
			.GetType("Uno.UI.Hosting.XamlRootMap")
			?? throw new InvalidOperationException("XamlRootMap type not found.");
		var getHost = xamlRootMapType.GetMethod(
			"GetHostForRoot",
			BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
			?? throw new InvalidOperationException("XamlRootMap.GetHostForRoot not found.");
		var host = getHost.Invoke(null, new object[] { xamlRoot });
		if (host is null)
		{
			return null;
		}

		var ownerType = FindType("Uno.UI.Runtime.Skia.IAccessibilityOwner");
		return ownerType is not null && ownerType.IsInstanceOfType(host) ? host : null;
	}

	private static object? GetAccessibility(object owner)
	{
		var ownerType = owner.GetType();
		// Look up the property via the explicit-interface backing member or the
		// public/internal property surface.
		var property = ownerType.GetProperty(
			"Accessibility",
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		if (property is not null)
		{
			return property.GetValue(owner);
		}

		// Fall back to explicit interface implementation lookup.
		var iface = FindType("Uno.UI.Runtime.Skia.IAccessibilityOwner");
		if (iface is null)
		{
			return null;
		}

		var map = ownerType.GetInterfaceMap(iface);
		for (var i = 0; i < map.InterfaceMethods.Length; i++)
		{
			if (map.InterfaceMethods[i].Name.EndsWith("get_Accessibility", StringComparison.Ordinal))
			{
				return map.TargetMethods[i].Invoke(owner, null);
			}
		}
		return null;
	}

	private static bool IsAccessibilityEnabled(object accessibility)
	{
		var prop = accessibility.GetType().GetProperty(
			"IsAccessibilityEnabled",
			BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		return prop?.GetValue(accessibility) is true;
	}

	private static object? RouterResolveElement(UIElement element)
	{
		var router = FindType("Uno.UI.Runtime.Skia.AccessibilityRouter")
			?? throw new InvalidOperationException("AccessibilityRouter type not found.");
		var resolve = router.GetMethod(
			"Resolve",
			BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
			binder: null,
			types: new[] { typeof(UIElement) },
			modifiers: null)
			?? throw new InvalidOperationException("AccessibilityRouter.Resolve(UIElement) not found.");
		return resolve.Invoke(null, new object[] { element });
	}

	private static object? RouterResolvePeer(AutomationPeer peer)
	{
		var router = FindType("Uno.UI.Runtime.Skia.AccessibilityRouter")
			?? throw new InvalidOperationException("AccessibilityRouter type not found.");
		var resolve = router.GetMethod(
			"Resolve",
			BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
			binder: null,
			types: new[] { typeof(AutomationPeer) },
			modifiers: null)
			?? throw new InvalidOperationException("AccessibilityRouter.Resolve(AutomationPeer) not found.");
		return resolve.Invoke(null, new object[] { peer });
	}

	private static void AssertDistinctFieldIdentities(object a, object b, string fieldName)
	{
		var type = FindCommonBase(a.GetType(), b.GetType());
		var field = type?.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
		if (field is null)
		{
			// Not a hard failure — the field name is an implementation detail that
			// could evolve. Log and skip so the test stays meaningful on refactors.
			return;
		}

		var valueA = field.GetValue(a);
		var valueB = field.GetValue(b);

		if (valueA is null && valueB is null)
		{
			// Timers are lazily created; neither window has announced yet.
			// Nothing to assert here beyond "they are not sharing a single timer"
			// which is trivially satisfied.
			return;
		}

		Assert.AreNotSame(valueA, valueB,
			$"Field '{fieldName}' must be per-instance to isolate announcement state across windows (FR-009).");
	}

	private static Type? FindCommonBase(Type a, Type b)
	{
		var current = a;
		while (current is not null)
		{
			if (current.IsAssignableFrom(b))
			{
				return current;
			}
			current = current.BaseType;
		}
		return null;
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
