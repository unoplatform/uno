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
using Uno.UI.Dispatching;

namespace Uno.UI.Xaml.Core
{
	internal class CoreServices
	{
		private static Lazy<CoreServices> _instance = new Lazy<CoreServices>(() => new CoreServices());

		private VisualTree? _mainVisualTree;

#if UNO_HAS_ENHANCED_LIFECYCLE

		private static int _isAdditionalFrameRequested;

		public EventManager EventManager { get; private set; }
#endif

		public CoreServices()
		{
			ContentRootCoordinator = new ContentRootCoordinator(this);
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

		internal static void RequestAdditionalFrame()
		{
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
				root.XamlRoot?.OnPaintFrameOpportunity();
#endif
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
#if HAS_UNO_WINUI
			InitializationType.IslandsOnly;
#else
			InitializationType.MainView;
#endif

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
	}
}
