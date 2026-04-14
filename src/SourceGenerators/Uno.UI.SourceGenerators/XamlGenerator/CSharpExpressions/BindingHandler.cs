#nullable enable

namespace Uno.UI.SourceGenerators.XamlGenerator.CSharpExpressions;

/// <summary>
/// INPC subscription tuple describing one leaf of the refresh set. See data-model.md §7.
/// </summary>
internal sealed record BindingHandler(string Accessor, string PropertyName);
