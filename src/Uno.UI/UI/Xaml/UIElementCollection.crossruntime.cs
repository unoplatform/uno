using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Uno.Extensions;
using System;
using View = Microsoft.UI.Xaml.UIElement;
using Uno.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class UIElementCollection
	{
		private readonly UIElement _owner;

		// CUIElementCollection::m_pUnloadingStorage: removed children kept visually alive by an unload transition.
		private List<View> m_pUnloadingStorage;

		internal UIElementCollection(UIElement view)
		{
			_owner = view;
		}

		private void OnChildEntered(View item)
		{
			if (_owner is Panel panel)
			{
				panel.OnChildEntered(item);
			}
		}

		private bool UnloadElement(View pRemove)
			=> pRemove.m_pLayoutTransitionStorage is not null && Microsoft.UI.Xaml.Media.Animation.Transition.UnloadElement(this, pRemove);

		internal void AddUnloadingElement(View pRemove)
		{
			(m_pUnloadingStorage ??= new()).Add(pRemove);
			_owner.RemoveChildKeepingVisual(pRemove);
		}

		// MUX Reference UIElementCollection.cpp (CUIElementCollection::RemoveUnloadedElement), tag winui3/release/2.5.1, commit ba3a8d59e6d
		internal void RemoveUnloadedElement(View pTarget)
		{
			if (m_pUnloadingStorage is null || !m_pUnloadingStorage.Remove(pTarget))
			{
				return;
			}

			_owner.RemoveKeptVisualChild(pTarget);

			if (pTarget.m_pLayoutTransitionStorage is { m_deferredUnlinkingPayload: { } spPayload } pStorage)
			{
				pStorage.m_deferredUnlinkingPayload = null;
				spPayload.Execute(pTarget);
			}
		}

		// CUIElementCollection::RemoveAllUnloadingChildren
		internal void RemoveAllUnloadingChildren()
		{
			if (m_pUnloadingStorage is { Count: > 0 })
			{
				foreach (var child in m_pUnloadingStorage.ToArray())
				{
					Microsoft.UI.Xaml.Media.Animation.Transition.CancelTransitions(child);
				}
			}
		}

		private void AddCore(View item)
		{
			_owner.AddChild(item);
		}

		private IEnumerable<View> ClearCore()
		{
			var deleted = _owner._children.ToArray();
			_owner.ClearChildren();

			return deleted;
		}

		private bool ContainsCore(View item)
		{
			return _owner._children.Contains(item);
		}

		private void CopyToCore(View[] array, int arrayIndex)
			=> _owner._children.ToArray().CopyTo(array, arrayIndex);


		private int CountCore() => _owner._children.Count;

		private View GetAtIndexCore(int index) => _owner._children[index];

		public List<View>.Enumerator GetEnumerator() => (List<View>.Enumerator)_owner._children.GetEnumerator();

		IEnumerator<UIElement> IEnumerable<UIElement>.GetEnumerator() => GetEnumerator();

		private int IndexOfCore(View item) => _owner._children.IndexOf(item);

		private void InsertCore(int index, View item)
		{
			_owner.AddChild(item, index);
		}

		private void MoveCore(uint oldIndex, uint newIndex)
		{
			_owner.MoveChildTo((int)oldIndex, (int)newIndex);
		}

		// MUX Reference UIElementCollection.cpp: position is the slot to insert before, counted before the removal.
		// Unlike Move, this neither invalidates measure nor raises a collection change.
		internal void MoveInternal(int index, int position)
		{
			var newIndex = index < position ? position - 1 : position;
			if (newIndex != index)
			{
				MoveCore((uint)index, (uint)newIndex);
			}
		}

		private View RemoveAtCore(int index)
		{
			var item = _owner._children.ElementAtOrDefault(index);
			_owner.RemoveChild(item);
			return item;
		}

		private bool RemoveCore(View item)
			=> _owner.RemoveChild(item);

		private View SetAtIndexCore(int index, View value)
			=> _owner.ReplaceChild(index, value);
	}
}
