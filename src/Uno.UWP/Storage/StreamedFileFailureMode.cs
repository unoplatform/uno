
using Uno;

namespace Windows.Storage
{
	public enum StreamedFileFailureMode 
	{
		Failed,

		[NotImplemented] // We don't have any specific support fo this failure kind, all failures are handled the same way
		CurrentlyUnavailable,

		[NotImplemented] // We don't have any specific support fo this failure kind, all failures are handled the same way
		Incomplete,
	}
}
