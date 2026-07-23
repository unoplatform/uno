#if __SKIA__
#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

[TestClass]
[RunsOnUIThread]
[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32)]
public class Given_Win32Accessibility
{
	private const int UiaClickablePointPropertyId = 30014;
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
	public async Task When_Button_ClickablePoint_Is_Center_Of_BoundingRectangle()
	{
		var button = new Button
		{
			Content = "Subject",
			Width = 120,
			Height = 40,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(40, 30, 0, 0),
		};

		try
		{
			await UITestHelper.Load(new Grid
			{
				Width = 300,
				Height = 200,
				Children = { button },
			});

			var accessibility = ResolveAccessibility(button)
				?? throw new InvalidOperationException("Win32Accessibility instance not found.");
			var provider = GetOrCreateProvider(accessibility, button)
				?? throw new InvalidOperationException("Button provider not found.");
			var clickablePoint = GetPropertyValue(provider, UiaClickablePointPropertyId) as double[]
				?? throw new InvalidOperationException("Button clickable point not found.");
			var bounds = GetBoundingRectangle(provider);

			Assert.AreEqual(2, clickablePoint.Length);
			Assert.AreEqual(bounds.Left + bounds.Width / 2, clickablePoint[0], 1);
			Assert.AreEqual(bounds.Top + bounds.Height / 2, clickablePoint[1], 1);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_SelfClipped_Button_ClickablePoint_Uses_Clipped_Center()
	{
		var button = new Button
		{
			Content = "Subject",
			Width = 120,
			Height = 40,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(40, 30, 0, 0),
			Clip = new RectangleGeometry { Rect = new Rect(0, 0, 20, 40) },
		};

		try
		{
			await UITestHelper.Load(new Grid
			{
				Width = 300,
				Height = 200,
				Children = { button },
			});

			var accessibility = ResolveAccessibility(button)
				?? throw new InvalidOperationException("Win32Accessibility instance not found.");
			var provider = GetOrCreateProvider(accessibility, button)
				?? throw new InvalidOperationException("Button provider not found.");
			var clickablePoint = GetPropertyValue(provider, UiaClickablePointPropertyId) as double[]
				?? throw new InvalidOperationException("Button clickable point not found.");
			var providerBounds = GetBoundingRectangle(provider);
			var logicalOwnerBounds = button
				.TransformToVisual(null)
				.TransformBounds(new Rect(0, 0, button.ActualWidth, button.ActualHeight));
			var logicalClippedBounds = button
				.TransformToVisual(null)
				.TransformBounds(new Rect(0, 0, 20, 40));
			var rasterizationScale = button.XamlRoot?.RasterizationScale
				?? throw new InvalidOperationException("Button XamlRoot not found.");
			var clientOriginX = providerBounds.Left - logicalOwnerBounds.X * rasterizationScale;
			var clientOriginY = providerBounds.Top - logicalOwnerBounds.Y * rasterizationScale;

			Assert.AreEqual(2, clickablePoint.Length);
			Assert.AreEqual(clientOriginX + (logicalClippedBounds.X + logicalClippedBounds.Width / 2) * rasterizationScale, clickablePoint[0], 1);
			Assert.AreEqual(clientOriginY + (logicalClippedBounds.Y + logicalClippedBounds.Height / 2) * rasterizationScale, clickablePoint[1], 1);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_FullySelfClipped_Button_Has_No_ClickablePoint_Or_Bounds()
	{
		var button = new Button
		{
			Content = "Subject",
			Width = 120,
			Height = 40,
			Clip = new RectangleGeometry { Rect = new Rect(200, 0, 20, 40) },
		};

		try
		{
			await UITestHelper.Load(button);

			var accessibility = ResolveAccessibility(button)
				?? throw new InvalidOperationException("Win32Accessibility instance not found.");
			var provider = GetOrCreateProvider(accessibility, button)
				?? throw new InvalidOperationException("Button provider not found.");
			var bounds = GetBoundingRectangle(provider);

			Assert.IsNull(GetPropertyValue(provider, UiaClickablePointPropertyId));
			Assert.AreEqual(0, bounds.Width);
			Assert.AreEqual(0, bounds.Height);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_ColorSpectrum_ClickablePoint_Uses_Single_Dpi_Conversion()
	{
		var colorSpectrum = new ColorSpectrum
		{
			Width = 300,
			Height = 160,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(40, 30, 0, 0),
		};

		try
		{
			await UITestHelper.Load(new Grid
			{
				Width = 400,
				Height = 240,
				Children = { colorSpectrum },
			});
			await WindowHelper.WaitForIdle();

			var accessibility = ResolveAccessibility(colorSpectrum)
				?? throw new InvalidOperationException("Win32Accessibility instance not found.");
			var provider = GetOrCreateProvider(accessibility, colorSpectrum)
				?? throw new InvalidOperationException("ColorSpectrum provider not found.");
			var clickablePoint = GetPropertyValue(provider, UiaClickablePointPropertyId) as double[]
				?? throw new InvalidOperationException("ColorSpectrum clickable point not found.");
			var providerBounds = GetBoundingRectangle(provider);
			var logicalOwnerBounds = colorSpectrum
				.TransformToVisual(null)
				.TransformBounds(new Rect(0, 0, colorSpectrum.ActualWidth, colorSpectrum.ActualHeight));
			var inputTarget = colorSpectrum.FindFirstDescendant<FrameworkElement>("InputTarget")
				?? throw new InvalidOperationException("ColorSpectrum InputTarget not found.");
			var logicalInputBounds = inputTarget
				.TransformToVisual(null)
				.TransformBounds(new Rect(0, 0, inputTarget.ActualWidth, inputTarget.ActualHeight));
			var rasterizationScale = colorSpectrum.XamlRoot?.RasterizationScale
				?? throw new InvalidOperationException("ColorSpectrum XamlRoot not found.");
			var clientOriginX = providerBounds.Left - logicalOwnerBounds.X * rasterizationScale;
			var clientOriginY = providerBounds.Top - logicalOwnerBounds.Y * rasterizationScale;

			Assert.AreEqual(2, clickablePoint.Length);
			Assert.AreEqual(clientOriginX + (logicalInputBounds.X + logicalInputBounds.Width / 2) * rasterizationScale, clickablePoint[0], 1);
			Assert.AreEqual(clientOriginY + (logicalInputBounds.Y + logicalInputBounds.Height / 2) * rasterizationScale, clickablePoint[1], 1);
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

	private static (double Left, double Top, double Width, double Height) GetBoundingRectangle(object provider)
	{
		var property = provider.GetType().GetProperty("BoundingRectangle", BindingFlags.Instance | BindingFlags.Public)
			?? throw new InvalidOperationException("Win32RawElementProvider.BoundingRectangle not found.");
		var rectangle = property.GetValue(provider)
			?? throw new InvalidOperationException("Win32RawElementProvider.BoundingRectangle returned null.");
		var rectangleType = rectangle.GetType();

		return (
			GetFieldValue(rectangleType, rectangle, "Left"),
			GetFieldValue(rectangleType, rectangle, "Top"),
			GetFieldValue(rectangleType, rectangle, "Width"),
			GetFieldValue(rectangleType, rectangle, "Height"));
	}

	private static double GetFieldValue(Type type, object instance, string name)
		=> Convert.ToDouble(
			type.GetField(name, BindingFlags.Instance | BindingFlags.Public)?.GetValue(instance)
				?? throw new InvalidOperationException($"{type.Name}.{name} not found."));

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
