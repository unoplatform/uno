#if UNO_HAS_ENHANCED_LIFECYCLE
namespace Uno.UI.Xaml;

internal struct LeaveParams
{
	public bool IsLive;

	public LeaveParams()
	{
		IsLive = true;
	}

	public LeaveParams(bool isLive)
	{
		IsLive = isLive;
	}
}
#endif
