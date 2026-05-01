// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AutoSuggestBoxTextChangedEventArgs.idl, commit 5f9e85113

namespace Microsoft.UI.Xaml.Controls
{
	partial class AutoSuggestBoxTextChangedEventArgs
	{
		/// <summary>
		/// Gets a value that indicates the reason the text in an AutoSuggestBox was changed.
		/// </summary>
		public AutoSuggestionBoxTextChangeReason Reason
		{
			get => m_reason;
			set => m_reason = value;
		}
	}
}
