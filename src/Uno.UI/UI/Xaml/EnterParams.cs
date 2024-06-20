namespace Uno.UI.Xaml;

internal struct EnterParams
{
	public bool IsLive;

	public bool IsForKeyboardAccelerator;

	public EnterParams()
	{
		IsLive = true;
	}

	public EnterParams(bool isLive)
	{
		IsLive = isLive;
	}
}
