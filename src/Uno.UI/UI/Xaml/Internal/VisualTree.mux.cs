// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\core\inc\VisualTree.cpp, tag winui3/release/1.4.3, commit 685d2bfa86d6169aa1998a7eaa2c38bfcf9f74bc

#nullable enable

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Input;
using Uno.UI.Xaml.Islands;
using Windows.UI;
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
	private static void VisualTreeNotFoundWarning()
	{
		if (typeof(VisualTree).Log().IsEnabled(LogLevel.Debug))
		{
			typeof(VisualTree).Log().LogDebug("Visual Tree was not found.");
		}
	}

	/// <summary>
	/// Replace the existing popup root (if any) with the provided one.
	/// </summary>
	private void EnsurePopupRoot()
	{
		if (_popupRoot is null)
		{
			_popupRoot = new();
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
			_fullWindowMediaRoot = new();
			Canvas.SetZIndex(_fullWindowMediaRoot, FullWindowMediaRootZIndex);
		}
	}

	private void EnsureXamlIslandRootCollection()
	{
		if (IsMainVisualTree && _xamlIslandRootCollection is null)
		{
			_xamlIslandRootCollection = new();

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
			ResetRoots(out var _);

			_publicRootVisual = publicRoot;
			_rootScrollViewer = rootScrollViewer;
			_rootContentPresenter = rootContentPresenter;

			//EnsureVisualDiagnosticsRoot();
			EnsureXamlIslandRootCollection();
			//EnsureConnectedAnimationRoot();
			EnsurePopupRoot();
			//EnsurePrintRoot();
			//EnsureTransitionRoot();
			EnsureFullWindowMediaRoot();

#if HAS_UNO
			//TODO Uno specific: We require some additional layers on top
			EnsureFocusVisualRoot();
#endif

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

#if HAS_UNO
			//TODO Uno specific: Focus visual layer
			AddRoot(FocusVisualRoot);
#endif

			//if (_pCoreNoRef.IsInBackgroundTask())
			//{
			//	AddRoot(_renderTargetBitmapRoot));
			//}

			ContentRoot.AddPendingXamlRootChangedEvent(ContentRoot.ChangeType.Content);
		}
		catch (Exception ex)
		{
			ResetRoots(out var _);
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

			_rootVisual.SetBackgroundColor(backgroundColor);

			// m_layoutManager = std::make_shared<CLayoutManager>(m_pCoreNoRef, this);
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
	internal static RootVisual? GetRootForElement(DependencyObject? pObject) =>
		pObject?.GetContext().MainRootVisual;

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
	internal static PopupRoot? GetPopupRootForElement(DependencyObject dependencyObject)
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

							var popupRoot = xamlIslandRoot?.PopupRoot;

							if (popupRoot != null) // TODO Uno: Check for open or unloading - && (popupRoot.ContainsOpenOrUnloadingPopup(popup)))
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

	internal static FullWindowMediaRoot? GetFullWindowMediaRootForElement(DependencyObject dependencyObject)
	{
		var pRootVisual = GetRootForElement(dependencyObject);
		if (pRootVisual is not null)
		{
			var pVisualTree = pRootVisual.AssociatedVisualTree;
			return pVisualTree?.FullWindowMediaRoot;
		}

		return null;
	}

	internal static ContentRoot? GetContentRootForElement(DependencyObject dependencyObject, LookupOptions options = LookupOptions.WarningIfNotFound)
	{
		if (GetForElement(dependencyObject, options) is { } visualTree)
		{
			return visualTree.ContentRoot;
		}

		if (dependencyObject.GetContext().ContentRootCoordinator.Unsafe_XamlIslandsIncompatible_CoreWindowContentRoot is { } contentRoot)
		{
			return contentRoot;
		}

		return null;
	}

	/// <summary>
	/// Adds the given root to the implicit root visual, and potentially 'Enter' it into
	/// the tree.
	/// </summary>
	/// <param name="root"></param>
	/// <remarks>
	/// A root is entered into the tree if we can retrieve a namescope owner for it
	/// using GetNamescopeOwnerForRoot.
	/// A precondition of this function is that the root being entered is already set
	/// in its corresponding member variable (e.g. _popupRoot, _publicRootVisual, etc.)
	/// </remarks>
	private void AddRoot(UIElement? root)
	{
		if (root is not null)
		{
			// CDependencyObject* pNamescopeOwner = GetNamescopeOwnerForRoot(pRoot);
			bool isXamlIslandRootCollection = root == _xamlIslandRootCollection;

			// The XamlIslandRootCollection is not a part of any VisualTree -- it's parent
			// RootVisual defines a VisualTree, and it's children, XamlIslandRoots will
			// define their own VisualTrees. During development, we've seen it's helpful
			// to make sure the RootVisual VisualTree pointer doesn't propagate down -- so
			// we set the VisualTree param null here as we enter.
			//
			//EnterParams enterParams(
			//	/*isLive*/                TRUE,
			//	/*skipNameRegistration*/  TRUE,
			//	/*coercedIsEnabled*/      TRUE,
			//	/*useLayoutRounding*/     EnterParams::UseLayoutRoundingDefault,
			//	/*visualTree*/            isXamlIslandRootCollection? nullptr : this
			//);

			MUX_ASSERT(_rootElement != null);
			_rootElement!.AddChild(root);

			//if (IsMainVisualTree() && pNamescopeOwner)
			//{
			//	IFC_RETURN(pRoot->Enter(pNamescopeOwner, enterParams));
			//}
		}
	}

	/// <summary>
	/// Removes the given root from the implicit root visual, and potentially 'Leave' it
	/// from the tree.
	/// </summary>
	/// <param name="root"></param>
	private void RemoveRoot(UIElement root)
	{
		//UIElement* pPublicRoot = GetPublicRootVisual();
		//CDependencyObject* pNamescopeOwner = GetNamescopeOwnerForRoot(pRoot);

		//LeaveParams leaveParams(
		//	/*fIsLive*/               pPublicRoot? pPublicRoot->IsActive() : FALSE,
		//	/*fSkipNameRegistration*/ TRUE,
		//	/*fCoercedIsEnabled*/     TRUE,
		//	/*fVisualTreeBeingReset*/ TRUE
		//);

		//if (pNamescopeOwner)
		//{
		//	IFC_RETURN(pRoot->Leave(pNamescopeOwner, leaveParams));
		//}
		_rootElement.RemoveChild(root);

		// Ensure that incremental PC render data is cleaned up.
		// This would normally happen during a 'live' Leave, but the Leave call here doesn't always happen,
		// isn't always live, and there's no live Leave generated by the RemoveChild call either since m_rootVisual
		// itself is never live.
		//pRoot->LeavePCSceneRecursive();
	}

	private void ResetRoots(out bool releasedPopup)
	{
		releasedPopup = _popupRoot is not null;

		//if (_connectedAnimationRoot != null)
		//{
		//	RemoveRoot(_connectedAnimationRoot);
		//}

		//if (_visualDiagnosticsRoot)
		//{
		//	RemoveRoot(_visualDiagnosticsRoot);
		//}

		if (_popupRoot is not null && _rootElement is not null)
		{
			// The popup root does not always get cleared so it needs to be checked in the collection before attempting to remove
			var children = _rootElement.GetChildren();
			if (children is not null)
			{
				if (children.IndexOf(_popupRoot) >= 0)
				{
					RemoveRoot(_popupRoot);
				}
			}
		}

		if (FocusVisualRoot is not null)
		{
			RemoveRoot(FocusVisualRoot);
		}

		//if (_printRoot != null)
		//{
		//	RemoveRoot(_printRoot);
		//}

		//if (_transitionRoot != null)
		//{
		//	RemoveRoot(_transitionRoot);
		//}

		if (_fullWindowMediaRoot != null)
		{
			RemoveRoot(_fullWindowMediaRoot);
		}

		//if (_renderTargetBitmapRoot != null)
		//{
		//	RemoveRoot(_renderTargetBitmapRoot != null);
		//}

		if (_xamlIslandRootCollection != null)
		{
			RemoveRoot(_xamlIslandRootCollection);
		}

		//if (PublicRootVisual != null)
		//{
		//	// ToolTipService attaches handlers to the public root of the main window and each Xaml island. Clean up its
		//	// bookkeeping now that the root is going away.
		//	//ToolTipService.OnPublicRootRemoved(_publicRootVisual);
		//}

		if (false) //RootScrollViewer is not null && _bIsRootScrollViewerAddedToRoot)
		{
			//// Remove both root ScrollViewer and visual root from the tree
			//RemoveRootScrollViewer(RootScrollViewer);
			//RemoveVisualRootFromRootScrollViewer(PublicRootVisual);

			//// The public visual root is always released immediately.
			//// But we keep the root ScrollViewer reference to reuse it
			//// for new public visual root.
			//PublicRootVisual = null;

			//_bIsRootScrollViewerAddedToRoot = false;
		}
		else
		{
			// Public root visual is always removed last
			if (_publicRootVisual is not null)
			{
				RemoveRoot(_publicRootVisual);

				// The public root visual is always released, regardless of 'resetRoots'.
				_publicRootVisual = null;
			}
		}
	}

	//internal Microsoft.UI.Xaml.DependencyObject[] GetAllVisibleRoots()
	//{
	//	var roots = new Microsoft.UI.Xaml.DependencyObject[3];
	//	var contentRoot = ContentRoot;
	//	if (contentRoot.XamlIslandRoot is { } xamlIslandRoot)
	//	{
	//		roots[0] = xamlIslandRoot;
	//	}
	//	else
	//	{
	//		roots[0] = RootVisual;
	//	}
	//	roots[1] = PopupRoot;
	//	roots[2] = FullWindowMediaRoot;
	//}

	/// <summary>
	/// Gets the currently active root visual - can be either public root visual or full-window
	/// media root if it is currently active.
	/// </summary>
	public DependencyObject? ActiveRootVisual
	{
		get
		{
			UIElement? candidateRoot = FullWindowMediaRoot;
			if (candidateRoot != null)
			{
				if (candidateRoot is UIElement uiElement && uiElement.Visibility == Visibility.Collapsed)
				{
					candidateRoot = null;
				}
			}

			if (candidateRoot == null)
			{
				candidateRoot = PublicRootVisual;
			}

			return candidateRoot;
		}
	}

	[NotImplemented]
	internal bool IsBehindFullWindowMediaRoot(DependencyObject? focusedElement)
	{
		if (focusedElement is null)
		{
			return false;
		}

		var pActiveRoot = ActiveRootVisual;
		if (pActiveRoot is null)
		{
			return false;
		}

		if (pActiveRoot == _fullWindowMediaRoot)
		{
			var pPublicRoot = PublicRootVisual;
			object pParent = focusedElement;
			while (pParent != null)
			{
				if (pParent == pPublicRoot)
				{
					return true;
				}
				else if (pParent == pActiveRoot)
				{
					return false;
				}
				pParent = pParent.GetParent();
			}
		}

		return false;
	}

	private static UIElement? GetXamlIslandRootForElement(DependencyObject? pObject)
	{
		if (pObject is null || !pObject.GetContext().HasXamlIslands)
		{
			return null;
		}

		if (GetForElement(pObject) is VisualTree visualTree)
		{
			return visualTree.RootElement;
		}
		return null;
	}

	internal static VisualTree? GetForElement(DependencyObject? element, LookupOptions options = LookupOptions.WarningIfNotFound)
	{
		if (element is null)
		{
			return null;
		}

		if (element.GetVisualTree() is VisualTree visualTree)
		{
			return visualTree;
		}

		var result = GetVisualTreeViaTreeWalk(element, options);
		if (result != null)
		{
			// We found the right VisualTree -- we might as well remember it!
			element.SetVisualTree(result);
		}
		return result;
	}

	internal static VisualTree? GetVisualTreeViaTreeWalk(DependencyObject element, LookupOptions options)
	{
		DependencyObject? currentAncestor = element;

		const bool isDebugMode =
#if VISUALTREEWALK_DEBUG
			true;
#else
			false;
#endif

		VisualTree? result = null;

		// We cap the tree walk because we've found at least one case (Skype app -- 19h1) where there's a cycle in the tree.
		int iterationsLeft = 1000;

		// Walk up the tree to find either the XamlIslandRoot or RootVisual
		while (currentAncestor != null && iterationsLeft != 0)
		{
			--iterationsLeft;
			if (currentAncestor.GetVisualTree() is VisualTree visualTree)
			{
				if (isDebugMode)
				{
					// In debug mode, let's keep walking up the tree, even though
					// we found the answer, just to make sure the tree is consistent.
#pragma warning disable CS0162 // Unreachable code detected
					if (result != null)
					{
						MUX_ASSERT(visualTree == result);
					}
					else
					{
						result = visualTree;
					}
#pragma warning restore CS0162 // Unreachable code detected
				}
				else
				{
					return visualTree;
				}
			}

			if (currentAncestor is XamlIsland xamlIslandRoot)
			{
				return xamlIslandRoot.VisualTree;
			}
			else if (currentAncestor is RootVisual rootVisual)
			{
				return rootVisual.AssociatedVisualTree;
			}

			DependencyObject? nextAncestor = null;
			//TODO Uno: Uncomment and implement
			if (false)//currentAncestor.DoesAllowMultipleAssociation() && currentAncestor.GetParentCount() > 1)
			{
				// We cannot travese up a tree through a multiply associated element.  Our goal is to support DOs being
				// shared between XAML trees.  We've seen cases where we traverse up the tree through CSetter objects,
				// so for now we allow the traversal if there's one unique parent.  TODO: This could be fragile?  Allowing
				// the traversal to happen when the parent count is 1 means that if this element gets another parent later,
				// we're now in an inconsistent state.
				// Bug 19548424: Investigate places where an element entering the tree doesn't have a unique VisualTree ptr
			}
			else
			{
				nextAncestor = currentAncestor.GetParentInternal(false /* public parent only */);
			}

			//
			// We have a few tricks to figure out which VisualTree an element may be associated with.
			// There is now a cached weak VisualTree pointer on each DO that we update when we do a live
			// enter, so we may be able to remove some of these lookups.  Let's investigate this with bug 19548424.
			//

			if (currentAncestor is Popup popup)
			{
				// TODO Uno: Port Tooltips
				//if (popup.GetToolTipOwner() is { } owner)
				//{
				//	nextAncestor = owner;
				//}

				if (popup.AssociatedFlyout is { } flyout)
				{
					if (VisualTree.GetForElement(flyout) is { } visualTreeFlyout)
					{
						return visualTreeFlyout;
					}
				}

				if (nextAncestor is null && popup.IsOpen)
				{
					// If the popup is open, its child will be parented to a PopupRoot.  We
					// can quickly fine the root from there.
					var popupChild = popup.Child;
					if (popupChild is not null)
					{
						nextAncestor = popupChild.GetParentInternal(false /* public parent only */);
					}
				}
			}

			// TODO Uno: Implement remaining logic
			//if (nextAncestor == nullptr)
			//{
			//	if (CCollection * collection = do_pointer_cast<CCollection>(currentAncestor))
			//	{
			//		nextAncestor = collection->GetOwner();
			//	}
			//}

			//// If the next visual parent is null, check the logical parent. Popups are connected to their child with
			//// a logical link. Only allow the walk up if the logical parent is a popup.
			//if (nextAncestor == nullptr)
			//{
			//	if (CUIElement * currentUIE = do_pointer_cast<CUIElement>(currentAncestor))
			//	{
			//		CPopup* nextAncestorPopup = do_pointer_cast<CPopup>(currentUIE->GetLogicalParentNoRef());
			//		nextAncestor = nextAncestorPopup;
			//	}
			//}

			//// If this is a hyperlink's automation peer, check if the owner is in the tree.
			//if (nextAncestor == nullptr)
			//{
			//	if (CHyperlinkAutomationPeer * currentHyperlinkAP = do_pointer_cast<CHyperlinkAutomationPeer>(currentAncestor))
			//	{
			//		CHyperlink* currentHyperlink = do_pointer_cast<CHyperlink>(currentHyperlinkAP->GetDONoRef());
			//		nextAncestor = currentHyperlink;
			//	}
			//}

			currentAncestor = nextAncestor;
		}
		if (result is null)
		{
			if (options == LookupOptions.WarningIfNotFound)
			{
				// We didn't find anything
				VisualTreeNotFoundWarning();
			}
		}
		return result;
	}

	internal static UIElement? GetRootOrIslandForElement(DependencyObject? element)
	{
		var root = GetXamlIslandRootForElement(element);
		if (root is null)
		{
			return GetRootForElement(element);
		}

		return root;
	}

	//internal void CloseAllPopupsForTreeReset()
	//{
	//	if (_popupRoot is not null)
	//	{
	//		_popupRoot.CloseAllPopupsForTreeReset();
	//	}
	//}

	internal XamlRoot GetOrCreateXamlRoot()
	{
		_xamlRoot ??= new(this);

		return _xamlRoot;
	}

	internal XamlRoot? XamlRoot => _xamlRoot;

	/// <summary>
	/// Helper function for types like Popup, Flyout, and ContentDialog to figure out which VisualTree
	/// it's attached to.  If there is no clear, unambiguous answer, this function returns an error.
	/// </summary>
	internal static VisualTree? GetUniqueVisualTree(
		DependencyObject element,
		DependencyObject? positionReferenceElement,
		VisualTree explicitTree)
	{
		var result = VisualTree.GetForElement(element);
		//if (result is null)
		//{
		//	// For a flyout that's on a button via e.g. Button.Flyout, we need to follow the mentor
		//	// pointer to the Button and find the tree it belongs to.
		//	result = VisualTree.GetForElement(element.Mentor);
		//}

		if (positionReferenceElement is not null)
		{
			var referenceTree = VisualTree.GetForElement(positionReferenceElement);
			if (result is null)
			{
				result = referenceTree;
			}
			else if (result != referenceTree)
			{
				throw new InvalidOperationException("XamlRoot ambiguous");
			}
		}

		if (explicitTree is not null)
		{
			if (result is null)
			{
				result = explicitTree;
			}
			else if (result != explicitTree)
			{
				throw new InvalidOperationException("XamlRoot ambiguous");
			}
		}

		if (result is null)
		{
			var core = element.GetContext();
			if (core.InitializationType == InitializationType.IslandsOnly)
			{
				// If we started in an "islands-only way" (e.g., DesktopWindowXamlSource) there in no XAML content
				// attached to the main window.  In this case, we can't fall back to the main visual tree -- this is
				// an error.
				throw new InvalidOperationException("XamlRoot ambiguous");
			}
			// The answer is unclear, so fall back to the MainVisualTree (the XAML content on the CoreWindow)
			result = core.MainVisualTree;
		}

		return result;
	}

	internal void AttachElement(DependencyObject element)
	{
		var uniqueTree = VisualTree.GetUniqueVisualTree(
			element,
			null /*positionReferenceElement*/,
			this /*explicitTree*/);

		MUX_ASSERT(this == uniqueTree);
		element.SetVisualTree(this);
	}
}
