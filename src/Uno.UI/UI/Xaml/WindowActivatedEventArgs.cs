#if HAS_UNO_WINUI
using System;
using Windows.Foundation;
using Windows.Foundation.Metadata;
using Windows.UI.Core;

namespace Microsoft.UI.Xaml
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
#endif
