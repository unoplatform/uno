#if __SKIA__
#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
public class Given_Win32Accessibility
{
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
