#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
#if DEBUG
	[DebuggerDisplay("Type: {_type.Name}")]
#endif
	internal class XamlObjectDefinition : IXamlLocation
	{
		private XamlType _type;

		public XamlObjectDefinition(XamlType type, int lineNumber, int linePosition, XamlObjectDefinition? owner, List<NamespaceDeclaration>? namespaces)
		{
			LineNumber = lineNumber;
			LinePosition = linePosition;
			_type = type;
			Owner = owner;
			Members = new List<XamlMemberDefinition>();
			Objects = new List<XamlObjectDefinition>();
			Namespaces = namespaces;
		}

		public XamlType Type { get { return _type; } }

		public List<XamlMemberDefinition> Members { get; private set; }

		public List<XamlObjectDefinition> Objects { get; private set; }

		public object? Value { get; set; }

		public int LineNumber { get; private set; }

		public int LinePosition { get; set; }

		public XamlObjectDefinition? Owner { get; }

		public List<NamespaceDeclaration>? Namespaces { get; }


		private string? _key;
		public string Key => _key ??= BuildKey();

		private string BuildKey()
		{
			var owner = Owner;
			if (owner is null)
			{
				return $"__{NamingHelper.GetShortName(Type.Name)}";
			}

			var ownerMember = owner
				.Members
				.Select(member => (value: member, idx: member.Objects.IndexOf(this)))
				.FirstOrDefault(member => member.idx >= 0);
			if (ownerMember is { value: not null })
			{
				return $"{ownerMember.value.Key}Ͱ{ownerMember.idx}_{NamingHelper.GetShortName(Type.Name)}";
			}

			var ownerObjectIdx = owner.Objects.IndexOf(this);
			if (ownerObjectIdx >= 0)
			{
				return $"{owner.Key}ϟ{ownerObjectIdx}_{NamingHelper.GetShortName(Type.Name)}";
			}

			return $"{owner.Key}_ø_{NamingHelper.GetShortName(Type.Name)}"; // Should not happen

			
		}
	}

	public class NamingHelper
	{
		/// <summary>
		/// Gets a short name for the given XAML type name, eg.
		/// * ListView => LiVi
		/// * MyLongControlName123 => MyLoCoNa123
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static string GetShortName(string name)
		{
			var sb = new StringBuilder();
			var i = 0;
			var len = name?.Length ?? 0;
			var allowLower = true;

			while (i < len)
			{
				if (char.IsLetterOrDigit(name, i))
				{
					var c = name![i];
					if (char.IsUpper(c) || char.IsDigit(c))
					{
						allowLower = true;
						sb.Append(c);
					}
					else if (allowLower)
					{
						allowLower = false;
						sb.Append(c);
					}
				}
				else
				{
					allowLower = true; // We ignore the char, but if's an _ we allow lower next time
					i++;
				}

				i++;
			}

			return sb.ToString();
		}
	}
}
