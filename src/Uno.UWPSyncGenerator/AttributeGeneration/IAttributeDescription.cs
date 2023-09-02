#nullable enable

using Microsoft.CodeAnalysis;

namespace Uno.UWPSyncGenerator.AttributeGeneration;

internal interface IAttributeDescription
{
	/// <summary>
	/// Given an <see cref="AttributeData"/>, return a string representing the C# code for the attribute. If
	/// the given <paramref name="attributeData"/> can't be handled by this attribute description instance, returns null.
	/// </summary>
	string? TryGenerateCodeFromAttributeData(AttributeData attributeData);
}
