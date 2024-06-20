namespace Uno.UI.Xaml;

internal struct LeaveParams
{
	public bool IsLive;

	public bool IsForKeyboardAccelerator;

	public LeaveParams()
	{
		IsLive = true;
	}

	public LeaveParams(bool isLive)
	{
		IsLive = isLive;
	}
}
