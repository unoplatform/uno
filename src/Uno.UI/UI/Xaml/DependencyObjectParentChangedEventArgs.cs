using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml
{
	/// <summary>
	/// A event arg used when the parent of a DependencyObject changes.
	/// </summary>
	internal class DependencyObjectParentChangedEventArgs : EventArgs
	{
		public DependencyObjectParentChangedEventArgs(object previousParent, object newParent)
		{
			PreviousParent = previousParent;
			NewParent = newParent;
		}

		public object NewParent { get; }
		public object PreviousParent { get; }
	}
}
