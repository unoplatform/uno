using System.Runtime.InteropServices.JavaScript;

namespace __Windows.UI.Notifications
{
	internal partial class BadgeUpdater
	{
		internal static partial class NativeMethods
		{
			private const string JsType = "globalThis.Windows.UI.Notifications.BadgeUpdater";

			[JSImport($"{JsType}.clear")]
			internal static partial void Clear();

			[JSImport($"{JsType}.setNumber")]
			internal static partial void SetNumber(int number);
		}
	}
}
