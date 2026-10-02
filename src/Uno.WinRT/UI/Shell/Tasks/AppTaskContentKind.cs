namespace Windows.UI.Shell.Tasks;

/// <summary>
/// Internal enum to track what kind of content an <see cref="AppTaskContent"/> represents.
/// </summary>
internal enum AppTaskContentKind
{
	SequenceOfSteps,
	PreviewThumbnail,
	TextSummary,
	GeneratedAssets,

	/// <summary>
	/// The task was created without content, which Windows allows. Content accessors are rejected.
	/// </summary>
	None,
}
