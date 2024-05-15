#if HAS_UNO_WINUI && IS_UNO_UI_PROJECT
namespace Microsoft.UI.Input
#else
namespace Windows.UI.Input
#endif
{
	public enum HoldingState
	{
		Started,
		Completed,
		Canceled,
	}
}
