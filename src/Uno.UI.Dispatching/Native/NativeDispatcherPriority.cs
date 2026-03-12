namespace Uno.UI.Dispatching
{
	internal enum NativeDispatcherPriority
	{
		High = 0,
		Normal = 1,
		Render = 2,   // Internal only — below Normal, matches WPF/Avalonia ordering
		Low = 3,
		Idle = 4
	}
}
