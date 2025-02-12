// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// TextCore.cpp

#nullable enable

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Core
{
	internal class TextCore
	{
		private static readonly Lazy<TextCore> _instance = new Lazy<TextCore>(() => new TextCore());

		private TextCore()
		{
		}

		internal static TextCore Instance => _instance.Value;

		/// <summary>
		/// Determines whether a given dependency object is a text control.
		/// </summary>
		/// <param name="dependencyObject">Dependency object.</param>
		/// <returns>Ture if the object is a text control.</returns>
		internal static bool IsTextControl(DependencyObject? dependencyObject) =>
			dependencyObject is TextBlock ||
			dependencyObject is RichTextBlock ||
			dependencyObject is RichTextBlockOverflow ||
			dependencyObject is RichEditBox ||
			dependencyObject is TextBox ||
			dependencyObject is PasswordBox;

		internal void ClearLastSelectedTextElement()
		{
			//TODO Uno: Implement
		}
	}
}
