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

	XamlIslandRootCollection GetXamlIslandRootCollection()
	{
		if (!_shutdownInProgress)
		{
			EnsureXamlIslandRootCollection();
		}
		return m_xamlIslandRootCollection;
	}

	private void EnsureFullWindowMediaRoot()
	{
		if (IsMainVisualTree && FullWindowMediaRoot is null)
		{
			FullWindowMediaRoot = new FullWindowMediaRoot();
			Canvas.SetZIndex(FullWindowMediaRoot, FullWindowMediaRootZIndex);
		}
	}

	private void EnsureXamlIslandRootCollection()
	{
		if (IsMainVisualTree && _xamlIslandRootCollection is null)
		{
			CREATEPARAMETERS cp(m_pCoreNoRef);
			IFCFAILFAST(CreateDO(m_xamlIslandRootCollection.ReleaseAndGetAddressOf(), &cp));

			// Protect the root.
			IFCFAILFAST(m_xamlIslandRootCollection->EnsurePeerAndTryPeg());
			IFCFAILFAST(m_xamlIslandRootCollection->EnsureChildrenCollection());
		}
	}
}
