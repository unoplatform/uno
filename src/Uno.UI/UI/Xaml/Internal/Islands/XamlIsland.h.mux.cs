// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml\xcp\dxaml\lib\XamlIsland_Partial.h, tag winui3/release/1.4.3, commit 685d2bfa86d6169aa1998a7eaa2c38bfcf9f74bc

#nullable enable

// XamlIsland adds multiple UI tree support to Xaml
//
// Layout:
//
//     Single layout pass for all XamlIslands
//
// Rendering:
//
//     Per-Island render pass, using separate render target
//
// Contraints:
//
//     Layout (Measure and Arrange)
//         - Size contraint on each CXamlIslandRoot
//     Render
//         - Per-island 'Windowless' WindowsPresentTarget, sized per island
//
// New DCompTreeHost composition tree associations:
//
//     XamlIslandRenderData : Maps CRootVisual to per-Island render data
//     - Shared target created from host's interop compositor (Root comp node is rooted here)
//     - HANDLE to shared target
//     - WindowsPresentTarget: dimensions, rotation, DComp synchronized commit handle
//
// Composition Trees:
//
//     One tree per XamlIsland, rooted to shared target created from host's
//     interop compositor. See DCompTreeHost's XamlIslandRenderData.
//
//
// Shared WUC Visual Tree (per XamlIsland):
//
//           Host's Visual
//                |
//           Shared Target
//                |
//            XamlIslandRoot- - - - - - - - - - - -
//           /    |      \                        |
//          /     |       \                       |
//         /      |        \                      |
// MainVisual PopupVisual  TransitionVisuals    Other Visual roots

using Uno.UI.DataBinding;
using Uno.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Islands;

partial class XamlIsland
{
	private readonly ContentManager _contentManager;
	private ManagedWeakReference? _owner;
}
