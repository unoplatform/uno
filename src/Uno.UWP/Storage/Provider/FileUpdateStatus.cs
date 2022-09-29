#nullable disable

namespace Windows.Storage.Provider
{
	/// <summary>
	/// Describes the status of a file update request.
	/// </summary>
	public enum FileUpdateStatus
	{
		/// <summary>
		/// The file update was not fully completed and should be retried.
		/// </summary>
		Incomplete,

		/// <summary>
		/// The file update was completed successfully.
		/// </summary>
		Complete,

		/// <summary>
		/// User input (like credentials) is needed to update the file.
		/// </summary>
		UserInputNeeded,

		/// <summary>
		/// The remote version of the file was not updated because the storage location
		/// wasn't available. The file remains valid and subsequent updates
		/// to the file may succeed.
		/// </summary>
		CurrentlyUnavailable,

		/// <summary>
		/// The file is now invalid and can't be updated now or in the future. For example,
		/// this could occur if the remote version of the file was deleted.
		/// </summary>
		Failed,

		/// <summary>
		/// The file update was completed successfully and the file has been renamed.
		/// For example, this could occur if the user chose to save their changes under
		/// a different file name because of conflicting changes made to the remote
		/// version of the file.
		/// </summary>
		CompleteAndRenamed,
	}
}
