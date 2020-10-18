#nullable enable

using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Uno.ApplicationModel.Contacts.Internal;
using Uno.Foundation;

namespace Windows.ApplicationModel.Contacts
{
	public partial class ContactPicker
	{
		private const string JsType = "Windows.ApplicationModel.Contacts.ContactPicker";

		private static Task<bool> IsSupportedTaskAsync()
		{
			var isSupportedString = WebAssemblyRuntime.InvokeJS($"{JsType}.isSupported()");
			return Task.FromResult(bool.TryParse(isSupportedString, out var isSupported) && isSupported);
		}

		private async Task<Contact?> PickContactTaskAsync()
		{
			var pickResult = await WebAssemblyRuntime.InvokeAsync($"{JsType}.pickContacts(false)");
			return new Contact() { FirstName = result?.ToString() ?? "N/A" };
		}

		private ContactInfo[] DeserializePickResult(string pickResult)
		{
			using (var stream = new MemoryStream(Encoding.Default.GetBytes(pickResult)))
			{
				var serializer = new DataContractJsonSerializer(typeof(ContactInfo[]));
				return (ContactInfo[])serializer.ReadObject(stream);
			}
		}
	}
}
