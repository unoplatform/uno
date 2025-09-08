#if HAS_UNO_WINUI

using Uno;
using Windows.Foundation;

namespace Microsoft.UI.Input
{
	public sealed partial class InputLightDismissAction : InputObject
	{
#pragma warning disable 67
		public event TypedEventHandler<InputLightDismissAction, InputLightDismissEventArgs> Dismissed;
#pragma warning restore 67

		private InputLightDismissAction()
		{
		}

		[NotImplemented]
		public static InputLightDismissAction GetForWindowId(WindowId windowId)
			=> new InputLightDismissAction();
	}
}
#endif
