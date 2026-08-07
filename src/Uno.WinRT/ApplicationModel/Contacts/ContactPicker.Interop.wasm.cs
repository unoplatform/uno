using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace __Windows.ApplicationModel.Contacts
{
	internal partial class ContactPicker
	{
		internal static partial class NativeMethods
		{
			private const string JsType = "globalThis.Windows.ApplicationModel.Contacts.ContactPicker";

			[JSImport($"{JsType}.isSupported")]
			internal static partial bool IsSupported();

			[JSImport($"{JsType}.pickContacts")]
			internal static partial Task<string> PickContactsAsync(bool multiple);
		}
	}
}
