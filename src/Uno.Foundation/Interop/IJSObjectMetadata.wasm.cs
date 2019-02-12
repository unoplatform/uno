using System;
using System.Linq;

namespace Uno.Foundation.Interop
{
	/// <summary>
	/// Type metadata for a JS object
	/// </summary>
	public interface IJSObjectMetadata
	{
		/// <summary>
		/// Creates a new JSObject instance
		/// </summary>
		long CreateNativeInstance(IntPtr managedHandle);

		/// <summary>
		/// Get the replacement javascript code needed to retrieve an instance of a JSObject
		/// </summary>
		string GetNativeInstance(IntPtr managedHandle, long jsHandle);

		/// <summary>
		/// Delete an instance of the JS Object
		/// </summary>
		void DestroyNativeInstance(IntPtr managedHandle, long jsHandle);

		/// <summary>
		/// Invoke a method on the managed object
		/// </summary>
		object InvokeManaged(object instance, string method, string parameters);
	}
}
