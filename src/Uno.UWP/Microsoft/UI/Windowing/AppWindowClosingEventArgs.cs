namespace Microsoft.UI.Windowing;

#if HAS_UNO_WINUI
public
#else
internal
#endif
partial class AppWindowClosingEventArgs
{
	internal AppWindowClosingEventArgs()
	{
	}

	public bool Cancel { get; set; }
}
