#if __SKIA__
#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
public class Given_Win32Accessibility
{
	// UIA_NamePropertyId; Win32UIAutomationInterop is internal to the runtime assembly.
	private const int UiaNamePropertyId = 30005;

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23802")]
	public async Task When_PathIcon_Button_Name_Is_Exposed_Through_Uia_Provider()
	{
		var namedIcon = new PathIcon { Data = new RectangleGeometry { Rect = new Rect(0, 0, 12, 12) } };
		var namedButton = new Button
		{
			Width = 48,
			Height = 48,
			Content = namedIcon,
		};
		AutomationProperties.SetName(namedButton, "Refresh");

		var unnamedIcon = new PathIcon { Data = new RectangleGeometry { Rect = new Rect(0, 0, 12, 12) } };
		var unnamedButton = new Button
		{
			Width = 48,
			Height = 48,
			Content = unnamedIcon,
		};
		var panel = new StackPanel
		{
			Children =
			{
				namedButton,
				unnamedButton,
			},
		};

		try
		{
			await UITestHelper.Load(panel);

			var accessibility = ResolveAccessibility(namedButton)
				?? throw new InvalidOperationException("Win32Accessibility instance not found.");
			var namedProvider = GetOrCreateProvider(accessibility, namedButton)
				?? throw new InvalidOperationException("Named Button provider not found.");
			var unnamedProvider = GetOrCreateProvider(accessibility, unnamedButton)
				?? throw new InvalidOperationException("Unnamed Button provider not found.");

			Assert.AreEqual("Refresh", GetPropertyValue(namedProvider, UiaNamePropertyId));
			Assert.IsNull(GetPropertyValue(unnamedProvider, UiaNamePropertyId));
			Assert.IsNull(GetOrCreateProvider(accessibility, namedIcon), "PathIcon must not create its own UIA provider.");

			AutomationProperties.SetName(unnamedButton, "Settings");
			Assert.AreEqual("Settings", GetPropertyValue(unnamedProvider, UiaNamePropertyId));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public void When_Traversing_Deep_Cyclic_Descendants()
	{
		var accessibilityType = FindType("Uno.UI.Runtime.Skia.Win32.Win32Accessibility")
			?? throw new InvalidOperationException("Win32Accessibility type not found.");
		var traverse = accessibilityType.GetMethod(
			"TraverseDescendants",
			BindingFlags.Static | BindingFlags.NonPublic)
			?? throw new InvalidOperationException("Win32Accessibility.TraverseDescendants not found.");

		var root = new Grid();
		var descendants = new Grid[128];
		var target = new Button();
		for (var i = 0; i < descendants.Length; i++)
		{
			descendants[i] = new Grid();
		}

		var graph = new Dictionary<UIElement, IReadOnlyList<UIElement>>(ReferenceEqualityComparer.Instance)
		{
			[root] = new[] { descendants[0] },
		};

		for (var i = 0; i < descendants.Length - 1; i++)
		{
			graph[descendants[i]] = new[] { descendants[i + 1] };
		}

		graph[descendants[^1]] = new UIElement[] { descendants[32], target };

		var visited = new HashSet<UIElement>(ReferenceEqualityComparer.Instance);
		IEnumerable<UIElement> GetChildren(UIElement element) =>
			graph.TryGetValue(element, out var children) ? children : Array.Empty<UIElement>();
		bool Visit(UIElement element)
		{
			Assert.IsTrue(visited.Add(element), "Each descendant must be visited at most once.");
			return !ReferenceEquals(element, target);
		}

		traverse.Invoke(
			null,
			new object[]
			{
				root,
				(Func<UIElement, IEnumerable<UIElement>>)GetChildren,
				(Func<UIElement, bool>)Visit,
			});

		Assert.IsTrue(visited.Contains(target), "Deep descendants after a cycle must still be reached.");
		Assert.AreEqual(descendants.Length + 1, visited.Count);
	}

	private static object? ResolveAccessibility(UIElement element)
	{
		var router = FindType("Uno.UI.Runtime.Skia.AccessibilityRouter")
			?? throw new InvalidOperationException("AccessibilityRouter type not found.");
		var resolve = router.GetMethod(
			"Resolve",
			BindingFlags.Static | BindingFlags.Public,
			binder: null,
			types: new[] { typeof(UIElement) },
			modifiers: null)
			?? throw new InvalidOperationException("AccessibilityRouter.Resolve(UIElement) not found.");
		return resolve.Invoke(null, new object[] { element });
	}

	private static object? GetOrCreateProvider(object accessibility, UIElement element)
	{
		var getOrCreateProvider = accessibility.GetType().GetMethod(
			"GetOrCreateProvider",
			BindingFlags.Instance | BindingFlags.NonPublic,
			binder: null,
			types: new[] { typeof(UIElement) },
			modifiers: null)
			?? throw new InvalidOperationException("Win32Accessibility.GetOrCreateProvider(UIElement) not found.");
		return getOrCreateProvider.Invoke(accessibility, new object[] { element });
	}

	private static object? GetPropertyValue(object provider, int propertyId)
	{
		var getPropertyValue = provider.GetType().GetMethod(
			"GetPropertyValue",
			BindingFlags.Instance | BindingFlags.Public,
			binder: null,
			types: new[] { typeof(int) },
			modifiers: null)
			?? throw new InvalidOperationException("Win32RawElementProvider.GetPropertyValue(int) not found.");
		return getPropertyValue.Invoke(provider, new object[] { propertyId });
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
