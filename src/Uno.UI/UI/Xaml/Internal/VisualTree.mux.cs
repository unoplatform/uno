// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// VisualTree.h, VisualTree.cpp

#nullable enable

using System;
using Uno.Foundation.Logging;
using Uno.UI.__Resources;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Input;
using Uno.UI.Xaml.Islands;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Input;
using static Microsoft.UI.Xaml.Controls._Tracing;

#if __IOS__
using UIKit;
#endif
#if __MACOS__
using AppKit;
#endif

namespace Uno.UI.Xaml.Core;

/// <summary>
/// Contains a single visual tree and is the primary interface
/// for other components to interact with the tree.
/// </summary>
/// <remarks>
/// Uno Platform implementation is mostly a stub for now, needs to be expanded upon.
/// </remarks>
partial class VisualTree
{
	/// <summary>
	/// Replace the existing popup root (if any) with the provided one.
	/// </summary>
	private void EnsurePopupRoot()
	{
		if (PopupRoot == null)
		{
			PopupRoot = new PopupRoot();
			Canvas.SetZIndex(PopupRoot, PopupZIndex);
		}
	}

	/// <summary>
	/// Find the XamlIslandRootCollection in the tree, if there is one
	/// </summary>
	/// <returns></returns>
	private XamlIslandRootCollection? XamlIslandRootCollection
	{
		get
		{
			if (!_shutdownInProgress)
			{
				EnsureXamlIslandRootCollection();
			}

			return _xamlIslandRootCollection;
		}
	}

	private void EnsureFullWindowMediaRoot()
	{
		if (IsMainVisualTree && _fullWindowMediaRoot is null)
		{
			_fullWindowMediaRoot = new FullWindowMediaRoot();
			Canvas.SetZIndex(_fullWindowMediaRoot, FullWindowMediaRootZIndex);
		}
	}

	private void EnsureXamlIslandRootCollection()
	{
		if (IsMainVisualTree && _xamlIslandRootCollection is null)
		{
			_xamlIslandRootCollection = new XamlIslandRootCollection();

			// Protect the root.
			//_xamlIslandRootCollection.EnsurePeerAndTryPeg();
			//_xamlIslandRootCollection.EnsureChildrenCollection();
		}
	}

	internal PopupRoot? PopupRoot
	{
		get
		{
			if (!(_shutdownInProgress || _isShutdown))
			{
				EnsurePopupRoot();
			}
			return _popupRoot;
		}
	}

	internal FullWindowMediaRoot? FullWindowMediaRoot
	{
		get
		{
			if (!_shutdownInProgress)
			{
				EnsureFullWindowMediaRoot();
			}
			return _fullWindowMediaRoot;
		}
	}

	internal void SetPublicRootVisual(
		UIElement? publicRoot,
		ScrollViewer? rootScrollViewer,
		ContentPresenter? rootContentPresenter)
	{
		try
		{
			// NOTE: This doesn't check for the root scroll viewer changing independently of the root visual.
			if (publicRoot == _publicRootVisual)
			{
				return;
			}

			// If the public root visual changes, we need to remove and re-add the existing roots so that
			// they use the new public visual as namescope owner if needed, but we don't want to release them.
			//
			// Note: This needs to be done even if the public root visual is null. The app can explicitly set
			// a null root, which will cause us to create the popup/print/transition/media root elements. When
			// the app sets a non-null root later, we have to make sure to not add those elements a second time.
			//
			ResetRoots(null);

			_publicRootVisual = publicRoot;
			_rootScrollViewer = rootScrollViewer;
			_rootContentPresenter = rootContentPresenter;

			//EnsureVisualDiagnosticsRoot();
			EnsureXamlIslandRootCollection();
			//EnsureConnectedAnimationRoot();
			EnsurePopupRoot();

			//TODO Uno specific: We require some additional layers on top
			EnsureFocusVisualRoot();

			//EnsurePrintRoot();
			//EnsureTransitionRoot();
			EnsureFullWindowMediaRoot();

			//if (_pCoreNoRef.IsInBackgroundTask())
			//{
			//	EnsureRenderTargetBitmapRoot());
			//}

			//if (publicRootVisual != null)
			//{
			//	// A visual set as the root of the tree implicitly becomes a permanent
			//	// namescope owner, and will always have a name store.
			//	publicRootVisual.IsStandardNameScopeOwner = true;
			//	publicRootVisual.IsStandardNameScopeMember = false;
			//}

			//TODO Uno specific - we need to add the public content first, until
			//https://github.com/unoplatform/uno/issues/325 is properly supported.
			if (publicRoot is not null)
			{
				if (RootScrollViewer != null)
				{
					//// A visual set as the root SV of the tree implicitly becomes a permanent
					//// namescope owner, and will always have a name store.
					//_rootScrollViewer.tIsStandardNameScopeOwner = true;
					//_rootScrollViewer.IsStandardNameScopeMember = false;

					//// Add the visual root as the child of the root ScrollViwer
					//AddVisualRootToRootScrollViewer(PublicRootVisual);

					//// Add the root ScrollViewer to the hidden root visual
					//AddRootScrollViewer(RootScrollViewer);

					//_bIsRootScrollViewerAddedToRoot = true;
				}
				else
				{
					AddRoot(publicRoot);
				}

				// TODO: Probably needed in https://github.com/unoplatform/uno/issues/2895
				//_core?.RaisePendingLoadedRequests();
			}

			// Re-enter the roots with the new public root's namescope.
			//AddRoot(_visualDiagnosticsRoot));
			AddRoot(_xamlIslandRootCollection);
			//AddRoot(_connectedAnimationRoot));
#if !__MACOS__
			AddRoot(PopupRoot);
#endif
			//AddRoot(_printRoot);
			//AddRoot(_transitionRoot);
			AddRoot(_fullWindowMediaRoot);

			//AddRoot(_printRoot));
			//AddRoot(_transitionRoot));

			//TODO Uno specific: Focus visual layer
			AddRoot(FocusVisualRoot);

			//if (_pCoreNoRef.IsInBackgroundTask())
			//{
			//	AddRoot(_renderTargetBitmapRoot));
			//}

			ContentRoot.AddPendingXamlRootChangedEvent(ContentRoot.ChangeType.Content);
		}
		catch (Exception ex)
		{
			ResetRoots(null);
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().LogError("Could not set public root visual.", ex);
			}
		}
	}

	internal ContentRoot ContentRoot => _coreContentRoot;

	//internal QualifierContext QualifierContext => _pQualifierContext;

	/// <summary>
	/// Initializes a Visual tree.
	/// </summary>
	/// <param name="rootElement">Root element.</param>
	/// <param name="contentRoot">Content root.</param>
	/// <remarks>
	/// Our implementation is currently simplified whereas WinUI can create the root element
	/// if not passed in.
	/// </remarks>
	public VisualTree(CoreServices coreServices, Color backgroundColor, UIElement? rootElement, ContentRoot contentRoot)
	{
		_coreServices = coreServices ?? throw new ArgumentNullException(nameof(coreServices));
		_coreContentRoot = contentRoot ?? throw new ArgumentNullException(nameof(contentRoot));

		if (rootElement is not null)
		{
			_rootElement = rootElement;
		}
		else
		{
			_rootVisual = new(coreServices);
			//// Mark the element as the root of the render walk.
			//_rootVisual->SetRequiresComposition(
			//	CompositionRequirement::RootElement,
			//	IndependentAnimationType::None
			//	);
			_rootVisual.AssociatedVisualTree = this;
			//// Mark the root as used to prevent circular dependencies
			//m_rootVisual->SetAssociated(true, nullptr /*Association owner needed only for shareable, non-parent aware DOs */);

			RootVisual.SetBackgroundColor(backgroundColor);


			_rootElement = _rootVisual;
		}

		//if (_coreContentRoot.Type == ContentRootType.CoreWindow)
		//{
		//	const auto config = XamlOneCoreTransforms::IsEnabled() ? RootScaleConfig::ParentApply : RootScaleConfig::ParentInvert;
		//	m_rootScale = std::make_shared<CoreWindowRootScale>(config, m_pCoreNoRef, this);
		//}
		//else
		//{
		//	m_rootScale = std::make_shared<XamlIslandRootScale>(m_pCoreNoRef, this);
		//}

		_focusInputHandler = new UnoFocusInputHandler(_rootElement);
	}

	/// <summary>
	/// Static helper function that encapsulates walking up the tree
	/// to get the CRootVisual.  This function returns NULL if walking
	/// up the parent chain does not reach a CRootVisual.
	/// </summary>
	/// <remarks>This is NOT the root element, it is the *root visual*!</remarks>
	internal static RootVisual? GetRootForElement(DependencyObject pObject) =>
		pObject.GetContext().MainRootVisual;

	/// <summary>
	/// Static helper function that encapsulates getting the FocusManager
	/// that is associated with the VisualTree containing the provided
	/// object.  If the object is not contained within a VisualTree's tree
	/// then this method falls back to returning the FocusManager
	/// associated with the core's main VisualTree.
	/// </summary>
	/// <param name="dependencyObject">Dependency object.</param>
	/// <param name="options">Lookup options</param>
	internal static FocusManager? GetFocusManagerForElement(DependencyObject? dependencyObject, LookupOptions options = LookupOptions.WarningIfNotFound)
	{
		if (GetContentRootForElement(dependencyObject, options) is { } contentRoot)
		{
			return contentRoot.FocusManager;
		}

		if (dependencyObject?.GetContext().ContentRootCoordinator.Unsafe_XamlIslandsIncompatible_CoreWindowContentRoot is { } coreWindowContentRoot)
		{
			return coreWindowContentRoot.FocusManager;
		}

		return null;
	}

	internal static InputManager? GetInputManagerForElement(DependencyObject dependencyObject)
	{
		if (GetContentRootForElement(dependencyObject) is { } contentRoot)
		{
			return contentRoot.InputManager;
		}

		return null;
	}

	/// <summary>
	/// Static helper function that encapsulates getting the PopupRoot
	/// that is associated with the provided object. If the object is not
	/// contained within a VisualTree's tree then this method checks if
	/// the object is a Popup, or if the root of its visual tree is the
	/// logical child of a Popup. If so, it gets the PopupRoot associated
	/// with that Popup. Otherwise, this method falls back to returning
	/// the PopupRoot of the core's main VisuaTree.
	/// </summary>
	/// <param name="dependencyObject">Dependency object.</param>
	/// <returns></returns>
	public static PopupRoot? GetPopupRootForElement(DependencyObject dependencyObject)
	{
		if (dependencyObject is null)
		{
			throw new ArgumentNullException(nameof(dependencyObject));
		}

		var core = dependencyObject.GetContext();

		var popup = dependencyObject as Popup;
		if (popup is not null)
		{
			// The PopupRoot may be disconnected from its parent by this point.
			if (popup.Child is UIElement child)
			{
				if (child.GetParentInternal(false /*publicParentOnly*/) is PopupRoot parentPopupRoot)
				{
					return parentPopupRoot;
				}
			}
		}

		if (GetForElement(dependencyObject) is VisualTree visualTree)
		{
			if (visualTree.PopupRoot is PopupRoot visualTreePopupRoot)
			{
				return visualTreePopupRoot;
			}
		}

		// TODO Uno: Add proper support for XamlIslandRootCollection #8978.
		if (popup is not null)
		{
			// If this is an unparented popup, then we don't have a parent, and there's no ancestor chain leading up to a
			// popup root. We can try one last thing - since the popup is open, some popup root knows about it. Walk every
			// XamlIslandRoot and check whether its popup root contains this open popup.
			var mainVisualTree = core.MainVisualTree;

			if (mainVisualTree is not null)
			{
				var xamlIslandRootCollection = mainVisualTree.XamlIslandRootCollection;

				if (xamlIslandRootCollection is not null)
				{
					var xamlIslandRoots = xamlIslandRootCollection.GetChildren();

					if (xamlIslandRoots is not null)
					{
						foreach (DependencyObject xamlIslandRootDO in xamlIslandRoots)
						{
							var xamlIslandRoot = xamlIslandRootDO as XamlIsland;
							MUX_ASSERT(xamlIslandRoot is not null);

							PopupRoot popupRoot = xamlIslandRoot.PopupRoot;

							if (popupRoot != null && (popupRoot.ContainsOpenOrUnloadingPopup(popup)))
							{
								return popupRoot;
							}
						}
					}
				}
			}
		}

		if (core.HasXamlIslands && core.InitializationType == InitializationType.IslandsOnly)
		{
			// We failed to find a XamlIslandRoot. Rather than defaulting to the CRootVisual's popup root, return null here.
			// We don't want to use the CRootVisual's popup root because the entire CRootVisual tree is non-live when Xaml islands
			// are involved. If we use that popup root, we'll walk up that subtree when rendering the popup, fail to find a comp
			// node, and crash. If we return a null popup root, then the popup will just not render.
			return null;
		}

		return core.MainPopupRoot;
	}
}
