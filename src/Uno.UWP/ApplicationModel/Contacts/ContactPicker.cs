#nullable enable

using System;
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
			IsSupportedTaskAsync().AsAsyncOperation();

		/// <summary>
		/// Launches the Contact Picker to select a single contact.
		/// </summary>
		/// <returns>The operation that launches the Contact Picker.</returns>
		/// <remarks>Unsupported platforms only return null.</remarks>
		public IAsyncOperation<Contact?> PickContactAsync() =>
			PickContactTaskAsync().AsAsyncOperation();
	}
}
