using Uno.UI.Xaml.Core;

namespace Uno.UI.Xaml;

internal struct EnterParams
{
	public bool IsLive;

	public bool IsForKeyboardAccelerator;

	/// <summary>
	/// The visual tree associated with this Enter walk. In WinUI, this is propagated
	/// through the Enter walk and set on each element via SetVisualTree.
	/// In Uno, we use this to help elements that aren't in the visual tree
	/// (e.g., flyout content) find the correct ContentRoot.
	/// </summary>
	public VisualTree VisualTree;

	public EnterParams()
	{
		IsLive = true;
	}

	public EnterParams(bool isLive)
	{
		IsLive = isLive;
	}
}
