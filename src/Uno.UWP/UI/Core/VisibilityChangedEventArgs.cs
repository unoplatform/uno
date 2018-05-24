using System;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.UI.Core
{
	public sealed partial class VisibilityChangedEventArgs
	{
		public bool Handled
		{
			get;
			set;
		}
		public bool Visible
		{
			get;
		}
	}
}
