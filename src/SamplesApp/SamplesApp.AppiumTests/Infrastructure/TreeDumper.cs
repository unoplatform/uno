#nullable enable

using System;
using System.Collections.Generic;
using OpenQA.Selenium;

namespace SamplesApp.AppiumTests.Infrastructure;

/// <summary>
/// Walks the live automation tree exposed by <see cref="IPlatformAdapter"/>
/// and builds a normalized <see cref="AccessibilityNode"/> graph suitable
/// for snapshot serialization and diffing.
/// </summary>
public static class TreeDumper
{
	/// <summary>
	/// AutomationIds that come from WinUI control templates / window chrome
	/// rather than the sample under test. Excluded from snapshots so the
	/// recorded baselines are stable across host frameworks and sample
	/// changes that don't touch these parts.
	/// </summary>
	private static readonly HashSet<string> s_noiseAutomationIds = new(StringComparer.Ordinal)
	{
		// Window chrome
		"_MinimizeButton",
		"_MaximizeButton",
		"_CloseButton",
		"_RestoreButton",
		"InfoButton",
		// ScrollViewer template parts
		"VerticalScrollBar",
		"HorizontalScrollBar",
		"VerticalSmallDecrease",
		"VerticalSmallIncrease",
		"VerticalLargeDecrease",
		"VerticalLargeIncrease",
		"HorizontalSmallDecrease",
		"HorizontalSmallIncrease",
		"HorizontalLargeDecrease",
		"HorizontalLargeIncrease",
		// TextBox template parts
		"TextBox",
		// Other framework-injected template parts that aren't sample content.
		"PART_ContentPresenter",
	};

	/// <summary>
	/// Max recursion depth. Guards against pathological trees and bad
	/// adapter implementations that would otherwise cause runaway walks.
	/// </summary>
	private const int MaxDepth = 64;

	public static AccessibilityNode Capture(IWebDriver driver, IPlatformAdapter adapter)
	{
		var topLevel = adapter.GetChildren(driver, parent: null);
		var root = new AccessibilityNode
		{
			Role = "root",
			Name = string.Empty,
			AutomationId = string.Empty,
		};

		foreach (var child in topLevel)
		{
			var node = CaptureRecursive(driver, adapter, child, depth: 0);
			if (node is not null)
			{
				root.Children.Add(node);
			}
		}

		return root;
	}

	private static AccessibilityNode? CaptureRecursive(
		IWebDriver driver,
		IPlatformAdapter adapter,
		IWebElement element,
		int depth)
	{
		if (depth >= MaxDepth)
		{
			return null;
		}

		string automationId;
		string name;
		string rawRole;
		string? value;
		IReadOnlyList<string> patterns;
		IReadOnlyDictionary<string, string> extras;
		try
		{
			automationId = adapter.GetAutomationId(element);
			if (IsNoise(automationId))
			{
				return null;
			}

			rawRole = adapter.GetRole(element);
			name = adapter.GetName(element);
			value = adapter.GetValue(element);
			patterns = adapter.GetSupportedPatterns(element);
			extras = adapter.GetExtras(element);
		}
		catch (StaleElementReferenceException)
		{
			return null;
		}
		catch (NoSuchElementException)
		{
			return null;
		}

		var node = new AccessibilityNode
		{
			Role = CanonicalRole.Normalize(rawRole, adapter.Platform),
			Name = name,
			AutomationId = automationId,
			Value = value,
			Patterns = new List<string>(patterns),
			Extras = new Dictionary<string, string>(extras),
		};

		IReadOnlyList<IWebElement> children;
		try
		{
			children = adapter.GetChildren(driver, element);
		}
		catch (StaleElementReferenceException)
		{
			return node;
		}

		foreach (var child in children)
		{
			var sub = CaptureRecursive(driver, adapter, child, depth + 1);
			if (sub is not null)
			{
				node.Children.Add(sub);
			}
		}

		return node;
	}

	private static bool IsNoise(string automationId)
	{
		if (string.IsNullOrEmpty(automationId))
		{
			return false;
		}

		return s_noiseAutomationIds.Contains(automationId);
	}
}
