// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// VisualTree.h, VisualTree.cpp

#nullable enable

using System;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Input;
using Uno.UI.Xaml.Islands;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

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
internal partial class VisualTree
{
	private const int VisualDiagnosticsRootZIndex = UnoTopZIndex - 1;
	private const int ConnectedAnimationRootZIndex = VisualDiagnosticsRootZIndex - 1;
	private const int PopupZIndex = ConnectedAnimationRootZIndex - 1;
	private const int FullWindowMediaRootZIndex = PopupZIndex - 1;

	internal enum LookupOptions
	{
		/// <summary>
		/// Normal lookup.
		/// </summary>
		NoFallback = 0,

		/// <summary>
		/// Provides warning if not found.
		/// </summary>
		WarningIfNotFound = 1
	}

	private readonly CoreServices _coreServices;
	private readonly UnoFocusInputHandler? _focusInputHandler;

	

	internal UnoFocusInputHandler? UnoFocusInputHandler => _focusInputHandler;

	public ContentRoot ContentRoot { get; }

	public UIElement? PublicRootVisual { get; private set; }

	public ScrollViewer? RootScrollViewer { get; private set; }

	public ContentPresenter? RootContentPresenter { get; private set; }

	public XamlRoot? XamlRoot { get; private set; }

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

	



	internal static ContentRoot? GetContentRootForElement(DependencyObject? dependencyObject, LookupOptions options = LookupOptions.WarningIfNotFound)
	{
		if (GetForElement(dependencyObject, options) is { } visualTree)
		{
			return visualTree.ContentRoot;
		}

		if (dependencyObject?.GetContext().ContentRootCoordinator.Unsafe_XamlIslandsIncompatible_CoreWindowContentRoot is ContentRoot contentRoot)
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
		if (root != null)
		{
			//TODO Uno: The logic here is more complex in WinUI,
			//setting the namespace owner. Not needed currently.

			MUX_ASSERT(RootElement != null);
			RootElement!.AddChild(root);
		}
	}


	private void ResetRoots()
	{
		//TODO Uno: We currently never reset existing roots for backwards compatability
		//with existing infrastructure. This should be adjusted later.
		//if (_connectedAnimationRoot != null)
		//{
		//	RemoveRoot(_connectedAnimationRoot);
		//}

		//if (_visualDiagnosticsRoot)
		//{
		//	RemoveRoot(_visualDiagnosticsRoot);
		//}

		//if (_popupRoot && _rootElement)
		//{
		//	// The popup root does not always get cleared so it needs to be checked in the collection before attempting to remove
		//	CUIElementCollection* pCollection = static_cast<CUIElementCollection*>(_rootElement->GetChildren();
		//	if (pCollection)
		//	{
		//		XINT32 iIndex = -1;
		//		IFC(pCollection->IndexOf(_popupRoot, &iIndex);
		//		if (iIndex >= 0)
		//		{
		//			RemoveRoot(_popupRoot);
		//		}
		//	}
		//}

		if (FocusVisualRoot is not null)
		{
			RemoveRoot(FocusVisualRoot);
		}

		if (PopupRoot is not null)
		{
			RemoveRoot(PopupRoot);
		}

		//if (_printRoot != null)
		//{
		//	RemoveRoot(_printRoot);
		//}

		//if (_transitionRoot != null)
		//{
		//	RemoveRoot(_transitionRoot);
		//}

		if (FullWindowMediaRoot != null)
		{
			RemoveRoot(FullWindowMediaRoot);
		}

		//if (_renderTargetBitmapRoot != null)
		//{
		//	RemoveRoot(_renderTargetBitmapRoot != null);
		//}

		//if (_xamlIslandRootCollection != null)
		//{
		//	RemoveRoot(_xamlIslandRootCollection);
		//}

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
			if (PublicRootVisual is not null)
			{
				RemoveRoot(PublicRootVisual);

				// The public root visual is always released, regardless of 'resetRoots'.
				PublicRootVisual = null;
			}
		}
	}

	private void RemoveRoot(UIElement root) => RootElement.RemoveChild(root);

	[NotImplemented]
	internal bool IsBehindFullWindowMediaRoot(DependencyObject? focusedElement)
	{
		if (focusedElement == null)
		{
			return false;
		}

		//TODO Uno: Implement full window media root.
		//DependencyObject pActiveRoot = ActiveRootVisual;
		//if (pActiveRoot == null)
		//{
		//	return false;
		//}

		//if (pActiveRoot == _fullWindowMediaRoot)
		//{
		//	DependencyObject pPublicRoot = PublicRootVisual;
		//	DependencyObject pParent = focusedElement;
		//	while (pParent != null)
		//	{
		//		if (pParent == pPublicRoot)
		//		{
		//			return true;
		//		}
		//		else if (pParent == pActiveRoot)
		//		{
		//			return false;
		//		}
		//		pParent = pParent.GetParent();
		//	}
		//}

		return false;
	}

#if false
	/// <summary>
	/// Removes the given root from the implicit root visual, and potentially 'Leave' it
	/// from the tree.
	/// </summary>
	/// <param name="root">Root to remove.</param>
	/// <remarks>
	/// A root leaves the tree if we can retrieve a namescope owner for it
	/// using GetNamescopeOwnerForRoot.
	/// A precondition of this function is that the root being entered is already set
	/// in its corresponding member variable (e.g. m_popupRoot, m_publicRootVisual, etc.)
	/// </remarks>
	[NotImplemented]
	private void RemoveRoot(UIElement root)
	{
		// TODO Uno: Implement when multi-window support is added.
		return;
		//UIElement publicRoot = PublicRootVisual;
		//DependencyObject pNamescopeOwner = GetNamescopeOwnerForRoot(pRoot);

		//LeaveParams leaveParams(
		//	/*fIsLive*/ pPublicRoot? pPublicRoot->IsActive() : FALSE,
		//	/*fSkipNameRegistration*/ TRUE,
		//	/*fCoercedIsEnabled*/     TRUE,
		//	/*fVisualTreeBeingReset*/ TRUE
		//);

		//if (pNamescopeOwner)
		//{
		//	IFC(pRoot->Leave(pNamescopeOwner, leaveParams));
		//}
		//_rootElement.RemoveChild(root);

		// Ensure that incremental PC render data is cleaned up.
		// This would normally happen during a 'live' Leave, but the Leave call here doesn't always happen,
		// isn't always live, and there's no live Leave generated by the RemoveChild call either since m_rootVisual
		// itself is never live.
		// publicRoot.LeavePCSceneRecursive();
	}
#endif

	[NotImplemented]
	private static UIElement? GetXamlIslandRootForElement(DependencyObject? pObject)
	{
		//if (!pObject || !pObject->GetContext()->HasXamlIslands())
		//{
		//	return nullptr;
		//}
		if (GetForElement(pObject) is VisualTree visualTree)
		{
			return visualTree.RootElement;
		}
		return null;
	}

	internal static VisualTree? GetForElement(DependencyObject? element, LookupOptions options = LookupOptions.WarningIfNotFound)
	{
		if (element == null)
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
				return xamlIslandRoot.ContentRoot.VisualTree;
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

			//TODO Uno: Uncomment and implement
			////
			//// We have a few tricks to figure out which VisualTree an element may be associated with.
			//// There is now a cached weak VisualTree pointer on each DO that we update when we do a live
			//// enter, so we may be able to remove some of these lookups.  Let's investigate this with bug 19548424.
			////

			//if (Popup popup = do_pointer_cast<CPopup>(currentAncestor))
			//{
			//	if (CDependencyObject * owner = popup->GetToolTipOwner())
			//	{
			//		nextAncestor = owner;
			//	}

			//	if (CFlyoutBase * flyout = popup->GetAssociatedFlyoutNoRef())
			//	{
			//		if (VisualTree * visualTree = VisualTree::GetForElementNoRef(flyout))
			//		{
			//			return visualTree;
			//		}
			//	}

			//	if (nextAncestor == null && popup->IsOpen())
			//	{
			//		// If the popup is open, its child will be parented to a PopupRoot.  We
			//		// can quickly fine the root from there.
			//		CUIElement* popupChild = popup->GetChildNoRef();
			//		if (popupChild)
			//		{
			//			nextAncestor = popupChild->GetParentInternal(false /* public parent only */);
			//		}
			//	}
			//}

			//if (nextAncestor == null)
			//{
			//	if (CCollection * collection = do_pointer_cast<CCollection>(currentAncestor))
			//	{
			//		nextAncestor = collection->GetOwner();
			//	}
			//}

			//// If the next visual parent is null, check the logical parent. Popups are connected to their child with
			//// a logical link. Only allow the walk up if the logical parent is a popup.
			//if (nextAncestor == null)
			//{
			//	if (CUIElement * currentUIE = do_pointer_cast<CUIElement>(currentAncestor))
			//	{
			//		CPopup* nextAncestorPopup = do_pointer_cast<CPopup>(currentUIE->GetLogicalParentNoRef());
			//		nextAncestor = nextAncestorPopup;
			//	}
			//}

			currentAncestor = nextAncestor;
		}
		if (result == null)
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
		if (root == null)
		{
			return GetRootForElement(element);
		}

		return root;
	}

	internal XamlRoot GetOrCreateXamlRoot()
	{
		if (XamlRoot is null)
		{
			XamlRoot = new XamlRoot(this);
		}

		return XamlRoot;
	}

	private static void VisualTreeNotFoundWarning()
	{
		if (typeof(VisualTree).Log().IsEnabled(LogLevel.Debug))
		{
			typeof(VisualTree).Log().LogDebug("Visual Tree was not found.");
		}
	}
}
