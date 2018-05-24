using System;
using Windows.Foundation;
using Windows.Foundation.Metadata;
namespace Windows.UI.Core
{
	public sealed partial class CoreWindowEventArgs
	{
		public bool Handled
		{
			get;
			set;
		}
	}
}
