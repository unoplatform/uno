#nullable disable

#if HAS_UNO_WINUI

using Uno;

namespace Microsoft.UI.Input
{
	public sealed class InputLightDismissAction : InputObject
	{
#pragma warning disable 67
		public event Windows.Foundation.TypedEventHandler<InputLightDismissAction, InputLightDismissEventArgs> Dismissed;
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
