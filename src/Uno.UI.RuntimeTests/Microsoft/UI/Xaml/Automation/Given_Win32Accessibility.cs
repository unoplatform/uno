#if __SKIA__
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
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
	public async Task When_Scrolled_Partially_Out_Of_View_BoundingRectangle_Is_Clipped()
	{
		var button = new Button
		{
			Width = 150,
			Height = 60,
			HorizontalAlignment = HorizontalAlignment.Left,
			Content = "target",
		};
		var scrollViewer = new ScrollViewer
		{
			Width = 200,
			Height = 200,
			Content = new StackPanel
			{
				Children =
				{
					new Border { Height = 170 },
					button,
					new Border { Height = 400 },
				},
			},
		};

		try
		{
			await UITestHelper.Load(scrollViewer);
			// Button occupies [170, 230] in content coordinates; viewport becomes [200, 400],
			// leaving only the bottom 30 logical pixels of the button visible.
			scrollViewer.ChangeView(null, 200, null, disableAnimation: true);
			await WindowHelper.WaitForIdle();

			var accessibility = ResolveAccessibility(button)
				?? throw new InvalidOperationException("Win32Accessibility instance not found.");
			var buttonProvider = GetOrCreateProvider(accessibility, button)
				?? throw new InvalidOperationException("Button provider not found.");
			var scrollViewerProvider = GetOrCreateProvider(accessibility, scrollViewer)
				?? throw new InvalidOperationException("ScrollViewer provider not found.");

			var scale = scrollViewer.XamlRoot?.RasterizationScale
				?? throw new InvalidOperationException("XamlRoot not found.");
			var buttonBounds = GetBoundingRectangle(buttonProvider);
			var scrollViewerBounds = GetBoundingRectangle(scrollViewerProvider);

			// Derive the client origin from the fully-visible ScrollViewer, then compare the
			// button's provider bounds against the visible intersection in logical coordinates.
			var logicalScrollViewerBounds = scrollViewer
				.TransformToVisual(null)
				.TransformBounds(new Rect(0, 0, scrollViewer.ActualWidth, scrollViewer.ActualHeight));
			var logicalButtonBounds = button
				.TransformToVisual(null)
				.TransformBounds(new Rect(0, 0, button.ActualWidth, button.ActualHeight));
			var expectedVisible = logicalButtonBounds;
			expectedVisible.Intersect(logicalScrollViewerBounds);
			var clientOriginX = scrollViewerBounds.Left - logicalScrollViewerBounds.X * scale;
			var clientOriginY = scrollViewerBounds.Top - logicalScrollViewerBounds.Y * scale;

			Assert.IsFalse(expectedVisible.IsEmpty, "The button should be partially visible.");
			Assert.IsTrue(expectedVisible.Height < logicalButtonBounds.Height, "The button should be partially scrolled out of view.");
			Assert.AreEqual(clientOriginX + expectedVisible.X * scale, buttonBounds.Left, 2 * scale);
			Assert.AreEqual(clientOriginY + expectedVisible.Y * scale, buttonBounds.Top, 2 * scale);
			Assert.AreEqual(expectedVisible.Width * scale, buttonBounds.Width, 2 * scale);
			Assert.AreEqual(expectedVisible.Height * scale, buttonBounds.Height, 2 * scale);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	// No BoundingRectangle change is raised on layout (WinUI raises none either), so clients rely on
	// pulling fresh bounds after an element moves.
	[TestMethod]
	public async Task When_Element_Moves_BoundingRectangle_Is_Current()
	{
		var button = new Button
		{
			Content = "Subject",
			Width = 120,
			Height = 40,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(10, 10, 0, 0),
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
			var scale = button.XamlRoot?.RasterizationScale
				?? throw new InvalidOperationException("XamlRoot not found.");
			var before = GetBoundingRectangle(provider);

			button.Margin = new Thickness(70, 50, 0, 0);
			await WindowHelper.WaitForIdle();

			var after = GetBoundingRectangle(provider);

			Assert.AreEqual(before.Left + 60 * scale, after.Left, 2 * scale);
			Assert.AreEqual(before.Top + 40 * scale, after.Top, 2 * scale);
			Assert.AreEqual(before.Width, after.Width, 2 * scale);
			Assert.AreEqual(before.Height, after.Height, 2 * scale);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	// Arranging an element with a peer but no UIA provider must not evaluate IsOffscreen: no client can
	// receive the resulting events, and the evaluation intersects every ancestor clip path.
	[TestMethod]
	public async Task When_Element_Without_Provider_Moves_IsOffscreen_Is_Not_Evaluated()
	{
		var button = new OffscreenCountingButton
		{
			Content = "Subject",
			Width = 120,
			Height = 40,
			HorizontalAlignment = HorizontalAlignment.Left,
			VerticalAlignment = VerticalAlignment.Top,
			Margin = new Thickness(10, 10, 0, 0),
		};

		try
		{
			await UITestHelper.Load(new Grid { Width = 300, Height = 200, Children = { button } });

			Assert.IsNotNull(FrameworkElementAutomationPeer.CreatePeerForElement(button));
			var accessibility = ResolveAccessibility(button)
				?? throw new InvalidOperationException("Win32Accessibility instance not found.");
			RequireNoProvider(accessibility, button);

			button.IsOffscreenCalls = 0;
			button.Margin = new Thickness(70, 50, 0, 0);
			await WindowHelper.WaitForIdle();

			// A UIA client on the machine (Narrator, Inspect) may have asked for the button meanwhile.
			Assert.IsNull(TryGetExistingProvider(accessibility, button), "A UIA client created a provider during the test; the count below is not meaningful.");
			Assert.AreEqual(0, button.IsOffscreenCalls);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	// Once a provider exists, layout must keep raising IsOffscreen changes (WinUI's RaiseAutomaticPropertyChanges).
	[TestMethod]
	public async Task When_Element_With_Provider_Moves_Out_Of_View_IsOffscreen_Change_Is_Raised()
	{
		var (button, clipHost) = CreateButtonInClippedCanvas();
		var recorder = new RecordingListener();

		try
		{
			await UITestHelper.Load(clipHost);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button)
				?? throw new InvalidOperationException("Peer not created.");
			var accessibility = ResolveAccessibility(button)
				?? throw new InvalidOperationException("Win32Accessibility instance not found.");
			_ = GetOrCreateProvider(accessibility, button)
				?? throw new InvalidOperationException("Button provider not found.");
			Assert.IsFalse(peer.IsOffscreen(), "The button starts in view.");

			recorder.Attach();
			button.IsOffscreenCalls = 0;
			Canvas.SetTop(button, 400);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(button.IsOffscreenCalls > 0, "Layout must re-evaluate IsOffscreen for a peer that has a provider.");
			Assert.IsTrue(peer.IsOffscreen(), "The button moved outside its ancestor's clip.");
			CollectionAssert.AreEqual(new[] { (false, true) }, recorder.IsOffscreenChanges(peer), "Expected one IsOffscreen false -> true change.");
		}
		finally
		{
			recorder.Detach();
			WindowHelper.WindowContent = null;
		}
	}

	// A provider created while the element is out of view starts from what the client reads then, so moving
	// back into view raises IsOffscreen true -> false. Without seeding, the baseline layout skipped while there
	// was no provider still says "in view", and the change is never raised.
	[TestMethod]
	public async Task When_Provider_Created_While_Out_Of_View_Moving_Into_View_Raises_IsOffscreen_Change()
	{
		var (button, clipHost) = CreateButtonInClippedCanvas();
		var recorder = new RecordingListener();

		try
		{
			await UITestHelper.Load(clipHost);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button)
				?? throw new InvalidOperationException("Peer not created.");
			var accessibility = ResolveAccessibility(button)
				?? throw new InvalidOperationException("Win32Accessibility instance not found.");

			Canvas.SetTop(button, 400);
			await WindowHelper.WaitForIdle();
			RequireNoProvider(accessibility, button);

			_ = GetOrCreateProvider(accessibility, button)
				?? throw new InvalidOperationException("Button provider not found.");
			Assert.IsTrue(peer.IsOffscreen(), "A client creating the provider now reads IsOffscreen = true.");

			recorder.Attach();
			Canvas.SetTop(button, 10);
			await WindowHelper.WaitForIdle();

			CollectionAssert.AreEqual(new[] { (true, false) }, recorder.IsOffscreenChanges(peer), "Expected one IsOffscreen true -> false change.");
		}
		finally
		{
			recorder.Detach();
			WindowHelper.WindowContent = null;
		}
	}

	// The mirror case: after the provider is created out of view, a layout pass that leaves the element out of
	// view must not raise IsOffscreen with a stale old value.
	[TestMethod]
	public async Task When_Provider_Created_While_Out_Of_View_Layout_Without_Change_Raises_Nothing()
	{
		var (button, clipHost) = CreateButtonInClippedCanvas();
		var recorder = new RecordingListener();

		try
		{
			await UITestHelper.Load(clipHost);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button)
				?? throw new InvalidOperationException("Peer not created.");
			var accessibility = ResolveAccessibility(button)
				?? throw new InvalidOperationException("Win32Accessibility instance not found.");

			Canvas.SetTop(button, 400);
			await WindowHelper.WaitForIdle();
			RequireNoProvider(accessibility, button);

			_ = GetOrCreateProvider(accessibility, button)
				?? throw new InvalidOperationException("Button provider not found.");

			recorder.Attach();
			button.IsOffscreenCalls = 0;
			Canvas.SetLeft(button, 20);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(button.IsOffscreenCalls > 0, "The move must re-evaluate IsOffscreen.");
			Assert.AreEqual(0, recorder.IsOffscreenChanges(peer).Count, "No IsOffscreen change may be raised: the button stayed out of view.");
		}
		finally
		{
			recorder.Detach();
			WindowHelper.WindowContent = null;
		}
	}

	// A peer whose events go to another peer's provider (EventsSource) can't be seeded when that provider is
	// created, so the gate keeps evaluating it, as before the gate existed.
	[TestMethod]
	public async Task When_Peer_Has_EventsSource_With_Provider_Gate_Lets_It_Raise()
	{
		var source = new Button { Content = "Source", Width = 100, Height = 40 };
		var button = new OffscreenCountingButton { Content = "Subject", Width = 100, Height = 40 };

		try
		{
			await UITestHelper.Load(new StackPanel { Children = { source, button } });

			var sourcePeer = FrameworkElementAutomationPeer.CreatePeerForElement(source)
				?? throw new InvalidOperationException("Source peer not created.");
			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button)
				?? throw new InvalidOperationException("Peer not created.");
			peer.EventsSource = sourcePeer;

			var accessibility = ResolveAccessibility(button)
				?? throw new InvalidOperationException("Win32Accessibility instance not found.");
			_ = GetOrCreateProvider(accessibility, source)
				?? throw new InvalidOperationException("Source provider not found.");

			button.IsOffscreenCalls = 0;
			button.Margin = new Thickness(30, 0, 0, 0);
			await WindowHelper.WaitForIdle();

			Assert.IsTrue(button.IsOffscreenCalls > 0, "Layout must evaluate IsOffscreen for a peer with an EventsSource.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	// A virtual (item) peer's provider is keyed by that peer. The ListViewItem's own peer has the item peer as
	// its EventsSource, so its events go to that provider too. The gate must let both through.
	[TestMethod]
	public async Task When_Virtual_Item_Peer_Has_Provider_Gate_Lets_Its_Events_Through()
	{
		var list = new ListView { ItemsSource = new[] { "One", "Two", "Three" }, Width = 200, Height = 200 };

		try
		{
			await UITestHelper.Load(list);

			var container = list.ContainerFromIndex(0) as ListViewItem
				?? throw new InvalidOperationException("Container not found.");
			var containerPeer = FrameworkElementAutomationPeer.CreatePeerForElement(container)
				?? throw new InvalidOperationException("Container peer not created.");
			var listPeer = FrameworkElementAutomationPeer.CreatePeerForElement(list)
				?? throw new InvalidOperationException("ListView peer not created.");
			var itemPeer = listPeer.GetChildren()?.FirstOrDefault(p => p is ItemAutomationPeer)
				?? throw new InvalidOperationException("Item peer not found.");

			var accessibility = ResolveAccessibility(list)
				?? throw new InvalidOperationException("Win32Accessibility instance not found.");

			var itemProvider = GetProviderForPeer(accessibility, itemPeer)
				?? throw new InvalidOperationException("Item provider not created.");

			Assert.AreSame(itemProvider, FindExistingProviderForPeer(accessibility, itemPeer), "The item peer's provider is keyed by the peer.");
			Assert.AreSame(itemProvider, FindExistingProviderForPeer(accessibility, containerPeer), "The container peer's events go to the item provider (EventsSource).");
			Assert.IsTrue(CanReceiveAutomaticPropertyChanges(accessibility, containerPeer), "The gate must let the container peer's events through.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	// A provider created off the UI thread is seeded by a queued callback. Until it has run the baseline is stale,
	// so the gate must not let layout compare against it: no event may be raised for the change in between.
	[TestMethod]
	public async Task When_Provider_Created_Off_UI_Thread_Change_Before_Seed_Raises_Nothing()
	{
		var (button, clipHost) = CreateButtonInClippedCanvas();
		var recorder = new RecordingListener();

		try
		{
			await UITestHelper.Load(clipHost);

			var peer = FrameworkElementAutomationPeer.CreatePeerForElement(button)
				?? throw new InvalidOperationException("Peer not created.");
			var accessibility = ResolveAccessibility(button)
				?? throw new InvalidOperationException("Win32Accessibility instance not found.");
			RequireNoProvider(accessibility, button);

			recorder.Attach();
			Exception? failure = null;
			var thread = new System.Threading.Thread(() =>
			{
				try
				{
					_ = GetOrCreateProvider(accessibility, button);
				}
				catch (Exception ex)
				{
					failure = ex;
				}
			});
			thread.Start();
			// Block (don't await): the UI thread must not run the queued seed yet.
			thread.Join();
			Assert.IsNull(failure);

			Assert.IsFalse(CanReceiveAutomaticPropertyChanges(accessibility, peer), "The gate must hold the peer back until the queued seed has run.");

			await WindowHelper.WaitForIdle();
			Assert.IsTrue(CanReceiveAutomaticPropertyChanges(accessibility, peer), "The peer has a provider and a seeded baseline.");

			Canvas.SetTop(button, 400);
			await WindowHelper.WaitForIdle();
			CollectionAssert.AreEqual(new[] { (false, true) }, recorder.IsOffscreenChanges(peer), "Expected one IsOffscreen false -> true change.");
		}
		finally
		{
			recorder.Detach();
			WindowHelper.WindowContent = null;
		}
	}

	// A listening UIA client (Narrator, Inspect) makes ChildAdded handling create providers for elements entering
	// the tree, so the provider of the subject is dropped to start from the no-provider state.
	private static void RequireNoProvider(object accessibility, UIElement element)
	{
		var peer = element.GetOrCreateAutomationPeer();
		foreach (var (field, key) in new (string, object?)[] { ("_providers", element), ("_peerProviders", peer) })
		{
			var table = accessibility.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(accessibility)
				?? throw new InvalidOperationException($"Win32Accessibility.{field} not found.");
			table.GetType().GetMethods().Single(m => m.Name == "Remove" && m.GetParameters().Length == 1).Invoke(table, new[] { key! });
		}

		Assert.IsNull(TryGetExistingProvider(accessibility, element), "The provider of the subject could not be dropped.");
	}

	private static (OffscreenCountingButton Button, Border ClipHost) CreateButtonInClippedCanvas()
	{
		var button = new OffscreenCountingButton
		{
			Content = "Subject",
			Width = 120,
			Height = 40,
		};
		Canvas.SetLeft(button, 10);
		Canvas.SetTop(button, 10);
		var clipHost = new Border
		{
			Width = 300,
			Height = 200,
			Clip = new RectangleGeometry { Rect = new Rect(0, 0, 300, 200) },
			Child = new Canvas { Width = 300, Height = 200, Children = { button } },
		};
		return (button, clipHost);
	}

	// Records property-changed events, then forwards everything to the listener that was in place, so the
	// Win32 bridge still delivers them to the provider.
	private sealed class RecordingListener : IAutomationPeerListener
	{
		private readonly List<(AutomationPeer Peer, AutomationProperty Property, object OldValue, object NewValue)> _changes = new();
		private IAutomationPeerListener? _inner;

		public void Attach()
		{
			_inner = AutomationPeer.AutomationPeerListener;
			AutomationPeer.TestAutomationPeerListener = this;
		}

		public void Detach() => AutomationPeer.TestAutomationPeerListener = null;

		public List<(bool OldValue, bool NewValue)> IsOffscreenChanges(AutomationPeer peer)
		{
			var result = new List<(bool, bool)>();
			foreach (var change in _changes)
			{
				if (ReferenceEquals(change.Peer, peer) && change.Property == AutomationElementIdentifiers.IsOffscreenProperty)
				{
					result.Add(((bool)change.OldValue, (bool)change.NewValue));
				}
			}

			return result;
		}

		public void NotifyPropertyChangedEvent(AutomationPeer peer, AutomationProperty automationProperty, object oldValue, object newValue)
		{
			_changes.Add((peer, automationProperty, oldValue, newValue));
			_inner?.NotifyPropertyChangedEvent(peer, automationProperty, oldValue, newValue);
		}

		public void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId) => _inner?.NotifyAutomationEvent(peer, eventId);

		public void NotifyInvalidatePeer(AutomationPeer peer) => _inner?.NotifyInvalidatePeer(peer);

		public void NotifyStructureChangedEvent(AutomationPeer peer, AutomationStructureChangeType structureChangeType, AutomationPeer? child)
			=> _inner?.NotifyStructureChangedEvent(peer, structureChangeType, child);

		public void NotifyTextEditTextChangedEvent(AutomationPeer peer, AutomationTextEditChangeType changeType, IReadOnlyList<string> changedData)
			=> _inner?.NotifyTextEditTextChangedEvent(peer, changeType, changedData);

		public void NotifyNotificationEvent(AutomationPeer peer, AutomationNotificationKind notificationKind, AutomationNotificationProcessing notificationProcessing, string displayString, string activityId)
			=> _inner?.NotifyNotificationEvent(peer, notificationKind, notificationProcessing, displayString, activityId);

		public void OnAutomationEvent(AutomationPeer peer, AutomationEvents eventId) => _inner?.OnAutomationEvent(peer, eventId);

		public bool ListenerExistsHelper(AutomationEvents eventId) => true;
	}

	private sealed partial class OffscreenCountingButton : Button
	{
		public int IsOffscreenCalls;

		// Page.OnLoaded focuses the first tab stop, and the focus event creates its provider.
		public OffscreenCountingButton() => IsTabStop = false;

		protected override AutomationPeer OnCreateAutomationPeer() => new CountingPeer(this);

		private sealed partial class CountingPeer(OffscreenCountingButton owner) : ButtonAutomationPeer(owner)
		{
			protected override bool IsOffscreenCore()
			{
				owner.IsOffscreenCalls++;
				return base.IsOffscreenCore();
			}
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25279")]
	public async Task When_ItemContainer_FindItemByProperty_Matches_WinUI()
	{
		const int UiaItemContainerPatternId = 10019;
		const int UiaControlTypePropertyId = 30003;
		const int UiaClassNamePropertyId = 30012;
		const int UiaSelectionItemIsSelectedPropertyId = 30079;
		const int UiaListItemControlTypeId = 50007;
		const int UiaButtonControlTypeId = 50000;

		var items = new List<string>();
		for (var i = 0; i < 200; i++)
		{
			items.Add("Item " + i);
		}

		var list = new ListView { Height = 150, Width = 200, ItemsSource = items };

		try
		{
			await UITestHelper.Load(list);

			var accessibility = ResolveAccessibility(list)
				?? throw new InvalidOperationException("Win32Accessibility instance not found.");
			var provider = GetOrCreateProvider(accessibility, list)
				?? throw new InvalidOperationException("ListView provider not found.");
			var itemContainer = provider.GetType().GetMethod("GetPatternProvider", BindingFlags.Instance | BindingFlags.Public)
				?.Invoke(provider, new object[] { UiaItemContainerPatternId })
				?? throw new InvalidOperationException("ItemContainer pattern not found.");
			var find = itemContainer.GetType().GetMethod("FindItemByProperty", BindingFlags.Instance | BindingFlags.Public)
				?? throw new InvalidOperationException("FindItemByProperty not found.");
			object? Find(object? startAfter, int propertyId, object? value)
			{
				try
				{
					return find.Invoke(itemContainer, new[] { startAfter, propertyId, value });
				}
				catch (TargetInvocationException e) when (e.InnerException is not null)
				{
					throw e.InnerException;
				}
			}

			string? NameOf(object? itemProvider)
				=> itemProvider is null ? null : GetPropertyValue(itemProvider, UiaNamePropertyId) as string;

			// propertyId 0: no criterion, each call returns the item after startAfter.
			var first = Find(null, 0, null);
			Assert.AreEqual("Item 0", NameOf(first));
			var second = Find(first, 0, null);
			Assert.AreEqual("Item 1", NameOf(second));
			Assert.AreEqual("Item 2", NameOf(Find(second, 0, null)));

			// ControlType values are UIA control type ids.
			Assert.AreEqual("Item 0", NameOf(Find(null, UiaControlTypePropertyId, UiaListItemControlTypeId)));
			Assert.IsNull(Find(null, UiaControlTypePropertyId, UiaButtonControlTypeId));

			// Virtualized items are found by their item's string.
			Assert.AreEqual("Item 150", NameOf(Find(null, UiaNamePropertyId, "Item 150")));

			// A null startAfter searches from the first item, not after the previous match.
			Assert.AreEqual("Item 0", NameOf(Find(null, UiaSelectionItemIsSelectedPropertyId, false)));

			// Properties WinUI doesn't support fail with UIA_E_INVALIDOPERATION.
			Assert.ThrowsExactly<InvalidOperationException>(() => Find(null, UiaClassNamePropertyId, "ListViewItem"));
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public void When_Traversing_Deep_Cyclic_Descendants()
	{
		var accessibilityType = FindType("Uno.UI.Runtime.Win32.Win32Accessibility")
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
		var router = FindType("Uno.UI.Runtime.AccessibilityRouter")
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

	private static bool CanReceiveAutomaticPropertyChanges(object accessibility, AutomationPeer peer)
	{
		var method = accessibility.GetType().GetMethod(
			"CanReceiveAutomaticPropertyChanges",
			BindingFlags.Instance | BindingFlags.NonPublic,
			binder: null,
			types: new[] { typeof(AutomationPeer) },
			modifiers: null)
			?? throw new InvalidOperationException("Win32Accessibility.CanReceiveAutomaticPropertyChanges(AutomationPeer) not found.");
		return (bool)method.Invoke(accessibility, new object[] { peer })!;
	}

	// The lookup NotifyPropertyChangedEvent uses to deliver a peer's events.
	private static object? FindExistingProviderForPeer(object accessibility, AutomationPeer peer)
	{
		var method = accessibility.GetType().GetMethod(
			"FindExistingProviderForPeer",
			BindingFlags.Instance | BindingFlags.NonPublic,
			binder: null,
			types: new[] { typeof(AutomationPeer), typeof(bool) },
			modifiers: null)
			?? throw new InvalidOperationException("Win32Accessibility.FindExistingProviderForPeer(AutomationPeer, bool) not found.");
		return method.Invoke(accessibility, new object[] { peer, true });
	}

	private static object? GetProviderForPeer(object accessibility, AutomationPeer peer)
	{
		var method = accessibility.GetType().GetMethod(
			"GetProviderForPeer",
			BindingFlags.Instance | BindingFlags.NonPublic,
			binder: null,
			types: new[] { typeof(AutomationPeer), typeof(bool) },
			modifiers: null)
			?? throw new InvalidOperationException("Win32Accessibility.GetProviderForPeer(AutomationPeer, bool) not found.");
		return method.Invoke(accessibility, new object[] { peer, false });
	}

	private static object? TryGetExistingProvider(object accessibility, UIElement element)
	{
		var tryGetExistingProvider = accessibility.GetType().GetMethod(
			"TryGetExistingProviderForElement",
			BindingFlags.Instance | BindingFlags.NonPublic,
			binder: null,
			types: new[] { typeof(UIElement) },
			modifiers: null)
			?? throw new InvalidOperationException("Win32Accessibility.TryGetExistingProviderForElement(UIElement) not found.");
		return tryGetExistingProvider.Invoke(accessibility, new object[] { element });
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
