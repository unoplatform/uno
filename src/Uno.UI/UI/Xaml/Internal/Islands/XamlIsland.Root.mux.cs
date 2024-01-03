// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\core\core\elements\XamlIslandRoot.cpp, tag winui3/release/1.4.3, commit 685d2bfa86d6169aa1998a7eaa2c38bfcf9f74bc

#nullable enable

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;

namespace Uno.UI.Xaml.Islands;

partial class XamlIsland
{
	private void SetPublicRootVisual(
		UIElement? rootVisual,
		ScrollViewer? rootScrollViewer,
		ContentPresenter? contentPresenter) =>
		_contentRoot.VisualTree.SetPublicRootVisual(rootVisual, rootScrollViewer, contentPresenter);


	internal VisualTree? VisualTree => _contentRoot?.VisualTree;

	internal UIElement? PublicRootVisual => _contentRoot?.VisualTree.PublicRootVisual;
}
