extern alias __ms;
extern alias __uno;

using System;
using Uno.Extensions;

namespace Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection
{
	internal class XamlMember : IEquatable<XamlMember>
	{
		private readonly string _name;
		private XamlType _declaringType;
		private readonly bool _isAttachable;

		private readonly __uno::Uno.Xaml.XamlMember _unoMember;
		private readonly __ms::System.Xaml.XamlMember _msMember;
		public static XamlMember FromMember(__uno::Uno.Xaml.XamlMember member) => member != null ? new XamlMember(member) : null;
		public static XamlMember FromMember(__ms::System.Xaml.XamlMember member) => member != null ? new XamlMember(member) : null;

		public static XamlMember WithDeclaringType(XamlMember member, XamlType declaringType)
		{
			var newMember = XamlConfig.IsUnoXaml ? FromMember(member._unoMember) : FromMember(member._msMember);
			newMember._declaringType = declaringType;
			return newMember;
		}

		private XamlMember(__uno::Uno.Xaml.XamlMember member) => this._unoMember = member;
		private XamlMember(__ms::System.Xaml.XamlMember member) => this._msMember = member;

		public XamlMember(string name, XamlType declaringType, bool isAttachable)
		{
			this._name = name;
			this._declaringType = declaringType;
			this._isAttachable = isAttachable;
		}

		public string Name 
			=> _name.HasValue() ? _name : XamlConfig.IsUnoXaml ? _unoMember.Name : _msMember.Name;

		public XamlType DeclaringType 
			=> _declaringType != null ? _declaringType : XamlConfig.IsUnoXaml ? XamlType.FromType(_unoMember.DeclaringType) : XamlType.FromType(_msMember.DeclaringType);

		public XamlType Type
			=> XamlConfig.IsUnoXaml ? XamlType.FromType(_unoMember?.Type) : XamlType.FromType(_msMember?.Type);

		public string PreferredXamlNamespace 
			=> XamlConfig.IsUnoXaml ? _unoMember.PreferredXamlNamespace : _msMember.PreferredXamlNamespace;

		public bool IsAttachable 
			=> _declaringType != null ? _isAttachable : XamlConfig.IsUnoXaml ? _unoMember.IsAttachable : _msMember.IsAttachable;

		public override string ToString() => XamlConfig.IsUnoXaml ? _unoMember.ToString() : _msMember.ToString();

		public bool Equals(XamlMember other)
			=> _name.HasValue()
			? (
				other != null
				&& _name == other._name
				&& _declaringType == other._declaringType
				&& _isAttachable == other.IsAttachable
			)
			: XamlConfig.IsUnoXaml ? _unoMember.Equals(other?._unoMember) : _msMember.Equals(other?._msMember);

		public override bool Equals(object other)
			=> other is XamlMember otherMember ? Equals(otherMember) : false;

		public override int GetHashCode()
			=> _name.HasValue()
			? _name.GetHashCode()
			: XamlConfig.IsUnoXaml
				? _unoMember.GetHashCode()
				: _msMember.GetHashCode();
	}
}
