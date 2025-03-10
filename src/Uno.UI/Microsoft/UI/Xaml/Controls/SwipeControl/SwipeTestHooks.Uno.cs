using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	internal partial class SwipeTestHooks
	{
		static SwipeTestHooks()
		{
			s_testHooks = new SwipeTestHooks();
		}
	}
}
