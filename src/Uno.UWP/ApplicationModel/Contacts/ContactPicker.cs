#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace Windows.ApplicationModel.Contacts
{
	/// <summary>
	/// Controls how the Contact Picker user interface opens and what information it shows.
	/// </summary>
	public partial class ContactPicker
	{
		/// <summary>
		/// Gets a Boolean value indicating if the contact picker is supported on the current platform.
		/// </summary>
		/// <returns>A Boolean value indicating if the contact picker is supported on the current platform.</returns>
		/// <remarks>Unsupported platforms return false.</remarks>
		public static IAsyncOperation<bool> IsSupportedAsync() =>
			AsyncOperation.FromTask(token => IsSupportedTaskAsync(token));

		/// <summary>
		/// Launches the Contact Picker to select a single contact.
		/// </summary>
		/// <returns>The operation that launches the Contact Picker.</returns>
		/// <remarks>Unsupported platforms only return null.</remarks>
#pragma warning disable CA1822 // Mark members as static
		public IAsyncOperation<Contact?> PickContactAsync() =>
			AsyncOperation.FromTask(token => PickContactTaskAsync(token));

		/// <summary>
		/// Launches the Contact Picker for selecting multiple contacts.
		/// </summary>
		/// <returns>The operation that launches the contact picker.</returns>
		/// <remarks>Unsupported platforms only return empty array.</remarks>
		public IAsyncOperation<IList<Contact>> PickContactsAsync() =>
			AsyncOperation.FromTask(token => PickContactsTaskAsync(token));
#pragma warning restore CA1822 // Mark members as static

		private static async Task<Contact?> PickContactTaskAsync(CancellationToken token)
		{
			var contacts = await PickContactsAsync(false, token);
			return contacts.FirstOrDefault();
		}

		private static async Task<IList<Contact>> PickContactsTaskAsync(CancellationToken token) =>
			await PickContactsAsync(true, token);
	}
}
