// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\controls\dev\NumberBox\NumberBox.h, tag winui3/release/1.7.1, commit 5f27a786ac9

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
