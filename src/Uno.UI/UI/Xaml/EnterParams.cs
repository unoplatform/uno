#if UNO_HAS_ENHANCED_LIFECYCLE
namespace Uno.UI.Xaml;

internal struct EnterParams
{
	public bool IsLive;

	public EnterParams()
	{
		IsLive = true;
	}

	public EnterParams(bool isLive)
	{
		IsLive = isLive;
	}
}
#endif
