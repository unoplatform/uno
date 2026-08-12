namespace Uno.HotReload;

/// <summary>
/// A metadata reference re-pointed by the manager's baseline-identity pinning before an EnC emit
/// (see <see cref="Utils.RoslynExtensions.WithBaselineReferenceIdentities"/>):
/// <paramref name="ConflictingPath"/> shared the simple name of a baseline reference and was pinned
/// back to <paramref name="BaselinePath"/>, so the emitted delta binds the identity the running
/// application actually loaded. Carried on <see cref="HotReloadUpdate.PinnedReferences"/> so
/// handlers that stage assembly files can avoid overwriting a baseline file with the conflicting
/// same-named one the emit just pinned away.
/// </summary>
public readonly record struct PinnedReference(string ProjectName, string AssemblyName, string ConflictingPath, string BaselinePath);
