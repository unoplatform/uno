// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\dxaml\xcp\core\text\common\TextCore.cpp, commit 15af68ea1b6 

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;

namespace Uno.UI.Text;

internal static class TextCore
{
	internal static bool IsTextControl(DependencyObject element) =>
		element is TextBlock ||
		element is RichTextBlock ||
		element is RichTextBlockOverflow ||
		element is RichEditBox ||
		element is TextBox ||
		element is PasswordBox;

	/// <summary>
	/// Determines whether a given control is a text control
	/// </summary>
	internal static bool IsTextSelectionEnabled(DependencyObject textControl)
	{
		if (textControl is TextBlock textBlock)
		{
			return textBlock.IsTextSelectionEnabled;
		}
		else if (textControl is RichTextBlock richTextBlock)
		{
			return richTextBlock.IsTextSelectionEnabled;
		}
		else if (textControl is RichTextBlockOverflow richTextBlockOverflow)
		{
			// Selection is not currently supported for RichTextBlockOverflow; treat as disabled.
			// Future implementation may use: return richTextBlockOverflow.GetMaster().IsSelectionEnabled();
			return false;
		}
		else if (textControl is RichEditBox or TextBox or PasswordBox)
		{
			return true;
		}
		else
		{
			return false;
		}
	}
}
