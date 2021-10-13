using System;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	/// <summary>
	/// Provides event data for RefreshRequested events.
	/// </summary>
	public sealed partial class RefreshRequestedEventArgs
    {
		private readonly Deferral _deferral;

		internal RefreshRequestedEventArgs(Deferral deferral)
		{
			_deferral = deferral;
		}

		public Deferral GetDeferral() => _deferral;
	}
}
