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

		private __uno::Uno.Xaml.XamlType _unoDeclaringType;

		public static XamlType FromType(__uno::Uno.Xaml.XamlType declaringType) => declaringType != null ? new XamlType(declaringType) : null;

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
			=> _isUnknown ? unknownTypeName : _unoDeclaringType.Name;

		public string PreferredXamlNamespace
			=> _isUnknown ? unknownTypeNamespace : _unoDeclaringType.PreferredXamlNamespace;

		public override string ToString() => _unoDeclaringType.ToString();

		public bool Equals(XamlType other) => _isUnknown
			? false
			: _unoDeclaringType.Equals(other?._unoDeclaringType);

		public override bool Equals(object other)
			=> other is XamlType otherType ? Equals(otherType) : false;

		public override int GetHashCode()
			=> _isUnknown
			? unknownTypeName.GetHashCode()
			: _unoDeclaringType.Name.GetHashCode();

	}
}
