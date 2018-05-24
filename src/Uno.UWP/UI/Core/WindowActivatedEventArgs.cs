using System;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.UI.Core
{
	public sealed partial class WindowActivatedEventArgs
	{
		internal WindowActivatedEventArgs(CoreWindowActivationState windowActivationState)
		{
			WindowActivationState = windowActivationState;
		}

		public bool Handled
		{
			get;
			set;
		}

		public CoreWindowActivationState WindowActivationState
		{
			get;
		}
	}
}
