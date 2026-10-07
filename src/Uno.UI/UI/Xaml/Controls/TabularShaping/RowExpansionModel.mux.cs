// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TabularShaping\RowExpansionModel.cpp, tag winui3/main, commit dc28206ea35

#nullable enable

using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls.Tabular;

internal static partial class ShapingHelpers
{
	internal sealed partial class RowExpansionModel
	{
		private partial void SetDefaultExpanded(bool expanded)
		{
			if (m_defaultExpanded == expanded && m_nonDefault.Count == 0)
			{
				return;
			}

			// Whether any key's resolved state actually moves depends on the exceptions: with none,
			// only a real default change matters. Decide before mutating, so the notification
			// describes what happened rather than what was requested.
			bool defaultMoved = m_defaultExpanded != expanded;
			bool hadExceptions = m_nonDefault.Count != 0;

			m_defaultExpanded = expanded;
			m_nonDefault.Clear();

			if (!defaultMoved && !hadExceptions)
			{
				return;
			}

			Change change = new();
			change.AffectsAllKeys = true;
			change.IsExpanded = expanded;
			RaiseChanged(change);
		}

		public partial bool IsExpanded(string key)
		{
			bool defaultExpanded = m_defaultExpanded;
			if (string.IsNullOrEmpty(key))
			{
				// An empty key cannot be stored as an exception (nothing could ever clear it
				// selectively), so it always reads as the default rather than silently sharing one
				// bucket with every other unkeyed group.
				return defaultExpanded;
			}

			return m_nonDefault.Contains(key) ? !defaultExpanded : defaultExpanded;
		}

		public partial void SetExpanded(string key, bool isExpanded)
		{
			if (string.IsNullOrEmpty(key) || IsExpanded(key) == isExpanded)
			{
				return;
			}

			bool defaultExpanded = m_defaultExpanded;
			if (isExpanded == defaultExpanded)
			{
				// Back to the baseline: drop the exception rather than record agreement with it.
				// Recording it would let the set grow by one entry per toggle cycle.
				m_nonDefault.Remove(key);
			}
			else
			{
				m_nonDefault.Add(key);
			}

			Change change = new();
			change.Keys.Add(key);
			change.IsExpanded = isExpanded;
			RaiseChanged(change);
		}

		public partial void SetAllExpanded(bool isExpanded) => SetDefaultExpanded(isExpanded);

		public partial void RetainOnly(HashSet<string> liveKeys) => m_nonDefault.RemoveWhere(key => !liveKeys.Contains(key));

		private partial void RaiseChanged(Change change)
		{
			if (m_changed is not null)
			{
				m_changed(change);
			}
		}
	}
}
