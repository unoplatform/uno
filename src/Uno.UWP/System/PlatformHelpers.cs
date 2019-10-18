using Uno;
using NotImplementedException = System.NotImplementedException;

namespace Windows.System
{
	internal static class PlatformHelpers
	{
		[NotImplemented]
		public static VirtualKeyModifiers GetKeyboardModifiers()
		{
			return VirtualKeyModifiers.None;
		}
	}
}
