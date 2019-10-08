//
// Copyright (C) 2010 Novell Inc. http://novell.com
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NON-INFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Uno.Xaml.Schema;
using System.Windows.Markup;

[assembly:XmlnsDefinition (Uno.Xaml.XamlLanguage.Xaml2006Namespace, "System.Windows.Markup")] // FIXME: verify.

namespace Uno.Xaml
{
	public static class XamlLanguage
	{
		public const string Xaml2006Namespace = "http://schemas.microsoft.com/winfx/2006/xaml";
		public const string Xml1998Namespace = "http://www.w3.org/XML/1998/namespace";
		internal const string Xmlns2000Namespace = "http://www.w3.org/2000/xmlns/";
		internal const string XmlnsMcNamespace = "http://schemas.openxmlformats.org/markup-compatibility/2006";

		// FIXME: I'm not sure if these "special names" should be resolved like this. I couldn't find any rule so far.
		internal static readonly SpecialTypeNameList SpecialNames;

		internal class SpecialTypeNameList : List<SpecialTypeName>
		{
			internal SpecialTypeNameList ()
			{
				Add (new SpecialTypeName ("Member", Member));
				Add (new SpecialTypeName ("Property", Property));
			}

			public XamlType Find (string name, string ns)
			{
				if (ns != Xaml2006Namespace)
				{
					return null;
				}

				var stn = this.FirstOrDefault (s => s.Name == name);
				return stn?.Type;
			}
		}

		internal class SpecialTypeName
		{
			public SpecialTypeName (string name, XamlType type)
			{
				Name = name;
				Type = type;
			}
			
			public string Name { get; }
			public XamlType Type { get; }
		}

		private static readonly XamlSchemaContext Sctx = new XamlSchemaContext (new[] {typeof (XamlType).Assembly});

		private static XamlType Xt<T> ()
		{
			return Sctx.GetXamlType (typeof (T));
		}

		internal static readonly bool InitializingDirectives;
		internal static readonly bool InitializingTypes;

		static XamlLanguage ()
		{
			InitializingTypes = true;

			// types

			Array = Xt<ArrayExtension> ();
			Boolean = Xt<bool> ();
			Byte = Xt<byte> ();
			Char = Xt<char> ();
			Decimal = Xt<decimal> ();
			Double = Xt<double> ();
			Int16 = Xt<short> ();
			Int32 = Xt<int> ();
			Int64 = Xt<long> ();
			Member = Xt<MemberDefinition> ();
			Null = Xt<NullExtension> ();
			Object = Xt<object> ();
			Property = Xt<PropertyDefinition> ();
			Reference = Xt<Reference> ();
			Single = Xt<float> ();
			Static = Xt<StaticExtension> ();
			String = Xt<string> ();
			TimeSpan = Xt<TimeSpan> ();
			Type = Xt<TypeExtension> ();
			Uri = Xt<Uri>();
			Bind = Xt<Bind>();
			XData = Xt<XData> ();

			InitializingTypes = false;

			AllTypes = new ReadOnlyCollection<XamlType> (new[] {Array, Boolean, Byte, Char, Decimal, Double, Int16, Int32, Int64, Member, Null, Object, Bind, Property, Reference, Single, Static, String, TimeSpan, Type, Uri, XData});

			// directives

			// Looks like predefined XamlDirectives have no ValueSerializer. 
			// To handle this situation, differentiate them from non-primitive XamlMembers.
			InitializingDirectives = true;

			var nss = new[] {Xaml2006Namespace};
			var nssXml = new[] {Xml1998Namespace};

			Arguments = new XamlDirective (nss, "Arguments", Xt<List<object>> (), null, AllowedMemberLocations.Any);
			AsyncRecords = new XamlDirective (nss, "AsyncRecords", Xt<string> (), null, AllowedMemberLocations.Attribute);
			Base = new XamlDirective (nssXml, "base", Xt<string> (), null, AllowedMemberLocations.Attribute);
			Class = new XamlDirective (nss, "Class", Xt<string> (), null, AllowedMemberLocations.Attribute);
			ClassAttributes = new XamlDirective (nss, "ClassAttributes", Xt<List<Attribute>> (), null, AllowedMemberLocations.MemberElement);
			ClassModifier = new XamlDirective (nss, "ClassModifier", Xt<string> (), null, AllowedMemberLocations.Attribute);
			Code = new XamlDirective (nss, "Code", Xt<string> (), null, AllowedMemberLocations.Attribute);
			ConnectionId = new XamlDirective (nss, "ConnectionId", Xt<string> (), null, AllowedMemberLocations.Any);
			FactoryMethod = new XamlDirective (nss, "FactoryMethod", Xt<string> (), null, AllowedMemberLocations.Any);
			FieldModifier = new XamlDirective (nss, "FieldModifier", Xt<string> (), null, AllowedMemberLocations.Attribute);
			Initialization = new XamlDirective (nss, "_Initialization", Xt<object> (), null, AllowedMemberLocations.Any);
			Items = new XamlDirective (nss, "_Items", Xt<List<object>> (), null, AllowedMemberLocations.Any);
			Key = new XamlDirective (nss, "Key", Xt<object> (), null, AllowedMemberLocations.Any);
			Lang = new XamlDirective (nssXml, "lang", Xt<string> (), null, AllowedMemberLocations.Attribute);
			Members = new XamlDirective (nss, "Members", Xt<List<MemberDefinition>> (), null, AllowedMemberLocations.MemberElement);
			Name = new XamlDirective (nss, "Name", Xt<string> (), null, AllowedMemberLocations.Attribute);
			PositionalParameters = new XamlDirective (nss, "_PositionalParameters", Xt<List<object>> (), null, AllowedMemberLocations.Any);
			Space = new XamlDirective (nssXml, "space", Xt<string> (), null, AllowedMemberLocations.Attribute);
			Subclass = new XamlDirective (nss, "Subclass", Xt<string> (), null, AllowedMemberLocations.Attribute);
			SynchronousMode = new XamlDirective (nss, "SynchronousMode", Xt<string> (), null, AllowedMemberLocations.Attribute);
			Shared = new XamlDirective (nss, "Shared", Xt<string> (), null, AllowedMemberLocations.Attribute);
			TypeArguments = new XamlDirective (nss, "TypeArguments", Xt<string> (), null, AllowedMemberLocations.Attribute);
			Uid = new XamlDirective (nss, "Uid", Xt<string> (), null, AllowedMemberLocations.Attribute);
			UnknownContent = new XamlDirective(nss, "_UnknownContent", Xt<object>(), null, AllowedMemberLocations.MemberElement) { InternalIsUnknown = true };
			Ignorable = new XamlDirective(nss, "Ignorable", Xt<object>(), null, AllowedMemberLocations.MemberElement) { InternalIsUnknown = true };

			AllDirectives = new ReadOnlyCollection<XamlDirective> (new[] {Arguments, AsyncRecords, Base, Class, ClassAttributes, ClassModifier, Code, ConnectionId, FactoryMethod, FieldModifier, Initialization, Items, Key, Lang, Members, Name, PositionalParameters, Space, Subclass, SynchronousMode, Shared, TypeArguments, Uid, UnknownContent});

			InitializingDirectives = false;

			SpecialNames = new SpecialTypeNameList ();
		}

		private static readonly string [] XamlNss = new[] {Xaml2006Namespace};

		public static IList<string> XamlNamespaces {
			get { return XamlNss; }
		}

		private static readonly string [] XmlNss = new[] {Xml1998Namespace};

		public static IList<string> XmlNamespaces {
			get { return XmlNss; }
		}

		public static ReadOnlyCollection<XamlDirective> AllDirectives { get; }

		public static XamlDirective Arguments { get; }
		public static XamlDirective AsyncRecords { get; }
		public static XamlDirective Base { get; }
		public static XamlDirective Class { get; }
		public static XamlDirective ClassAttributes { get; }
		public static XamlDirective ClassModifier { get; }
		public static XamlDirective Code { get; }
		public static XamlDirective ConnectionId { get; }
		public static XamlDirective FactoryMethod { get; }
		public static XamlDirective FieldModifier { get; }
		public static XamlDirective Initialization { get; }
		public static XamlDirective Items { get; }
		public static XamlDirective Key { get; }
		public static XamlDirective Lang { get; }
		public static XamlDirective Members { get; }
		public static XamlDirective Name { get; }
		public static XamlDirective PositionalParameters { get; }
		public static XamlDirective Subclass { get; }
		public static XamlDirective SynchronousMode { get; }
		public static XamlDirective Shared { get; }
		public static XamlDirective Space { get; }
		public static XamlDirective TypeArguments { get; }
		public static XamlDirective Uid { get; }
		public static XamlDirective UnknownContent { get; }
		public static XamlDirective Ignorable { get; }

		public static ReadOnlyCollection<XamlType> AllTypes { get; }

		public static XamlType Array { get; }
		public static XamlType Boolean { get; }
		public static XamlType Byte { get; }
		public static XamlType Char { get; }
		public static XamlType Decimal { get; }
		public static XamlType Double { get; }
		public static XamlType Int16 { get; }
		public static XamlType Int32 { get; }
		public static XamlType Int64 { get; }
		public static XamlType Member { get; }
		public static XamlType Null { get; }
		public static XamlType Object { get; }
		public static XamlType Bind { get; }
		public static XamlType Property { get; }
		public static XamlType Reference { get; }
		public static XamlType Single { get; }
		public static XamlType Static { get; }
		public static XamlType String { get; }
		public static XamlType TimeSpan { get; }
		public static XamlType Type { get; }
		public static XamlType Uri { get; }
		public static XamlType XData { get; }

		internal static bool IsValidXamlName (string name)
		{
			if (string.IsNullOrEmpty (name))
			{
				return false;
			}

			if (!IsValidXamlName (name [0], true))
			{
				return false;
			}

			foreach (char c in name)
			{
				if (!IsValidXamlName (c, false))
				{
					return false;
				}
			}

			return true;
		}

		private static bool IsValidXamlName (char c, bool first)
		{
			if (c == '_')
			{
				return true;
			}

			switch (char.GetUnicodeCategory (c)) {
			case UnicodeCategory.LowercaseLetter:
			case UnicodeCategory.UppercaseLetter:
			case UnicodeCategory.TitlecaseLetter:
			case UnicodeCategory.OtherLetter:
			case UnicodeCategory.LetterNumber:
				return true;
			case UnicodeCategory.NonSpacingMark:
			case UnicodeCategory.DecimalDigitNumber:
			case UnicodeCategory.SpacingCombiningMark:
			case UnicodeCategory.ModifierLetter:
				return !first;
			default:
				return false;
			}
		}
	}
}
