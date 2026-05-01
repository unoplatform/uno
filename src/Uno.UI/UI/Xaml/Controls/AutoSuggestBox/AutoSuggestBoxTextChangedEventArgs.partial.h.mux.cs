// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AutoSuggestBoxTextChangedEventArgs_Partial.h, commit 5f9e85113

#nullable disable

using System;

namespace Microsoft.UI.Xaml.Controls
{
	partial class AutoSuggestBoxTextChangedEventArgs
	{
		private AutoSuggestionBoxTextChangeReason m_reason = AutoSuggestionBoxTextChangeReason.ProgrammaticChange;
		private uint m_counter;
		private WeakReference<AutoSuggestBox> m_wkOwner;

		public AutoSuggestBoxTextChangedEventArgs()
		{
		}

		public bool CheckCurrent()
		{
			bool returnValue = false;
			AutoSuggestBox owner = null;

			if (m_wkOwner is not null)
			{
				m_wkOwner.TryGetTarget(out owner);
			}

			if (owner is not null)
			{
				returnValue = owner.GetTextChangedEventCounter() == m_counter;
			}

			return returnValue;
		}

		internal void SetCounter(uint counter)
		{
			m_counter = counter;
		}

		internal void SetOwner(AutoSuggestBox owner)
		{
			m_wkOwner = new WeakReference<AutoSuggestBox>(owner);
		}
	}
}
