// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	internal partial class UniqueIdElementPool : IEnumerable<KeyValuePair<string, UIElement>>
	{
		private Dictionary<string, UIElement> m_elementMap = new Dictionary<string, UIElement>();
		private readonly ItemsRepeater m_owner;

		public UniqueIdElementPool(ItemsRepeater owner)
		{
			// ItemsRepeater is not fully constructed yet. Don't interact with it.

			m_owner = owner;
		}

#if DEBUG
		public bool IsEmpty => m_elementMap.Count == 0;
#endif

		public void Add(UIElement element)
		{
			global::System.Diagnostics.Debug.Assert(m_owner.ItemsSourceView.HasKeyIndexMapping);

			var virtInfo = ItemsRepeater.GetVirtualizationInfo(element);
			var key = virtInfo.UniqueId;

			if (m_elementMap.ContainsKey(key))
			{
				throw new InvalidOperationException($"The unique id provided ({virtInfo.UniqueId}) is not unique.");
			}

			m_elementMap.Add(key, element);
		}

		public UIElement Remove(int index)
		{
			global::System.Diagnostics.Debug.Assert(m_owner.ItemsSourceView.HasKeyIndexMapping);

			// Check if there is already a element in the mapping and if so, use it.
			UIElement element = null;
			string key = m_owner.ItemsSourceView.KeyFromIndex(index);
			if (m_elementMap.TryGetValue(key, out element))
			{
				m_elementMap.Remove(key);
			}

			return element;
		}

		public void Clear()
		{
			global::System.Diagnostics.Debug.Assert(m_owner.ItemsSourceView.HasKeyIndexMapping);
			m_elementMap.Clear();
		}

		/// <inheritdoc />
		public IEnumerator<KeyValuePair<string, UIElement>> GetEnumerator()
			=> m_elementMap.GetEnumerator();

		/// <inheritdoc />
		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();
	}
}
