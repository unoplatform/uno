#nullable enable

using System;

namespace Windows.UI.Composition
{
	public partial class VisualCollection : global::Windows.UI.Composition.CompositionObject, global::System.Collections.Generic.IEnumerable<global::Windows.UI.Composition.Visual>
	{
		partial void InsertAbovePartial(Visual newChild, Visual sibling)
		{
			_owner.NativeLayer.InsertSublayerAbove(newChild.NativeLayer, sibling.NativeLayer);
		}

		partial void InsertAtBottomPartial(Visual newChild)
		{
			_owner.NativeLayer.InsertSublayer(newChild.NativeLayer, 0);
		}

		partial void InsertAtTopPartial(Visual newChild)
		{
			_owner.NativeLayer.InsertSublayer(newChild.NativeLayer, _visuals.Count - 1);
		}

		partial void InsertBelowPartial(Visual newChild, Visual sibling)
		{
			_owner.NativeLayer.InsertSublayerBelow(newChild.NativeLayer, sibling.NativeLayer);
		}

		partial void RemoveAllPartial()
		{
			foreach (var visual in _visuals)
			{
				visual.NativeLayer.RemoveFromSuperLayer();
			}
		}

		partial void RemovePartial(Visual child)
		{
			child.NativeLayer.RemoveFromSuperLayer();
		}
	}
}
