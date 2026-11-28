// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\NumberBox\NumberBox.h, commit b8cfb849061c00df624ebb29ac4727b9e58ea99c

using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Disposables;
using Windows.Globalization.NumberFormatting;

namespace Microsoft.UI.Xaml.Controls;

partial class NumberBox
{
	private bool m_valueUpdating = false;
	private bool m_textUpdating = false;

	private SignificantDigitsNumberRounder m_displayRounder = new();

	private TextBox m_textBox;
	private ContentPresenter m_headerPresenter;
	private Popup m_popup;
}
