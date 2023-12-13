// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\core\inc\Popup.h, PopupRoot_Partial.h, tag winui3/release/1.4.3, commit 685d2bf

using System.Collections.Generic;
using Uno.UI.Xaml.Core;
using Windows.Foundation;

//------------------------------------------------------------------------
//
//  Class:  CPopupRoot
//
//  Synopsis: Used to host all of the popups and the relationships between
//                 the popup child and the popup element for data binding
//
//------------------------------------------------------------------------

namespace Windows.UI.Xaml.Controls.Primitives;

internal partial class PopupRoot : Panel
{
	private PopupRoot(CoreServices pCore) //: CPanel(pCore)
	{
		_openPopups = null;
        _deferredPopups = null;
		_hasThemeChanged = false;
		_isRootHitTestingSuppressed = false;
		_availableSizeAtLastMeasure = new Size(0, 0);
	}

	internal enum PopupFilter
	{
		LightDismissOnly,
        LightDismissOrFlyout,
        All,
    }

	// A list of open and unloading popups. The most recently opened is at the head.
	private LinkedList<Popup> _openPopups;

	// A list of popups that were opened while running layout on open popups
	private PopupVector _deferredPopups;

	private Size _availableSizeAtLastMeasure;

	// Has theme ever changed from startup theme?
	private bool _hasThemeChanged = false;

	// Should we suppress hit-testing of the popup root?
	private bool _isRootHitTestingSuppressed = false;
}
