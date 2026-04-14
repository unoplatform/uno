using Uno.UI.Xaml.Core;

namespace Uno.UI.Xaml;

internal struct LeaveParams
{
	public bool IsLive;

	public bool IsForKeyboardAccelerator;

	/// <summary>
	/// The visual tree associated with this Leave walk.
	/// </summary>
	public VisualTree VisualTree;

	public LeaveParams()
	{
		IsLive = true;
	}

	public LeaveParams(bool isLive)
	{
		IsLive = isLive;
	}
}
