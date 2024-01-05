// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\XamlIsland_Partial.cpp, tag winui3/release/1.4.3, commit 685d2bfa86d6169aa1998a7eaa2c38bfcf9f74bc

#nullable enable

using DirectUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.DataBinding;
using Uno.UI.Xaml.Core;

namespace Uno.UI.Xaml.Islands;

partial class XamlIsland
{
	public XamlIsland()
	{
		_contentManager = new(this, false /* isUwpWindowContent */);

#if HAS_UNO
		// TODO: Uno specific - additional root logic required by Uno.
		_rootElementLogic = new(this);

		Initialize();
#endif
	}

	internal void Initialize()
	{
		// WinUI has two classes - XamlIsland and XamlIslandRoot. Uno has only one.
		// This takes care of the "root" initialization.
		InitializeRoot();
	}

	//internal void Initialize(ContentBridge contentBridge)
	//{
	//	// WinUI has two classes - XamlIsland and XamlIslandRoot. Uno has only one.
	//	// This takes care of the "root" initialization.
	//	InitializeRoot(contentBridge);
	//}

	public UIElement? Content
	{
		get
		{
			// CheckThread();
			return _contentManager.Content;
		}
		set
		{
			//CheckThread()
			_contentManager.Content = value;

			var coreRootScrollViewer = _contentManager.RootScrollViewer;
			// TODO Uno: Support RootScrollViewer
			//if (coreRootScrollViewer is not null)
			//{
			//	var alignmentValue = VerticalAlignment.Top;
			//	coreRootScrollViewer.SetValue(FrameworkElement.VerticalAlignmentProperty, alignmentValue));
			//	// Set the root ScrollViewer explicitly on the ScrollContentControl.
			//	coreRootScrollViewer.SetRootScrollViewer(true);
			//}

			var coreContentPresenter = _contentManager.RootSVContentPresenter;
			var coreContent = value;
			XamlIsland coreXamlIsland = this;

			coreXamlIsland.SetPublicRootVisual(coreContent, coreRootScrollViewer, coreContentPresenter);
		}
	}

	internal static void OnSizeChanged(XamlIsland xamlIsland) =>
		xamlIsland._contentManager.OnWindowSizeChanged();

	internal ScrollViewer? RootScrollViewer => _contentManager.RootScrollViewer;

	internal XamlIsland? GetIslandFromElement(DependencyObject element)
	{
		DXamlCore dxamlCore = DXamlCore.Current;
		CoreServices core = dxamlCore.GetHandle();
		DependencyObject coreElement = element;
		var coreXamlIsland = core.GetRootForElement(coreElement);
		return coreXamlIsland as XamlIsland;
	}

	// internal FocusController FocusController { get; }

	internal void SetOwner(object owner) => _owner = WeakReferencePool.RentWeakReference(this, owner);

	internal bool TryGetOwner(out object? owner)
	{
		owner = null;

		if (_owner is not null && _owner.IsAlive)
		{
			owner = _owner.Target;
			return true;
		}

		return false;
	}
}
