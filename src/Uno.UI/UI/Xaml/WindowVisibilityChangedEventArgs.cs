#if HAS_UNO_WINUI

namespace Microsoft.UI.Xaml;

public partial class WindowVisibilityChangedEventArgs
{
	public bool Handled { get; set; }
	public bool Visible { get; internal init; }
}
#endif
