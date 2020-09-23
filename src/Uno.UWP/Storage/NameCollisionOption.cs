#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously


namespace Windows.Storage
{
	public enum NameCollisionOption
	{
		// Summary:
		//     Automatically generate a unique name by appending a number to the name of
		//     the file or folder.
		GenerateUniqueName = 0,
		//
		// Summary:
		//     Replace the existing file or folder. Your app must have permission to access
		//     the location that contains the existing file or folder. Access to a location
		//     can be granted in several ways, for example, by a capability declared in
		//     your application's manifest, or by the user through the file picker. You
		//     can use Windows.Storage.AccessCache to manage the list of locations that
		//     are accessible to your app via the file picker.
		ReplaceExisting = 1,
		//
		// Summary:
		//     Return an error if another file or folder exists with the same name and abort
		//     the operation.
		FailIfExists = 2,
	}
}
