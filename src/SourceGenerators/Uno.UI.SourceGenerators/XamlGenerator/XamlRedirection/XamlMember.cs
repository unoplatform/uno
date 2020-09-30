#nullable enable

extern alias __uno;

using System;
using Uno.Extensions;

namespace Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection
{
	internal class XamlMember : IEquatable<XamlMember>
	{
		private string? _name;
		private XamlType? _declaringType;
		private bool _isAttachable;

		private __uno::Uno.Xaml.XamlMember? _unoMember;

		public static XamlMember? FromMember(__uno::Uno.Xaml.XamlMember? member) => member != null ? new XamlMember(member) : null;

		public static XamlMember? WithDeclaringType(XamlMember member, XamlType declaringType)
		{
			var newMember = FromMember(member._unoMember);
			if (newMember != null)
			{
				newMember._declaringType = declaringType;
			}
			return newMember;
		}

		private XamlMember(__uno::Uno.Xaml.XamlMember member) => this._unoMember = member;

		public XamlMember(string name, XamlType declaringType, bool isAttachable)
		{
			this._name = name;
			this._declaringType = declaringType;
			this._isAttachable = isAttachable;
		}

		public string Name 
			=> (_name?.HasValue() ?? false) ? _name : _unoMember?.Name!;

		public XamlType DeclaringType 
			=> _declaringType != null ? _declaringType : XamlType.FromType(_unoMember?.DeclaringType);

		public XamlType Type
			=> XamlType.FromType(_unoMember?.Type);

		public string? PreferredXamlNamespace 
			=> _unoMember?.PreferredXamlNamespace;

		public bool IsAttachable 
			=> _declaringType != null ? _isAttachable : _unoMember?.IsAttachable ?? false;

		public override string ToString() => _unoMember?.ToString() ?? "";

		public bool Equals(XamlMember? other)
			=> _name.HasValue()
			? (
				other != null
				&& _name == other._name
				&& _declaringType == other._declaringType
				&& _isAttachable == other.IsAttachable
			)
			: _unoMember?.Equals(other?._unoMember) ?? false;

		public override bool Equals(object? other)
			=> other is XamlMember otherMember ? Equals(otherMember) : false;

		public override int GetHashCode()
			=> _name.HasValue()
			? _name?.GetHashCode() ?? 0
			: _unoMember?.GetHashCode() ?? 0;
	}
}
