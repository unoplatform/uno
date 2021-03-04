// MUX reference InfoBar.idl, commit 3125489

namespace Microsoft.UI.Xaml.Controls
{
    public partial class InfoBarClosingEventArgs
    {
		internal InfoBarClosingEventArgs(InfoBarCloseReason reason) =>
			Reason = reason;

		public InfoBarCloseReason Reason { get; }

		public bool Cancel { get; set; } = false;
	}
}
