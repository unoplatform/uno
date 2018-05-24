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
		/// Ceates a new JSObject instance
		/// </summary>
		long CreateNativeInstance(IntPtr managedHandle);

		/// <summary>
		/// Get the replacement javascript code needed to retreive an instance of a JSObject
		/// </summary>
		string GetNativeInstance(IntPtr managedHandle, long jsHandle);

		/// <summary>
		/// Delete an insatnce of the JS Object
		/// </summary>
		void DestroyNativeInstance(IntPtr managedHandle, long jsHandle);

		/// <summary>
		/// Invoke a method on the managed object
		/// </summary>
		object InvokeManaged(object instance, string method, string parameters);
	}
}