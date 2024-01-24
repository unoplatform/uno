// MUX Reference Windows Kits\10\Include\10.0.22621.0\um\appmodel.h

using System.Threading;
using Uno.UI.Xaml.Core;

namespace Uno.UI.Xaml.Internal;

internal static partial class AppModel
{
	internal static AppPolicyWindowingModel AppPolicyGetWindowingModel(Thread _) =>
#if HAS_UNO_WINUI
		AppPolicyWindowingModel.ClassicDesktop;
#else
		AppPolicyWindowingModel.Universal;
#endif
}
