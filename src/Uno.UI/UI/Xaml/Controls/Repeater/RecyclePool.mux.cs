// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RecyclePool.cpp, commit 4b206bce3

using System;
using System.Collections.Generic;

namespace Microsoft.UI.Xaml.Controls;

partial class RecyclePool
{
	// #pragma region IRecyclePool

	public void PutElement(
		UIElement element,
		string key)
	{
		PutElementCore(element, key, null /* owner */);
	}

	public void PutElement(
		UIElement element,
		string key,
		UIElement owner)
	{
		PutElementCore(element, key, owner);
	}

	public UIElement TryGetElement(
		string key)
	{
		return TryGetElementCore(key, null /* owner */);
	}

	public UIElement TryGetElement(
		string key,
		UIElement owner)
	{
		return TryGetElementCore(key, owner);
	}

	// #pragma endregion

	// #pragma region IRecyclePoolOverrides

	protected virtual void PutElementCore(
		UIElement element,
		string key,
		UIElement owner)
	{
		var winrtKey = key;
		var winrtOwner = owner;
		var winrtOwnerAsPanel = EnsureOwnerIsPanelOrNull(winrtOwner);

		var elementInfo = new ElementInfo(element, winrtOwnerAsPanel);

		if (m_elements.TryGetValue(winrtKey, out var infos))
		{
			infos.Add(elementInfo);
		}
		else
		{
			var pool = new List<ElementInfo>();
			pool.Add(elementInfo);
			m_elements.Add(winrtKey, pool);
		}
	}

	protected virtual UIElement TryGetElementCore(
		string key,
		UIElement owner)
	{
		if (m_elements.TryGetValue(key, out var elements))
		{
			if (elements.Count > 0)
			{
				ElementInfo elementInfo = default;
				// Prefer an element from the same owner or with no owner so that we don't incur
				// the enter/leave cost during recycling.
				// TODO: prioritize elements with the same owner to those without an owner.
				var winrtOwner = owner;
				int iter = -1;
				for (int i = 0; i < elements.Count; i++)
				{
					var elemInfo = elements[i];
					if (elemInfo.Owner == winrtOwner || elemInfo.Owner == null)
					{
						iter = i;
						break;
					}
				}

				if (iter >= 0)
				{
					elementInfo = elements[iter];
					elements.RemoveAt(iter);
				}
				else
				{
					elementInfo = elements[elements.Count - 1];
					elements.RemoveAt(elements.Count - 1);
				}

				var ownerAsPanel = EnsureOwnerIsPanelOrNull(winrtOwner);
				if (elementInfo.Owner != null && elementInfo.Owner != ownerAsPanel)
				{
					// Element is still under its parent. remove it from its parent.
					var panel = elementInfo.Owner;
					if (panel != null)
					{
						int childIndex = panel.Children.IndexOf(elementInfo.Element);
						if (childIndex < 0)
						{
							throw new InvalidOperationException("ItemsRepeater's child not found in its Children collection.");
						}

						panel.Children.RemoveAt(childIndex);
					}
				}

				return elementInfo.Element;
			}
		}

		return null;
	}


	// #pragma endregion

	private IPanel EnsureOwnerIsPanelOrNull(UIElement owner)
	{
		IPanel ownerAsPanel = null;
		if (owner != null)
		{
			// Uno specific: WinUI casts to winrt::Panel directly. Uno's ItemsRecycler is an IPanel
			// implementation that is not a Panel subclass, so we cast to the IPanel interface instead.
			ownerAsPanel = owner as IPanel;
			if (ownerAsPanel == null)
			{
				throw new InvalidOperationException("owner must to be a Panel or null.");
			}
		}

		return ownerAsPanel;
	}

#if HAS_UNO
	// Uno specific: Helper used by the template updater (Uno.UI.TemplateManager) to flush the pool
	// when the DataTemplate is replaced. Not part of the WinUI API surface.
	internal void Clear()
	{
		m_elements.Clear();
	}
#endif
}
