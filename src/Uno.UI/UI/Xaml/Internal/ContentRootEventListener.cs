// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\components\ContentRoot\ContentRootEventListener.cpp, tag winui3/release/1.4.3, commit 685d2bfa86d6169aa1998a7eaa2c38bfcf9f74bc

// TODO Uno: Mostly not implemented for now.

using System;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Uno.UI.Xaml.Core;

internal class ContentRootEventListener
{
	private UIElement m_element;

	public ContentRootEventListener(ContentRoot contentRoot)
	{
		m_element = contentRoot.VisualTree.RootElement;
		//RegisterTabProcessEventHandler();
		//RegisterContextMenuOpeningEventHandler();
		//RegisterManipulationInertiaProcessingEventHandler();
		//RegisterRightTappedEventHandler();
	}

	//~ContentRootEventListener()
	//{
	//	//UnregisterTabProcessEventHandler();
	//	//UnregisterContextMenuOpeningEventHandler();
	//	//UnregisterManipulationInertiaProcessingEventHandler();
	//	//UnregisterRightTappedEventHandler();
	//}

	private void RegisterTabProcessEventHandler()
	{
		//m_element.TabProcessing += TabProcessingEventListener;
	}

	private void UnregisterTabProcessEventHandler()
	{
		//m_element.TabProcessing -= TabProcessingEventListener;
	}

	private void TabProcessingEventListener(object sender, KeyEventArgs e)
	{
		//if (e.Handled)
		//{
		//	return;
		//}

		//FocusManager focusManager = VisualTree.GetFocusManagerForElement(sender as DependencyObject);

		//// For TH2 we shipped without bubling up errors from TAB focus navigation
		//focusManager.ProcessTabStop(e.IsShiftPressed, out e.Handled);
	}

	private void RegisterContextMenuOpeningEventHandler()
	{
		//m_element.ContextMenuOpening += ContextMenuOpeningEventListener;
	}

	private void UnregisterContextMenuOpeningEventHandler()
	{
		//m_element.ContextMenuOpening -= ContextMenuOpeningEventListener;
	}

	private void ContextMenuOpeningEventListener(object sender, ContextMenuEventArgs e)
	{
		//if (e.Handled)
		//{
		//	return;
		//}

		//Point ptGlobal = new Point(e.CursorLeft, e.CursorTop);

		//// NOTE: Showing context menu is not considered a crucial operation that requires us to fail
		////       and propagate the result, so ignoring HR in case of failure is the right thing to do.
		//TextContextMenu.Show(
		//	e.UIElement,
		//	ptGlobal,
		//	e.ShowCut,
		//	e.ShowCopy,
		//	e.ShowPaste,
		//	e.ShowUndo,
		//	e.ShowRedo,
		//	e.ShowSelectAll);

		//e.Handled = true;
	}

	private void RegisterManipulationInertiaProcessingEventHandler()
	{
		//m_element.ManipulationInertiaProcessing += ManipulationInertiaProcessingEventListener;
	}

	private void UnregisterManipulationInertiaProcessingEventHandler()
	{
		//m_element.ManipulationInertiaProcessing -= ManipulationInertiaProcessingEventListener;
	}

	private void ManipulationInertiaProcessingEventListener(object sender, EventArgs e)
	{
		//((DependencyObject)sender).GetContext().GetInputServices().ProcessManipulationInertiaInteraction();
	}

	private void RegisterRightTappedEventHandler()
	{
		m_element.RightTapped += RightTappedEventListener;
	}

	private void UnregisterRightTappedEventHandler()
	{
		m_element.RightTapped -= RightTappedEventListener;
	}

	private void RightTappedEventListener(object sender, RightTappedRoutedEventArgs e)
	{
		//if (e.Handled ||
		//	(e.PointerDeviceType != PointerDeviceType.Mouse &&
		//	 e.PointerDeviceType != PointerDeviceType.Pen))
		//{
		//	return;
		//}

		//var contentRoot = VisualTree.GetContentRootForElement(sender as DependencyObject);
		//var xamlRootInspectable = contentRoot.GetOrCreateXamlRoot();
		//FxCallbacks.ApplicationBarService_ProcessToggleApplicationBarsFromMouseRightTapped(xamlRootInspectable);
		//e.Handled = true;
	}
}
