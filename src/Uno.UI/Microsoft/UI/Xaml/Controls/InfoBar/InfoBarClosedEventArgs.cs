// MUX reference InfoBar.idl, commit 3125489

namespace Microsoft.UI.Xaml.Controls
{
    public partial class InfoBarClosedEventArgs
    {
		internal InfoBarClosedEventArgs(InfoBarCloseReason reason) =>
			Reason = reason;

		public InfoBarCloseReason Reason { get; }
	}
}
