#nullable enable

using System.ComponentModel;
using Microsoft.UI.Xaml;

namespace Uno.UI;

/// <summary>
/// Builds the content of a <see cref="FrameworkTemplate"/>.
/// </summary>
/// <param name="owner">The owner declared with the template, provided back when the content is materialized.</param>
/// <param name="settings">Materialization settings, carrying the templated parent to assign to the created content.</param>
/// <returns>The root of the materialized content.</returns>
[EditorBrowsable(EditorBrowsableState.Never)]
public delegate UIElement? FrameworkTemplateBuilder(object? owner, TemplateMaterializationSettings settings);
