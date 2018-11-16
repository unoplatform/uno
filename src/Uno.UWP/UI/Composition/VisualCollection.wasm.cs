using System;

namespace Windows.UI.Composition
{
	public partial class VisualCollection : global::Windows.UI.Composition.CompositionObject, global::System.Collections.Generic.IEnumerable<global::Windows.UI.Composition.Visual>
	{
		private readonly Visual _owner;

		public VisualCollection(Visual owner)
		{
			_owner = owner;
		}

		partial void InsertAbovePartial(Visual newChild, Visual sibling)
		{
		}

		partial void InsertAtBottomPartial(Visual newChild)
		{
		}

		partial void InsertAtTopPartial(Visual newChild)
		{
		}

		partial void InsertBelowPartial(Visual newChild, Visual sibling)
		{
		}

		partial void RemoveAllPartial()
		{
		}

		partial void RemovePartial(Visual child)
		{
		}
	}
}
