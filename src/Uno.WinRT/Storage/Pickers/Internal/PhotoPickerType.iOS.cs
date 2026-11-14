#nullable enable

using System.Collections.Generic;

namespace Uno.Storage.Pickers.Internal;

/// <summary>
/// What iOS declares about a type that the item provider of a photo or video picked with <c>PHPickerViewController</c> registers.
/// </summary>
/// <param name="FileExtensions">The file name extensions of the type, without the dot, the preferred one first.</param>
/// <param name="ConformsToData">Whether the type conforms to <c>public.data</c>, so is a file, unlike a package such as a Live Photo bundle.</param>
internal sealed record PhotoPickerType(IReadOnlyList<string> FileExtensions, bool ConformsToData);
