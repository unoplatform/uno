using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.UI.Core
{
	public sealed partial class BackRequestedEventArgs
	{
		internal BackRequestedEventArgs()
		{
		}

		public bool Handled { get; set; }
	}
}
