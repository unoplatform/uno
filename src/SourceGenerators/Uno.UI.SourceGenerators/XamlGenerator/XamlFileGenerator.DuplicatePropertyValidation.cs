#nullable enable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.XamlGenerator;

internal partial class XamlFileGenerator
{
	/// <summary>
	/// Members that have been detected as duplicate property assignments. Skipped during code
	/// generation to avoid emitting C# that would itself fail to compile (CS1912 duplicate
	/// member initialization), so the user only sees the meaningful UXAML0001 error.
	/// </summary>
	private readonly HashSet<XamlMemberDefinition> _duplicatePropertyMembers = new();

	/// <summary>
	/// Validates that no property is set more than once on any element of the given XAML tree.
	/// </summary>
	/// <remarks>
	/// Mirrors WinUI behavior (<c>AG_E_PARSER2_OW_DUPLICATE_MEMBER</c>): "The property 'X' is set
	/// more than once" is raised whether the duplicate comes from attribute syntax,
	/// property-element syntax, or implicit child content (because the [ContentProperty] is
	/// resolved to the same property name as an explicit member).
	/// </remarks>
	private void ValidateNoDuplicateProperties(XamlObjectDefinition root)
	{
		ValidateDuplicatePropertiesOnObject(root);

		foreach (var child in root.Objects)
		{
			ValidateNoDuplicateProperties(child);
		}

		foreach (var member in root.Members)
		{
			foreach (var memberObject in member.Objects)
			{
				ValidateNoDuplicateProperties(memberObject);
			}
		}
	}

	private void ValidateDuplicatePropertiesOnObject(XamlObjectDefinition obj)
	{
		// Skip markup-extension references that have no real "property" semantics.
		if (obj.Type.Name is "StaticResource" or "ThemeResource" or "CustomResource")
		{
			return;
		}

		// Skip types where _UnknownContent is intentionally a list of items / a visual tree
		// rather than a single property assignment.
		if (obj.Type.Name is "ResourceDictionary" or "DataTemplate" or "ItemsPanelTemplate" or "ControlTemplate")
		{
			return;
		}

		var type = FindType(obj.Type);
		if (type is null)
		{
			return;
		}

		var contentProperty = FindContentProperty(type);

		var seenMembers = new Dictionary<string, XamlMemberDefinition>(System.StringComparer.Ordinal);

		foreach (var member in obj.Members)
		{
			var memberName = member.Member.Name;

			if (memberName is "_Initialization" or "_PositionalParameters")
			{
				continue;
			}

			// XAML directives (x:Key, x:Name, x:Uid, x:Class, etc.) live in the XAML language namespace.
			// Synthetic members like "_UnknownContent" also report that namespace from the parser, so we
			// must not skip them here — only true directives whose names are recognized as such.
			if (member.Member.PreferredXamlNamespace == XamlConstants.XamlXmlNamespace
				&& memberName != XamlConstants.UnknownContent)
			{
				continue;
			}

			// Attached properties target a different declaring type and cannot collide
			// with a property on the owning object.
			if (IsAttachedProperty(member))
			{
				continue;
			}

			var effectiveName = GetEffectivePropertyName(member, contentProperty);
			if (effectiveName is null)
			{
				continue;
			}

			if (seenMembers.ContainsKey(effectiveName))
			{
				AddError($"The property '{effectiveName}' is set more than once", member);
				_duplicatePropertyMembers.Add(member);
			}
			else
			{
				seenMembers[effectiveName] = member;
			}
		}
	}

	private static string? GetEffectivePropertyName(
		XamlMemberDefinition member,
		IPropertySymbol? contentProperty)
	{
		if (member.Member.Name != XamlConstants.UnknownContent)
		{
			return member.Member.Name;
		}

		// Whitespace-only literal content is not a real property assignment — skip it.
		if (IsEffectivelyEmptyImplicitContent(member))
		{
			return null;
		}

		// _UnknownContent maps to the type's [ContentProperty]. Every type that supports
		// implicit content (TextBlock/Span/Run/Border/SolidColorBrush/RowDefinition/
		// ColumnDefinition/ContentControl/Page/etc.) declares this attribute so a single
		// generic mapping is sufficient.
		return contentProperty?.Name;
	}

	private static bool IsEffectivelyEmptyImplicitContent(XamlMemberDefinition member)
	{
		// Direct-value case: empty/whitespace string and no child objects.
		if (member.Objects.Count == 0)
		{
			return member.Value is null
				|| (member.Value is string s && string.IsNullOrWhiteSpace(s));
		}

		// Wrapped-text case: for TextBlock/Span/Run/Bold/Italic/Underline/Hyperlink/Paragraph
		// the parser converts literal text inside _UnknownContent into Run objects with the
		// text in their Text member. If every wrapped Run holds only whitespace, treat the
		// member as empty so that whitespace surrounding a property-element does not get
		// interpreted as a duplicate Inlines/Text assignment.
		foreach (var obj in member.Objects)
		{
			if (obj.Type.Name != "Run")
			{
				return false;
			}

			var textMember = obj.Members.FirstOrDefault(m => m.Member.Name == "Text");
			if (textMember?.Value is not string runText || !string.IsNullOrWhiteSpace(runText))
			{
				return false;
			}
		}

		return true;
	}
}
