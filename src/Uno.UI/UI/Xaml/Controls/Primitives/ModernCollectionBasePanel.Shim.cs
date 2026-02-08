#nullable enable
using System;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Primitives
{
	// Minimal shim to provide the ModernCollectionBasePanel type expected by WinUI ported code.
	// Methods/properties return safe defaults so callers gracefully fall back to non-optimized logic.
	internal partial class ModernCollectionBasePanel : Panel
	{
		// Cache indices used by automation peers / helpers
		public virtual int FirstCacheIndexBase => -1;
		public virtual int LastCacheIndexBase => -1;

		public virtual int FirstCacheGroupIndexBase => -1;
		public virtual int LastCacheGroupIndexBase => -1;

		// Headers / containers
		public virtual DependencyObject? HeaderFromIndex(int index) => null;
		public virtual DependencyObject? ContainerFromIndex(int index) => null;

		// Indexing helpers
		public virtual int IndexFromContainer(DependencyObject? container) => -1;

		// Group information helper
		public virtual void GetGroupInformationFromItemIndex(int itemIndex, out int groupIndex, out int indexWithinGroup, out int groupCount)
		{
			groupIndex = -1;
			indexWithinGroup = -1;
			groupCount = 0;
		}
	}
}
