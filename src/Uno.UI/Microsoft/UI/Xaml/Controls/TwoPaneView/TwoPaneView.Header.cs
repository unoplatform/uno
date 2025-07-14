// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference TwoPaneView.h, tag winui3/release/1.4.2

#nullable enable

using Uno.Disposables;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

public partial class TwoPaneView
{
	private const double c_defaultMinWideModeWidth = 641.0;
	private const double c_defaultMinTallModeHeight = 641.0;

	private static readonly GridLength c_pane1LengthDefault = new GridLength(1, GridUnitType.Auto);
	private static readonly GridLength c_pane2LengthDefault = new GridLength(1, GridUnitType.Star);

	private enum ViewMode
	{
		Pane1Only,
		Pane2Only,
		LeftRight,
		RightLeft,
		TopBottom,
		BottomTop,
		None
	}

	private ViewMode m_currentMode = ViewMode.None;

	private bool m_loaded = false;

	private readonly SerialDisposable m_pane1LoadedRevoker = new();
	private readonly SerialDisposable m_pane2LoadedRevoker = new();

	private ColumnDefinition? m_columnLeft = null;
	private ColumnDefinition? m_columnMiddle = null;
	private ColumnDefinition? m_columnRight = null;
	private RowDefinition? m_rowTop = null;
	private RowDefinition? m_rowMiddle = null;
	private RowDefinition? m_rowBottom = null;
	private readonly SerialDisposable m_xamlRootChangedRevoker = new();
}
