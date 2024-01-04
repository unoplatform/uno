// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// corep.h, xpcore.cpp

#nullable enable

using System;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.UI.Xaml.Islands;
using Uno.UI.Extensions;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace Uno.UI.Xaml.Core
{
	internal class CoreServices
	{
		private static Lazy<CoreServices> _instance = new Lazy<CoreServices>(() => new CoreServices());

		private VisualTree? _mainVisualTree;

		public CoreServices()
		{
			ContentRootCoordinator = new ContentRootCoordinator(this);
		}

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

		public DependencyObject? VisualRoot => _mainVisualTree?.PublicRootVisual;

		public bool HasXamlIslands => InitializationType == InitializationType.IslandsOnly; // TODO Uno: This logic is simplified now.

		internal DependencyObject? GetRootForElement(DependencyObject dependencyObject)
		{
			if (_mainVisualTree is not null)
			{
				var xamlIslandRoot = _mainVisualTree.GetXamlIslandRootForElement(dependencyObject);
				if (xamlIslandRoot is not null)
				{
					return xamlIslandRoot;
				}

				return _mainVisualTree.RootVisual;
			}
			else
			{
				return null;
			}
		}

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

		[NotImplemented]
		internal void UIARaiseFocusChangedEventOnUIAWindow(DependencyObject sender)
		{
		}

		internal void AddXamlIslandRoot(XamlIsland xamlIslandRoot)
		{
			if (_mainVisualTree is null)
			{
				throw new InvalidOperationException("Main visual tree is not initialized.");
			}

			var xamlIslandRootCollection = _mainVisualTree.XamlIslandRootCollection;

			if (xamlIslandRootCollection is null)
			{
				throw new InvalidOperationException("Xaml island root collection is not initialized.");
			}

			var collection = xamlIslandRootCollection.Children;

			collection.Add(xamlIslandRoot);

			// m_pNWWindowRenderTarget->GetDCompTreeHost()->AddXamlIslandTarget(xamlIslandRoot);
		}

		internal void RemoveXamlIslandRoot(XamlIsland xamlIslandRoot)
		{
			_isTearingDownIsland = true;

			var xamlIslandRootCollection = (XamlIslandRootCollection)xamlIslandRoot.GetParentInternal(false /*publicOnly*/);
			if (_mainVisualTree is not null)
			{
				MUX_ASSERT(_mainVisualTree.XamlIslandRootCollection == xamlIslandRootCollection);
			}

			if (xamlIslandRootCollection is null)
			{
				throw new InvalidOperationException("Xaml island root collection is not initialized.");
			}

			var xamlIslands = xamlIslandRootCollection.Children;
			var result = xamlIslands.Remove(xamlIslandRoot);

			if (result)
			{
				xamlIslandRoot.Release();
			}

			// m_pNWWindowRenderTarget->GetDCompTreeHost()->RemoveXamlIslandTarget(xamlIslandRoot);

			if (_inputServices is not null
				&& InitializationType == InitializationType.IslandsOnly
				&& xamlIslands.Count == 0)
			{
				// If the last island is going away and we're in "islands-only" mode, we have some extra cleanup to do.
				// When tests run with non-island hosting, they set "Window.Content = null" at the end of the test,
				// which calls CCoreServices::StartApplication and cleans up the pointer objects.  The equivalent in
				// the islands-only case is to do this when the last island gets removed.
				m_inputServices->DestroyPointerObjects();
			}

			_isTearingDownIsland = false;
		}
	}
}
