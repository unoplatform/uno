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
}
