extern alias __ms;
extern alias __uno;

using System;
using System.Collections.Generic;

namespace Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection
{
	internal class XamlType : IEquatable<XamlType>
	{
		private string unknownTypeNamespace;
		private string unknownTypeName;
		private List<XamlType> list;
		private XamlSchemaContext xamlSchemaContext;
		private bool _isUnknown;

		private __ms::System.Xaml.XamlType _msDeclaringType;
		private __uno::Uno.Xaml.XamlType _unoDeclaringType;

		public static XamlType FromType(__ms::System.Xaml.XamlType declaringType) => declaringType != null ? new XamlType(declaringType) : null;
		public static XamlType FromType(__uno::Uno.Xaml.XamlType declaringType) => declaringType != null ? new XamlType(declaringType) : null;

		private XamlType(__ms::System.Xaml.XamlType declaringType) => this._msDeclaringType = declaringType;
		private XamlType(__uno::Uno.Xaml.XamlType declaringType) => this._unoDeclaringType = declaringType;

		public XamlType(string unknownTypeNamespace, string unknownTypeName, List<XamlType> list, XamlSchemaContext xamlSchemaContext)
		{
			this.unknownTypeNamespace = unknownTypeNamespace;
			this.unknownTypeName = unknownTypeName;
			this.list = list;
			this.xamlSchemaContext = xamlSchemaContext;
			_isUnknown = true;
		}

		public string Name
			=> _isUnknown ? unknownTypeName : XamlConfig.IsUnoXaml ? _unoDeclaringType.Name : _msDeclaringType.Name;

		public string PreferredXamlNamespace
			=> _isUnknown ? unknownTypeNamespace : XamlConfig.IsUnoXaml ? _unoDeclaringType.PreferredXamlNamespace : _msDeclaringType.PreferredXamlNamespace;

		public override string ToString() => XamlConfig.IsUnoXaml ? _unoDeclaringType.ToString() : _msDeclaringType.ToString();

		public bool Equals(XamlType other) => _isUnknown
			? false
			: XamlConfig.IsUnoXaml
				? _unoDeclaringType.Equals(other?._unoDeclaringType)
				: _msDeclaringType.Equals(other?._msDeclaringType);

		public override bool Equals(object other)
			=> other is XamlType otherType ? Equals(otherType) : false;

		public override int GetHashCode()
			=> _isUnknown
			? unknownTypeName.GetHashCode()
			: XamlConfig.IsUnoXaml
				? _unoDeclaringType.Name.GetHashCode()
				: _msDeclaringType.GetHashCode();

	}
}
