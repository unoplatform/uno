#nullable enable

using System;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.Samples.Controls;
using Windows.Foundation;

namespace UITests.Shared.Windows_UI_Xaml_Automation;

[Sample(
	"Automation",
	Name = "ClickablePoint_Visualizer",
	Description = "Visualizes AutomationPeer and Win32 UIA clickable-point coordinates.",
	IsManualTest = true,
	IgnoreInSnapshotTests = true)]
public sealed partial class ClickablePoint_Visualizer : UserControl
{
	private const int UiaClickablePointPropertyId = 30014;

	public ClickablePoint_Visualizer()
	{
		this.InitializeComponent();
		Loaded += OnLoaded;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
		=> DispatcherQueue.TryEnqueue(UpdateVisualization);

	private void OnRefreshClick(object sender, RoutedEventArgs e)
		=> UpdateVisualization();

	private void OnVisualizationSizeChanged(object sender, SizeChangedEventArgs e)
		=> DispatcherQueue.TryEnqueue(UpdateVisualization);

	private void UpdateVisualization()
	{
		if (!IsLoaded || VisualizationRoot.ActualWidth <= 0 || VisualizationRoot.ActualHeight <= 0)
		{
			return;
		}

		var peer = FrameworkElementAutomationPeer.CreatePeerForElement(TargetButton);
		if (peer is null)
		{
			ResultText.Text = "Automation peer is not available.";
			return;
		}

		var bounds = peer.GetBoundingRectangle();
		var point = peer.GetClickablePoint();
		var rootOrigin = VisualizationRoot.TransformToVisual(null).TransformPoint(default);
		var expectedX = bounds.X + bounds.Width / 2 - rootOrigin.X;
		var expectedY = bounds.Y + bounds.Height / 2 - rootOrigin.Y;

		PositionMarker(ExpectedMarker, expectedX, expectedY);

		var hasPeerPoint = point.X != 0 || point.Y != 0;
		ActualMarker.Visibility = hasPeerPoint ? Visibility.Visible : Visibility.Collapsed;
		MissingPointBadge.Visibility = hasPeerPoint ? Visibility.Collapsed : Visibility.Visible;

		if (hasPeerPoint)
		{
			PositionMarker(ActualMarker, point.X - rootOrigin.X, point.Y - rootOrigin.Y);
		}

		var providerMeasurement = GetWin32ProviderPoint(TargetButton);
		ResultText.Text = !providerMeasurement.IsAvailable
			? "Win32 provider comparison is not available on this target."
			: providerMeasurement.Point is null
				? "BEFORE behavior: Uno exposes no provider point, so UIA must choose a fallback."
				: "AFTER behavior: Uno exposes the clipped center directly to UIA.";
		BoundsText.Text = $"Button bounds: X={bounds.X:F1}, Y={bounds.Y:F1}, Width={bounds.Width:F1}, Height={bounds.Height:F1}";
		PeerPointText.Text = $"AutomationPeer.GetClickablePoint: X={point.X:F1}, Y={point.Y:F1}";
		ProviderPointText.Text = !providerMeasurement.IsAvailable
			? "Win32 UIA_ClickablePoint (physical screen): <not applicable>"
			: providerMeasurement.Point is { } rawPoint
				? $"Win32 UIA_ClickablePoint (physical screen): X={rawPoint.X:F1}, Y={rawPoint.Y:F1}"
				: "Win32 UIA_ClickablePoint (physical screen): <unset>";
	}

	private static void PositionMarker(FrameworkElement marker, double x, double y)
	{
		Canvas.SetLeft(marker, x - marker.Width / 2);
		Canvas.SetTop(marker, y - marker.Height / 2);
	}

	private static (bool IsAvailable, Point? Point) GetWin32ProviderPoint(UIElement element)
	{
		try
		{
			var accessibilityRouter = FindType("Uno.UI.Runtime.Skia.AccessibilityRouter");
			var resolve = accessibilityRouter?.GetMethod(
				"Resolve",
				BindingFlags.Static | BindingFlags.Public,
				binder: null,
				types: new[] { typeof(UIElement) },
				modifiers: null);
			var accessibility = resolve?.Invoke(null, new object[] { element });
			if (accessibility is null)
			{
				return (false, null);
			}

			var getProvider = accessibility.GetType().GetMethod(
				"GetOrCreateProvider",
				BindingFlags.Instance | BindingFlags.NonPublic,
				binder: null,
				types: new[] { typeof(UIElement) },
				modifiers: null);
			var provider = getProvider?.Invoke(accessibility, new object[] { element });
			if (provider is null)
			{
				return (false, null);
			}

			var getPropertyValue = provider.GetType().GetMethod(
				"GetPropertyValue",
				BindingFlags.Instance | BindingFlags.Public,
				binder: null,
				types: new[] { typeof(int) },
				modifiers: null);

			if (getPropertyValue is null)
			{
				return (false, null);
			}

			return getPropertyValue.Invoke(provider, new object[] { UiaClickablePointPropertyId }) is double[] { Length: 2 } point
				? (true, new Point(point[0], point[1]))
				: (true, null);
		}
		catch (TargetInvocationException)
		{
			return (false, null);
		}
		catch (ArgumentException)
		{
			return (false, null);
		}
	}

	private static Type? FindType(string fullName)
	{
		foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			if (assembly.GetType(fullName, throwOnError: false) is { } type)
			{
				return type;
			}
		}

		return null;
	}
}
