// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// corep.h, xpcore.cpp

#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI;
using Uno.UI.Dispatching;
using Windows.UI.ViewManagement;

namespace Uno.UI.Xaml.Core
{
	internal class CoreServices
	{
		private static Lazy<CoreServices> _instance = new Lazy<CoreServices>(() => new CoreServices());

		private VisualTree? _mainVisualTree;
		private double _fontScale = 1.0;
		private double _lastEffectiveFontScale;

#if UNO_HAS_ENHANCED_LIFECYCLE

		private static int _isAdditionalFrameRequested;

		public EventManager EventManager { get; private set; }
#endif

		public CoreServices()
		{
			ContentRootCoordinator = new ContentRootCoordinator(this);
			_lastEffectiveFontScale = FontScale;
#if UNO_HAS_ENHANCED_LIFECYCLE
			EventManager = EventManager.Create();
#endif
		}

#if UNO_HAS_ENHANCED_LIFECYCLE
		private static XamlRoot? GetXamlRoot()
		{
			if (CoreServices.Instance.ContentRootCoordinator.ContentRoots.Count > 0)
			{
				return CoreServices.Instance.ContentRootCoordinator.ContentRoots[0].XamlRoot;
			}

			if (CoreServices.Instance.MainVisualTree is { } mainVisualTree)
			{
				return mainVisualTree.XamlRoot;
			}

			return null;
		}

		// When true, OnTick is currently executing. RequestAdditionalFrame is suppressed
		// because layout will run within the same OnTick. This prevents animated property
		// changes (which invalidate layout) from re-enqueuing OnTick to the Normal queue,
		// which would starve the Idle queue and block WaitForIdle/RunIdleAsync.
		// In WinUI, SetAnimatedValue is within the frame pipeline and never touches the dispatcher.
		private static bool _isInTick;

		internal static void RequestAdditionalFrame()
		{
			if (_isInTick)
			{
				// We're already inside OnTick — layout will run shortly. No need to enqueue.
				return;
			}

			if (GetXamlRoot() is { Bounds: { Width: not 0, Height: not 0 } } &&
				Interlocked.CompareExchange(ref _isAdditionalFrameRequested, 1, 0) == 0)
			{
				// This lambda is intentionally static. It shouldn't capture anything to avoid allocations.
				NativeDispatcher.Main.Enqueue(static () => OnTick(), NativeDispatcherPriority.Normal);
			}
		}

		private static void OnTick()
		{
			_isAdditionalFrameRequested = 0;
			_isInTick = true;
			try
			{
#if __SKIA__
				// MUX Reference: CCoreServices::Tick() (xcpcore.cpp line 4106)
				// Tick all active animations BEFORE layout so animated property values
				// are applied before Measure/Arrange. This matches WinUI's frame cycle:
				// TimeManager.Tick() → Layout → Render.
				TimeManager.Instance.Tick(newTimelinesOnly: false);
#endif

				// NOTE: The below code should really be replaced with just this:
				// ----------------------------
				//if (GetXamlRoot()?.VisualTree?.RootElement is { } root)
				//{
				//	root.UpdateLayout();
				//
				//	if (CoreServices.Instance.EventManager.ShouldRaiseLoadedEvent)
				//	{
				//		CoreServices.Instance.EventManager.RaiseLoadedEvent();
				//		root.UpdateLayout();
				//	}
				//}
				// -----------------------------
				// However, as we don't yet have XamlIslandRootCollection, we will need to enumerate the windows through ApplicationHelper.Windows.

				// This happens for Islands.
				if (GetXamlRoot() is { HostWindow: null, VisualTree.RootElement: { } xamlIsland })
				{
					xamlIsland.UpdateLayout();

					if (CoreServices.Instance.EventManager.ShouldRaiseLoadedEvent)
					{
						CoreServices.Instance.EventManager.RaiseLoadedEvent();
						xamlIsland.UpdateLayout();
					}
				}

				foreach (var window in ApplicationHelper.WindowsInternal)
				{
					if (window.RootElement is not { } root)
					{
						continue;
					}

					root.UpdateLayout();

					if (CoreServices.Instance.EventManager.ShouldRaiseLoadedEvent)
					{
						CoreServices.Instance.EventManager.RaiseLoadedEvent();
						root.UpdateLayout();
					}

#if __SKIA__
					(root.XamlRoot?.Content?.Visual.CompositionTarget as CompositionTarget)?.OnRenderFrameOpportunity();
#endif
				}

#if __SKIA__
				// MUX Reference: Second tick pass in CCoreServices::Tick()
				// Tick only timelines added during layout (e.g., animations started by
				// Loaded event handlers or layout-triggered VisualState transitions).
				// These are at the head of the list, before the snapped previous-head marker.
				if (TimeManager.Instance.HasActiveTimelines)
				{
					TimeManager.Instance.Tick(newTimelinesOnly: true);
				}
#endif
			}
			finally
			{
				_isInTick = false;
			}
		}
#endif

		// TODO Uno: This will not be a singleton when multi-window setups are supported.
		public static CoreServices Instance => _instance.Value;

		/// <summary>
		/// Provides the content root coordinator.
		/// </summary>
		public ContentRootCoordinator ContentRootCoordinator { get; }

		/// <summary>
		/// Initialization type.
		/// </summary>
		public InitializationType InitializationType { get; internal set; } =
			InitializationType.IslandsOnly;

		public RootVisual? MainRootVisual => _mainVisualTree?.RootVisual;

		public PopupRoot? MainPopupRoot => _mainVisualTree?.PopupRoot;

		public Canvas? MainFocusVisualRoot => _mainVisualTree?.FocusVisualRoot;

		public FullWindowMediaRoot? MainFullWindowMediaRoot => _mainVisualTree?.FullWindowMediaRoot;

		public VisualTree? MainVisualTree => _mainVisualTree;

		public UIElement? VisualRoot => _mainVisualTree?.PublicRootVisual;

		internal void InitCoreWindowContentRoot()
		{
			if (_mainVisualTree is not null)
			{
				return;
			}

			var contentRoot = ContentRootCoordinator.CreateContentRoot(ContentRootType.CoreWindow, ThemingHelper.GetRootVisualBackground(), null);
			_mainVisualTree = contentRoot.VisualTree;

			//TODO Uno: Add input services
			//m_inputServices.attach(new CInputServices(this));

			//// While the tree is loading, delay async processing (such as downloads and drawing)
			//// until we're ready to raise the Loaded event and render the first frame.
			//if (m_pBrowserHost)
			//{
			//	m_isMainTreeLoading = TRUE;
			//}
		}

		internal bool IsXamlVisible()
		{
			// TODO Uno: This is currently highly simplified, adjust when all islands are rooted under main tree.
			return ContentRootCoordinator.Unsafe_IslandsIncompatible_CoreWindowContentRoot?.CompositionContent.IsSiteVisible ?? false;
		}

		[NotImplemented]
		internal void UIARaiseFocusChangedEventOnUIAWindow(DependencyObject sender)
		{
		}

#if UNO_HAS_ENHANCED_LIFECYCLE
		internal void RaisePendingLoadedRequests()
		{
			EventManager.RequestRaiseLoadedEventOnNextTick();
		}
#endif

		/// <summary>
		/// Gets the current text scale factor, with FeatureConfiguration overrides applied.
		/// </summary>
		// MUX Reference xcpcore.cpp, tag winui3/release/1.8.1
		internal double FontScale => TextScaleHelper.GetEffectiveFontScale(_fontScale);

		/// <summary>
		/// Updates the global font scale factor from the OS setting.
		/// If changed, fires UISettings.TextScaleFactorChanged and invalidates text elements.
		/// </summary>
		// MUX Reference xcpcore.cpp, tag winui3/release/1.8.1, line 11076
		internal void UpdateFontScale(double newScale)
		{
			_fontScale = newScale;
			var effectiveNew = FontScale;

			if (_lastEffectiveFontScale != effectiveNew)
			{
				_lastEffectiveFontScale = effectiveNew;
				UISettings.OnTextScaleFactorChanged();
				RecursiveInvalidateTextScale();
			}
		}

		/// <summary>
		/// Walks all visual trees and invalidates measure on text-rendering elements.
		/// </summary>
		// MUX Reference uielement.cpp, tag winui3/release/1.8.1, line 3498
		private void RecursiveInvalidateTextScale()
		{
			foreach (var window in ApplicationHelper.WindowsInternal)
			{
				if (window.RootElement is { } root)
				{
					InvalidateTextScaleRecursive(root);
				}
			}
		}

		private static void InvalidateTextScaleRecursive(UIElement element)
		{
			if (element is TextBlock or RichTextBlock)
			{
				element.InvalidateMeasure();
			}
#if __SKIA__ || __WASM__
			// ContentPresenter only needs Uno-level measure invalidation on Skia/WASM where
			// GetScaledFontSize() is called during the Uno measure pass. On iOS/Android, native
			// font APIs already handle text scaling, and invalidating ContentPresenter there
			// causes spurious layout passes that can break the TextBox auto-grow mechanism.
			else if (element is ContentPresenter)
			{
				element.InvalidateMeasure();
			}
#endif

#if __SKIA__
			// Invalidate cached font info on Inlines
			if (element is TextBlock textBlock)
			{
				foreach (var inline in textBlock.Inlines)
				{
					foreach (var descendant in InlineExtensions.Enumerate(inline))
					{
						descendant.InvalidateTextScaleFontInfo();
					}
				}
			}
#elif __WASM__
			if (element is TextBlock wasmTextBlock)
			{
				wasmTextBlock.InvalidateForTextScaleChange();
			}
#endif

			var count = VisualTreeHelper.GetChildrenCount(element);
			for (var i = 0; i < count; i++)
			{
				if (VisualTreeHelper.GetChild(element, i) is UIElement child)
				{
					InvalidateTextScaleRecursive(child);
				}
			}
		}
	}
}
