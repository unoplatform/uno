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
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Markup;
using Uno.Xaml;
using Uno.Xaml.Schema;
using System.Xml;
using NUnit.Framework;

using CategoryAttribute = NUnit.Framework.CategoryAttribute;

namespace MonoTests.Uno.Xaml
{
	[TestFixture]
	public partial class XamlXmlReaderTest : XamlReaderTestBase
	{
		// read test

		XamlReader GetReader (string filename, bool ignoreWhitespace = false)
		{
			var directory = Path.GetDirectoryName(new Uri(GetType().Assembly.CodeBase).LocalPath);

			string xml = File.ReadAllText (Path.Combine (directory, "Test/XmlFiles", filename)).Replace ("System.Xaml_test_net_4_0", "Uno.Xaml.Tests");

			var s = new XmlReaderSettings { IgnoreWhitespace = ignoreWhitespace };

			return new XamlXmlReader (XmlReader.Create (new StringReader (xml), s));
		}

		void ReadTest (string filename)
		{
			var r = GetReader (filename);
			while (!r.IsEof)
				r.Read ();
		}

		[Test]
		public void SchemaContext ()
		{
			Assert.AreNotEqual (XamlLanguage.Type.SchemaContext, new XamlXmlReader (XmlReader.Create (new StringReader ("<root/>"))).SchemaContext, "#1");
		}

		[Test]
		public void Read_Simple_SystemResourcesResources()
		{
			var sequence = new SequenceItem[] {
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ResourceDictionary"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = XamlLanguage.Base.ToString() },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Color"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "AppWhiteColor", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "#FFFFFFFF", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.None, },
			};


			ReadSequence("Simple_SemanticStylesResources.xaml", sequence);
		}

		[Test]
		public void Read_BindingMembers()
		{
			var sequence = new SequenceItem[] {
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ResourceDictionary"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = XamlLanguage.Base.ToString() },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TestObject"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TestObject.Text", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Binding"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters"},
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "[Property]", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Binding.ConverterParameter"},
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "A", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Binding.TargetNullValue"},
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "B", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Binding.FallbackValue"},
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "C,D,E,F", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.EndObject, }, // End Binding
				new SequenceItem { NodeType = XamlNodeType.EndMember, }, // End TestObject.Text
				new SequenceItem { NodeType = XamlNodeType.EndObject, }, // End TestObject
				new SequenceItem { NodeType = XamlNodeType.EndMember, }, // End UnknownContent
				new SequenceItem { NodeType = XamlNodeType.EndObject, }, // End ResourceDictionary
				new SequenceItem { NodeType = XamlNodeType.None, },
			};

			ReadSequence("BindingMembers.xaml", sequence);
		}

		[Test]
		public void Read_BindingDate()
		{
			var sequence = new SequenceItem[] {
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ResourceDictionary"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = XamlLanguage.Base.ToString() },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TestObject"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TestObject.Text", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Binding"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters"},
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "[Property]", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Binding.ConverterParameter"},
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "{0:h:mm}", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.EndObject, }, // End Binding
				new SequenceItem { NodeType = XamlNodeType.EndMember, }, // End TestObject.Text
				new SequenceItem { NodeType = XamlNodeType.EndObject, }, // End TestObject
				new SequenceItem { NodeType = XamlNodeType.EndMember, }, // End UnknownContent
				new SequenceItem { NodeType = XamlNodeType.EndObject, }, // End ResourceDictionary
				new SequenceItem { NodeType = XamlNodeType.None, },
			};

			ReadSequence("BindingDate.xaml", sequence);
		}

		[Test]
		public void Read_Binding_Escape()
		{
			var sequence = new SequenceItem[] {
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ResourceDictionary"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = XamlLanguage.Base.ToString() },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TestObject"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TestObject.StringFormat", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "**** **** **** {0}", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TestObject"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TestObject.StringFormat", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Binding"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Binding.ConverterParameter", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "test", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.None, },
			};


			ReadSequence("Binding_Escape.xaml", sequence);
		}

		[Test]
		public void Read_Binding_SemanticStylesResources()
		{
			var sequence = new SequenceItem[] {
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ResourceDictionary"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = XamlLanguage.Base.ToString() },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "AppWhiteColorBrush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "AppWhiteColor", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.None, },
			};


			ReadSequence("Binding_SemanticStylesResources.xaml", sequence);
		}
		[Test]

		public void Read_Binding2_SemanticStylesResources()
		{
			var sequence = new SequenceItem[] {
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ResourceDictionary"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = XamlLanguage.Base.ToString() },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "AppWhiteColorBrush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Binding"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "AppWhiteColor", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Binding.Converter", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}StaticResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "testResource", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.None, },
			};


			ReadSequence("Binding2_SemanticStylesResources.xaml", sequence);
		}

		[Test]
		public void Read_GenericSimple()
		{
			var sequence = new SequenceItem[] {
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ResourceDictionary"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = XamlLanguage.Base.ToString() },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Style"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "XamlDefaultTextBox", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Style.TargetType", MemberName = "TargetType" },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "TextBox", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Setter"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Setter.Property", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "Template", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Setter.Value", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ControlTemplate"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ControlTemplate.TargetType", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "TextBox", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Grid"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}VisualStateManager.VisualStateGroups", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}VisualStateGroup"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Name", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "CommonStates", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				//new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}VisualStateGroup"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Name", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "ButtonStates", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.None, },
			};

			ReadSequence("GenericSimple.xaml", sequence);
		}

		[Test]
		public void Read_GenericWithProperty()
		{
			var sequence = new SequenceItem[] {
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ResourceDictionary"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = XamlLanguage.Base.ToString() },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Style"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "DefaultComboBoxItemStyle", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Style.OtherProperty", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "test", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Style.TargetType", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SelectorItem", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.None, },
			};

			ReadSequence("GenericWithProperty.xaml", sequence);
		}

		[Test]
		public void Read_SystemResourcesResources()
		{
			ReadTest("SystemResources.xaml");
		}

		[Test]
		public void Read_Ignore01()
		{
			var sequence = new SequenceItem[] {
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },

				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ResourceDictionary"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = XamlLanguage.Base.ToString() },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },

				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Color"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "test2", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "#123456", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },

				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },

				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Color"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "test4", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "#323456", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.None, },
			};

			ReadSequence("IgnoreDirective.xaml", sequence);
		}

		[Test]
		public void Read_GenericNative()
		{
			ReadTest("Generic.Native.xaml");
		}

		[Test]
		public void Read_GenericNative2()
		{
			var sequence = new SequenceItem[] {
			new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ResourceDictionary" },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = XamlLanguage.Base.ToString() },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Style" },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Setter" },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Setter.Property", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "Template", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Setter.Value", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ControlTemplate" },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}StackPanel" },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}DeferLoadStrategy", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "Lazy", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Grid.Row", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "0", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}StackPanel.Resources", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}BindableUISwitch" },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ContentPresenter" },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.None, },
			};

			ReadSequence("GenericNative2.xaml", sequence);
		}

		[Test]
		public void Read_SemanticStylesResources()
		{
			var sequence = new SequenceItem[] {
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ResourceDictionary"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = XamlLanguage.Base.ToString() },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Color"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "AppWhiteColor", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "#FFFFFFFF", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Color"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "AppBlackColor", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "#FF000000", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Color"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "AppLightGrayColor", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "#FFCCCCCC", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Color"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "AppGrayColor", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "#80000000", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Color"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "AppDarkGrayColor", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "#CC000000", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Color"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "AppErrorRedColor", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "#FFA3BA1B", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Color"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "PrimaryColor1", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "#FF003053", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Color"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "PrimaryColor2", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "#FFFF9B00", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Color"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "PrimaryColor3", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "#FFEDF4F7", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Color"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor1", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "#FF3B6B93", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Color"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor2", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "#FFAEC1CF", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Color"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor3", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "#FFCDD7DC", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Color"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor4", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "#FFEDF4F7", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Color"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor5", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "#FF005288", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Color"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor6", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "#FF444444", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Color"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor7", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "#FF292929", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Color"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor8", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "#FF6E6F71", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Color"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "TertiaryColor1", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "#FF0082AA", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Color"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "TertiaryColor2", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "#FFB5B635", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Color"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "TertiaryColor3", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "#FF38B0DE", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "TransparentColorBrush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "Transparent", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "AppWhiteColorBrush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "AppWhiteColor", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "AppBlackColorBrush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "AppBlackColor", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "AppLightGrayColorBrush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "AppLightGrayColor", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "AppGrayColorBrush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "AppGrayColor", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "AppDarkGrayColorBrush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "AppDarkGrayColor", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "AppErrorRedColorBrush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "AppErrorRedColor", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "PrimaryColor1Brush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "PrimaryColor1", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "PrimaryColor2Brush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "PrimaryColor2", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "PrimaryColor3Brush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "PrimaryColor3", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor1Brush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor1", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor2Brush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor2", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor3Brush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor3", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor4Brush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor4", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor5Brush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor5", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor6Brush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor6", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor7Brush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor7", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor8Brush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor8", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "TertiaryColor1Brush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "TertiaryColor1", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "TertiaryColor2Brush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "TertiaryColor2", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "TertiaryColor3Brush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "TertiaryColor3", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "TileBackgroundBrush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor1", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SplashScreenBackgroundBrush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "AppWhiteColor", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "TopbarBackgroundBrush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor2", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "BottomAppbarBackgroundColorBrush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor2", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "FlyoutBackgroundBrush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor3", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "PaneBackgroundBrush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "TertiaryColor2", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "MessageDialogBoxBackgroundBrush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "AppWhiteColor", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "ImageOverlayBackgroundBrush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor2", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "ListViewBackgroundBrush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}StaticResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor4", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "PlaceholderImageBrush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "AppLightGrayColor", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "TextBlockForegroundBrush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}StaticResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SecondaryColor6", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "MessageDialogBoxBorderBrush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "AppBlackColor", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "ProgressRingForegroundThemeBrush", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}SolidColorBrush.Color", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ThemeResource"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "AppBlackColor", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}FontWeight"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "TextBoxFontWeightHeaderTheme", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "Light", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}FontWeight"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "ButtonFontWeight", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "Normal", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml}Double"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "TextControlInputZoneThemeInnerMinHeight", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_Initialization", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "28", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.None, },
			};

			ReadSequence("SemanticStylesResources.xaml", sequence);
		}

		[Test]
		public void Read_TextContent()
		{
			var sequence = new SequenceItem[] {
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Page"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = XamlLanguage.Base.ToString() },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Class", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "Uno.NativeStyles.MainPage", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TextBlock"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "This Sample demonstrates the use of Xaml controls, and how their visual appearance changes between their UWP Xaml style and native controls. Toggle the button below to switch between the two styles.", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TextBlock"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "This Sample demonstrates the use of Xaml controls, and how their visual appearance changes between their UWP Xaml style and native controls. Toggle the button below to switch between the two styles.", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TextBlock"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://www.w3.org/XML/1998/namespace}space", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "preserve", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "\n        This Sample demonstrates the use of Xaml controls, and how their visual appearance changes between their UWP Xaml style\n        and native controls. \r\n Toggle the button below to switch between the two styles.\n\t", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TextBlock"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "This Sample demonstrates the use of Xaml controls, and how their visual appearance changes between their UWP Xaml style and native controls. ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}LineBreak"},
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " Toggle the button below to switch between the two styles.", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.None, },
			};

			ReadSequence("TextContent.xaml", sequence);
		}

		[Test]
		public void Read_TextLiteral()
		{
			var sequence = new SequenceItem[] 
			{
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Page"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = XamlLanguage.Base.ToString() },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Class", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "Uno.NativeStyles.MainPage", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TextBlock"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Hyperlink"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Hyperlink.NavigateUri", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "http://www.lipsum.com", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "Lorem ipsum", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " dolor laborum. ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Hyperlink"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Hyperlink.NavigateUri", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "http://www.lipsum.com", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "Lorem ipsum", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " dolor sit laborum.", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.None, },
			};

			ReadSequence("TextLiteral.xaml", sequence);
		}

		[Test]
		public void Read_WhiteSpacePreservation()
		{
			var sequence = new SequenceItem[] 
			{
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Page"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = XamlLanguage.Base.ToString() },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				 
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TextBlock"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "this is some text", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TextBlock"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://www.w3.org/XML/1998/namespace}space", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "preserve", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " this is some text ", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TextBlock"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "this is", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Run"},
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "some text", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TextBlock"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "this is ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Run"},
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " some text", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TextBlock"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://www.w3.org/XML/1998/namespace}space", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "preserve", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " this is ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Run"},
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " some text ", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TextBlock"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://www.w3.org/XML/1998/namespace}space", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "preserve", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " this is   text with multi spaces ", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TextBlock"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://www.w3.org/XML/1998/namespace}space", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "preserve", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "  this is   text with multi spaces  ", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TextBlock"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "this is text with tabs", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TextBlock"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://www.w3.org/XML/1998/namespace}space", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "preserve", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "  this is\t text with tabs  ", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.None, },
			};

			ReadSequence("WhiteSpacePreservation.xaml", sequence);
		}

		[Test]
		public void Read_CustomAttachedProperty()
		{
			var sequence = new SequenceItem[] 
			{
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}UserControl"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = XamlLanguage.Base.ToString() },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Grid"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{using:Uno.UI.Toolkit}MyAttachedProperty.IsEnabled", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "True", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.None, },
			};

			ReadSequence("CustomAttachedProperty.xaml", sequence);
		}

		[Test]
		public void Read_xBind()
		{
			var sequence = new SequenceItem[] 
			{
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}UserControl"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = XamlLanguage.Base.ToString() },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Grid"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Grid.DataContext", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml}Bind"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "MyContent", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.None, },
			};

			ReadSequence("xBind.xaml", sequence);
		}

		[Test]
		public void Read_EmptyAttachedPropertyNode()
		{
			var r = GetReader("EmptyAttachedPropertyNode.xaml");

			while (r.Read()) { }

			var sequence = new SequenceItem[] 
			{
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}UserControl"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = XamlLanguage.Base.ToString() },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}VisualState"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}VisualState.Setters", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.None, },
			};

			ReadSequence("EmptyAttachedPropertyNode.xaml", sequence);
		}

		[Test]
		public void Read_RunSpace01()
		{
			var sequence = new SequenceItem[] 
			{
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}UserControl"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = XamlLanguage.Base.ToString() },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TextBlock"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },

				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Run"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Run.Text", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "AB", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },

				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },

				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Run"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Run.Text", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "CD", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },

				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.None, },
			};

			ReadSequence("RunSpace01.xaml", sequence);
		}

		[Test]
		public void Read_RunSpace02()
		{
			var sequence = new SequenceItem[] 
			{
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}UserControl"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = XamlLanguage.Base.ToString() },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}a"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}b"},
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}b"},
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}b"},
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}b"},
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}c"},
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = " ", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}c"},
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.None, },
			};

			ReadSequence("RunSpace02.xaml", sequence);
		}

		[Test]
		public void Read_AttachedPropertyWithoutNamespace()
		{
			var sequence = new SequenceItem[]
			{
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },

				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ResourceDictionary" },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = XamlLanguage.Base.ToString() },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{using:MyOtherNamespace}TestObject" },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Attached.Original", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "test", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.None, },
			};

			ReadSequence("AttachedPropertyWithoutNamespace.xaml", sequence);
		}

		[Test]
		public void Read_AttachedPropertyWithNamespace()
		{
			var sequence = new SequenceItem[]
			{
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ResourceDictionary"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = XamlLanguage.Base.ToString() },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{using:MyOtherNamespace}TestObject"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{using:MyOtherNamespace}Attached.Original", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "test", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{using:MyOtherNamespace2}Attached2.Original", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "test", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.None, },
			};

			ReadSequence("AttachedPropertyWithNamespace.xaml", sequence);
		}

		[Test]
		public void Read_xBindFunctionSingleParamWithoutPath()
		{
			var sequence = new SequenceItem[]
			{
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Page"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = XamlLanguage.Base.ToString() },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TextBlock"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TextBlock.Text", },

				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml}Bind"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "Add(a.Value)", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },

				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.None, },
			};

			ReadSequence("xBindFunctionSingleParamWithoutPath.xaml", sequence);
		}

		[Test]
		public void Read_xBindFunctionSingleParamWithPath()
		{
			var sequence = new SequenceItem[]
{
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Page"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = XamlLanguage.Base.ToString() },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TextBlock"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TextBlock.Text", },

				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml}Bind"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Bind.Path", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "Add(a.Value)", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },

				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.None, },
};


			ReadSequence("xBindFunctionSingleParamWithPath.xaml", sequence);
		}

		[Test]
		public void Read_xBindFunctionTwoParamWithoutPath()
		{
			var sequence = new SequenceItem[]
			{
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Page"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = XamlLanguage.Base.ToString() },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TextBlock"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TextBlock.Text", },

				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml}Bind"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "Add(a.Value", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "b.Value)", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },

				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.None, },
			};

			ReadSequence("xBindFunctionTwoParamWithoutPath.xaml", sequence);
		}

		[Test]
		public void Read_xBindFunctionTwoParamsWithPath()
		{
			var sequence = new SequenceItem[]
			{
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Page"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = XamlLanguage.Base.ToString() },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TextBlock"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TextBlock.Text", },

				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml}Bind"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Bind.Path", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "Add(a.Value", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "b.Value)", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },

				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.None, },
			};


			ReadSequence("xBindFunctionTwoParamsWithPath.xaml", sequence);
		}

		[Test]
		public void Read_xBindFunctionStringDoubleParams()
		{
			var sequence = new SequenceItem[]
			{
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Page"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = XamlLanguage.Base.ToString() },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TextBlock"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TextBlock.Text", },

				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml}Bind"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "Test(45.2", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "'myString')", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },

				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.None, },
			};


			ReadSequence("xBindFunctionStringDoubleParams.xaml", sequence);
		}

		[Test]
		public void Read_xBindFunctionEscapedString()
		{
			var sequence = new SequenceItem[]
			{
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Page"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = XamlLanguage.Base.ToString() },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TextBlock"},

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TextBlock.Text", },

				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml}Bind"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "Test('myString ^' escaped ')", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },

				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}TextBlock.Text2", },

				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml}Bind"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_PositionalParameters", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "Test('myString  ^'  escaped ')", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },

				new SequenceItem { NodeType = XamlNodeType.EndMember, },

				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.None, },
			};


			ReadSequence("xBindFunctionEscapedString.xaml", sequence);
		}

		[Test]
		public void Read_Int32 ()
		{
			ReadTest ("Int32.xml");
		}

		[Test]
		public void Read_DateTime ()
		{
			ReadTest ("DateTime.xml");
		}

		[Test]
		public void Read_TimeSpan ()
		{
			ReadTest ("TimeSpan.xml");
		}

		[Test]
		public void Read_ArrayInt32 ()
		{
			ReadTest ("Array_Int32.xml");
		}

		[Test]
		public void Read_DictionaryInt32String ()
		{
			ReadTest ("Dictionary_Int32_String.xml");
		}

		[Test]
		public void Read_DictionaryStringType ()
		{
			ReadTest ("Dictionary_String_Type.xml");
		}

		[Test]
		public void Read_SilverlightApp1 ()
		{
			ReadTest ("SilverlightApp1.xaml");
		}

		[Test]
		public void Read_Guid ()
		{
			ReadTest ("Guid.xml");
		}

		[Test]
		public void Read_GuidFactoryMethod ()
		{
			ReadTest ("GuidFactoryMethod.xml");
		}

		[Test]
		public void ReadInt32Details ()
		{
			var r = GetReader ("Int32.xml");

			Assert.IsTrue (r.Read (), "ns#1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ns#2");
			Assert.AreEqual (XamlLanguage.Xaml2006Namespace, r.Namespace.Namespace, "ns#3");

			Assert.IsTrue (r.Read (), "so#1");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "so#2");
			Assert.AreEqual (XamlLanguage.Int32, r.Type, "so#3");

			ReadBase (r);

			Assert.IsTrue (r.Read (), "sinit#1");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "sinit#2");
			Assert.AreEqual (XamlLanguage.Initialization, r.Member, "sinit#3");

			Assert.IsTrue (r.Read (), "vinit#1");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "vinit#2");
			Assert.AreEqual ("5", r.Value, "vinit#3"); // string

			Assert.IsTrue (r.Read (), "einit#1");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "einit#2");

			Assert.IsTrue (r.Read (), "eo#1");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "eo#2");

			Assert.IsFalse (r.Read (), "end");
		}

		[Test]
		public void ReadDateTimeDetails ()
		{
			var r = GetReader ("DateTime.xml");

			Assert.IsTrue (r.Read (), "ns#1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ns#2");
			Assert.AreEqual ("clr-namespace:System;assembly=mscorlib", r.Namespace.Namespace, "ns#3");

			Assert.IsTrue (r.Read (), "so#1");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "so#2");
			Assert.AreEqual (r.SchemaContext.GetXamlType (typeof (DateTime)), r.Type, "so#3");

			ReadBase (r);

			Assert.IsTrue (r.Read (), "sinit#1");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "sinit#2");
			Assert.AreEqual (XamlLanguage.Initialization, r.Member, "sinit#3");

			Assert.IsTrue (r.Read (), "vinit#1");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "vinit#2");
			Assert.AreEqual ("2010-04-14", r.Value, "vinit#3"); // string

			Assert.IsTrue (r.Read (), "einit#1");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "einit#2");

			Assert.IsTrue (r.Read (), "eo#1");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "eo#2");
			Assert.IsFalse (r.Read (), "end");
		}

		[Test]
		public void ReadGuidFactoryMethodDetails ()
		{
			var r = GetReader ("GuidFactoryMethod.xml");

			Assert.IsTrue (r.Read (), "ns#1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ns#2");
			Assert.AreEqual ("clr-namespace:System;assembly=mscorlib", r.Namespace.Namespace, "ns#3");
			Assert.AreEqual (String.Empty, r.Namespace.Prefix, "ns#4");

			Assert.IsTrue (r.Read (), "ns2#1");
			Assert.AreEqual (XamlNodeType.NamespaceDeclaration, r.NodeType, "ns2#2");
			Assert.AreEqual (XamlLanguage.Xaml2006Namespace, r.Namespace.Namespace, "ns2#3");
			Assert.AreEqual ("x", r.Namespace.Prefix, "ns2#4");

			Assert.IsTrue (r.Read (), "so#1");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "so#2");
			var xt = r.SchemaContext.GetXamlType (typeof (Guid));
			Assert.AreEqual (xt, r.Type, "so#3");

			ReadBase (r);

			Assert.IsTrue (r.Read (), "sfactory#1");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "sfactory#2");
			Assert.AreEqual (XamlLanguage.FactoryMethod, r.Member, "sfactory#3");

			Assert.IsTrue (r.Read (), "vfactory#1");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "vfactory#2");
			Assert.AreEqual ("Parse", r.Value, "vfactory#3"); // string

			Assert.IsTrue (r.Read (), "efactory#1");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "efactory#2");

			Assert.IsTrue (r.Read (), "sarg#1");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "sarg#2");
			Assert.AreEqual (XamlLanguage.Arguments, r.Member, "sarg#3");

			Assert.IsTrue (r.Read (), "sarg1#1");
			Assert.AreEqual (XamlNodeType.StartObject, r.NodeType, "sarg1#2");
			Assert.AreEqual (XamlLanguage.String, r.Type, "sarg1#3");

			Assert.IsTrue (r.Read (), "sInit#1");
			Assert.AreEqual (XamlNodeType.StartMember, r.NodeType, "sInit#2");
			Assert.AreEqual (XamlLanguage.Initialization, r.Member, "sInit#3");

			Assert.IsTrue (r.Read (), "varg1#1");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "varg1#2");
			Assert.AreEqual ("9c3345ec-8922-4662-8e8d-a4e41f47cf09", r.Value, "varg1#3");

			Assert.IsTrue (r.Read (), "eInit#1");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "eInit#2");

			Assert.IsTrue (r.Read (), "earg1#1");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "earg1#2");

			Assert.IsTrue (r.Read (), "earg#1");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "earg#2");


			Assert.IsTrue (r.Read (), "eo#1");
			Assert.AreEqual (XamlNodeType.EndObject, r.NodeType, "eo#2");

			Assert.IsFalse (r.Read (), "end");
		}

		[Test]
		public void ReadEventStore ()
		{
			var r = GetReader ("EventStore2.xml");

			var xt = r.SchemaContext.GetXamlType (typeof (EventStore));
			var xm = xt.GetMember ("Event1");
			Assert.IsNotNull (xt, "premise#1");
			Assert.IsNotNull (xm, "premise#2");
			Assert.IsTrue (xm.IsEvent, "premise#3");
			while (true) {
				r.Read ();
				if (r.Member != null && r.Member.IsEvent)
					break;
				if (r.IsEof)
					Assert.Fail ("Items did not appear");
			}

			Assert.AreEqual (xm, r.Member, "#x1");
			Assert.AreEqual ("Event1", r.Member.Name, "#x2");

			Assert.IsTrue (r.Read (), "#x11");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "#x12");
			Assert.AreEqual ("Method1", r.Value, "#x13");

			Assert.IsTrue (r.Read (), "#x21");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#x22");

			xm = xt.GetMember ("Event2");
			Assert.IsTrue (r.Read (), "#x31");
			Assert.AreEqual (xm, r.Member, "#x32");
			Assert.AreEqual ("Event2", r.Member.Name, "#x33");

			Assert.IsTrue (r.Read (), "#x41");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "#x42");
			Assert.AreEqual ("Method2", r.Value, "#x43");

			Assert.IsTrue (r.Read (), "#x51");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#x52");

			Assert.IsTrue (r.Read (), "#x61");
			Assert.AreEqual ("Event1", r.Member.Name, "#x62");

			Assert.IsTrue (r.Read (), "#x71");
			Assert.AreEqual (XamlNodeType.Value, r.NodeType, "#x72");
			Assert.AreEqual ("Method3", r.Value, "#x73"); // nonexistent, but no need to raise an error.

			Assert.IsTrue (r.Read (), "#x81");
			Assert.AreEqual (XamlNodeType.EndMember, r.NodeType, "#x82");

			while (!r.IsEof)
				r.Read ();

			r.Close ();
		}

		// common XamlReader tests.

		[Test]
		public void Read_String ()
		{
			var r = GetReader ("String.xml");
			Read_String (r);
		}

		[Test]
		public void WriteNullMemberAsObject ()
		{
			var r = GetReader ("TestClass4.xml");
			WriteNullMemberAsObject (r, null);
		}
		
		[Test]
		public void StaticMember ()
		{
			var r = GetReader ("TestClass5.xml");
			StaticMember (r);
		}

		[Test]
		public void Skip ()
		{
			var r = GetReader ("String.xml");
			Skip (r);
		}
		
		[Test]
		public void Skip2 ()
		{
			var r = GetReader ("String.xml");
			Skip2 (r);
		}

		[Test]
		public void Read_XmlDocument ()
		{
			var doc = new XmlDocument ();
			doc.LoadXml ("<root xmlns='urn:foo'><elem attr='val' /></root>");
			// note that corresponding XamlXmlWriter is untested yet.
			var r = GetReader ("XmlDocument.xml");
			Read_XmlDocument (r);
		}

		[Test]
		public void Read_NonPrimitive ()
		{
			var r = GetReader ("NonPrimitive.xml");
			Read_NonPrimitive (r);
		}
		
		[Test]
		public void Read_TypeExtension ()
		{
			var r = GetReader ("Type.xml");
			Read_TypeOrTypeExtension (r, null, XamlLanguage.Type.GetMember ("Type"));
		}
		
		[Test]
		public void Read_Type2 ()
		{
			var r = GetReader ("Type2.xml");
			Read_TypeOrTypeExtension2 (r, null, XamlLanguage.Type.GetMember ("Type"));
		}
		
		[Test]
		public void Read_Reference ()
		{
			var r = GetReader ("Reference.xml");
			Read_Reference (r);
		}
		
		[Test]
		public void Read_Null ()
		{
			var r = GetReader ("NullExtension.xml");
			Read_NullOrNullExtension (r, null);
		}
		
		[Test]
		public void Read_StaticExtension ()
		{
			var r = GetReader ("StaticExtension.xml");
			Read_StaticExtension (r, XamlLanguage.Static.GetMember ("Member"));
		}
		
		[Test]
		public void Read_ListInt32 ()
		{
			var r = GetReader ("List_Int32.xml", ignoreWhitespace: true);
			Read_ListInt32 (r, null, new int [] {5, -3, int.MaxValue, 0}.ToList ());
		}
		
		[Test]
		public void Read_ListInt32_2 ()
		{
			var r = GetReader ("List_Int32_2.xml");
			Read_ListInt32 (r, null, new int [0].ToList ());
		}
		
		[Test]
		public void Read_ListType ()
		{
			var r = GetReader ("List_Type.xml", ignoreWhitespace: true);
			Read_ListType (r, false);
		}

		[Test]
		public void Read_ListArray ()
		{
			var r = GetReader ("List_Array.xml", ignoreWhitespace: true);
			Read_ListArray (r);
		}

		[Test]
		public void Read_ArrayList ()
		{
			var r = GetReader ("ArrayList.xml", ignoreWhitespace: true);
			Read_ArrayList (r);
		}
		
		[Test]
		public void Read_Array ()
		{
			var r = GetReader ("ArrayExtension.xml", ignoreWhitespace: true);
			Read_ArrayOrArrayExtensionOrMyArrayExtension (r, null, typeof (ArrayExtension));
		}
		
		[Test]
		public void Read_MyArrayExtension ()
		{
			var r = GetReader ("MyArrayExtension.xml", ignoreWhitespace: true);
			Read_ArrayOrArrayExtensionOrMyArrayExtension (r, null, typeof (MyArrayExtension));
		}

		[Test]
		public void Read_ArrayExtension2 ()
		{
			var r = GetReader ("ArrayExtension2.xml");
			Read_ArrayExtension2 (r);
		}

		[Test]
		public void Read_CustomMarkupExtension ()
		{
			var r = GetReader ("MyExtension.xml");
			Read_CustomMarkupExtension (r);
		}
		
		[Test]
		public void Read_CustomMarkupExtension2 ()
		{
			var r = GetReader ("MyExtension2.xml");
			Read_CustomMarkupExtension2 (r);
		}
		
		[Test]
		public void Read_CustomMarkupExtension3 ()
		{
			var r = GetReader ("MyExtension3.xml");
			Read_CustomMarkupExtension3 (r);
		}
		
		[Test]
		public void Read_CustomMarkupExtension4 ()
		{
			var r = GetReader ("MyExtension4.xml");
			Read_CustomMarkupExtension4 (r);
		}
		
		[Test]
		public void Read_CustomMarkupExtension6 ()
		{
			var r = GetReader ("MyExtension6.xml");
			Read_CustomMarkupExtension6 (r);
		}

		[Test]
		public void Read_ArgumentAttributed ()
		{
			var obj = new ArgumentAttributed ("foo", "bar");
			var r = GetReader ("ArgumentAttributed.xml", ignoreWhitespace: true);
			Read_ArgumentAttributed (r, obj);
		}

		[Test]
		public void Read_Dictionary ()
		{
			var obj = new Dictionary<string,object> ();
			obj ["Foo"] = 5.0;
			obj ["Bar"] = -6.5;
			var r = GetReader ("Dictionary_String_Double.xml", ignoreWhitespace: true);
			Read_Dictionary (r);
		}
		
		[Test]
		public void Read_Dictionary2 ()
		{
			var obj = new Dictionary<string,Type> ();
			obj ["Foo"] = typeof (int);
			obj ["Bar"] = typeof (Dictionary<Type,XamlType>);
			var r = GetReader ("Dictionary_String_Type_2.xml", ignoreWhitespace: true);
			Read_Dictionary2 (r, XamlLanguage.Type.GetMember ("Type"));
		}
		
		[Test]
		[Ignore("Not supported for UWP XAML markup extensions")]
		public void PositionalParameters2 ()
		{
			var r = GetReader ("PositionalParametersWrapper.xml");
			PositionalParameters2 (r);
		}

		[Test]
		public void ComplexPositionalParameters ()
		{
			var r = GetReader ("ComplexPositionalParameterWrapper.xml");
			ComplexPositionalParameters (r);
		}
		
		[Test]
		public void Read_ListWrapper ()
		{
			var r = GetReader ("ListWrapper.xml", ignoreWhitespace: true);
			Read_ListWrapper (r);
		}
		
		[Test]
		public void Read_ListWrapper2 () // read-write list member.
		{
			var r = GetReader ("ListWrapper2.xml", ignoreWhitespace: true);
			Read_ListWrapper2 (r);
		}

		[Test]
		public void Read_ContentIncluded ()
		{
			var r = GetReader ("ContentIncluded.xml");
			Read_ContentIncluded (r);
		}

		[Test]
		public void Read_PropertyDefinition ()
		{
			var r = GetReader ("PropertyDefinition.xml");
			Read_PropertyDefinition (r);
		}

		[Test]
		public void Read_StaticExtensionWrapper ()
		{
			var r = GetReader ("StaticExtensionWrapper.xml");
			Read_StaticExtensionWrapper (r);
		}

		[Test]
		public void Read_TypeExtensionWrapper ()
		{
			var r = GetReader ("TypeExtensionWrapper.xml");
			Read_TypeExtensionWrapper (r);
		}

		[Test]
		public void Read_NamedItems ()
		{
			var r = GetReader ("NamedItems.xml", ignoreWhitespace: true);
			Read_NamedItems (r, false);
		}

		[Test]
		public void Read_NamedItems2 ()
		{
			var r = GetReader ("NamedItems2.xml", ignoreWhitespace: true);
			Read_NamedItems2 (r, false);
		}

		[Test]
		public void Read_XmlSerializableWrapper ()
		{
			var r = GetReader ("XmlSerializableWrapper.xml");
			Read_XmlSerializableWrapper (r, false);
		}

		[Test]
		public void Read_XmlSerializable ()
		{
			var r = GetReader ("XmlSerializable.xml");
			Read_XmlSerializable (r);
		}

		[Test]
		public void Read_ListXmlSerializable ()
		{
			var r = GetReader ("List_XmlSerializable.xml");
			Read_ListXmlSerializable (r);
		}

		[Test]
		public void Read_AttachedProperty ()
		{
			var r = GetReader ("AttachedProperty.xml");
			Read_AttachedProperty (r);
		}

		[Test]
		public void Read_AbstractWrapper ()
		{
			var r = GetReader ("AbstractContainer.xml");
			while (!r.IsEof)
				r.Read ();
		}

		[Test]
		public void Read_ReadOnlyPropertyContainer ()
		{
			var r = GetReader ("ReadOnlyPropertyContainer.xml");
			while (!r.IsEof)
				r.Read ();
		}

		[Test]
		public void Read_TypeConverterOnListMember ()
		{
			var r = GetReader ("TypeConverterOnListMember.xml");
			Read_TypeConverterOnListMember (r);
		}

		[Test]
		public void Read_EnumContainer ()
		{
			var r = GetReader ("EnumContainer.xml");
			Read_EnumContainer (r);
		}

		[Test]
		public void Read_CollectionContentProperty ()
		{
			var r = GetReader ("CollectionContentProperty.xml", ignoreWhitespace: true);
			Read_CollectionContentProperty (r, false);
		}

		[Test]
		public void Read_CollectionContentProperty2 ()
		{
			// bug #681835
			var r = GetReader ("CollectionContentProperty2.xml", ignoreWhitespace: true);
			Read_CollectionContentProperty (r, true);
		}

		[Test]
		public void Read_CollectionContentPropertyX ()
		{
			var r = GetReader ("CollectionContentPropertyX.xml", ignoreWhitespace: true);
			Read_CollectionContentPropertyX (r, false);
		}

		[Test]
		public void Read_CollectionContentPropertyX2 ()
		{
			var r = GetReader ("CollectionContentPropertyX2.xml", ignoreWhitespace: true);
			Read_CollectionContentPropertyX (r, true);
		}

		[Test]
		public void Read_AmbientPropertyContainer ()
		{
			var r = GetReader ("AmbientPropertyContainer.xml", ignoreWhitespace: true);
			Read_AmbientPropertyContainer (r, false);
		}

		[Test]
		public void Read_AmbientPropertyContainer2 ()
		{
			var r = GetReader ("AmbientPropertyContainer2.xml", ignoreWhitespace: true);
			Read_AmbientPropertyContainer (r, true);
		}

		[Test]
		public void Read_NullableContainer ()
		{
			var r = GetReader ("NullableContainer.xml");
			Read_NullableContainer (r);
		}

		// It is not really a common test; it just makes use of base helper methods.
		[Test]
		public void Read_DirectListContainer ()
		{
			var r = GetReader ("DirectListContainer.xml", ignoreWhitespace: true);
			Read_DirectListContainer (r);
		}

		// It is not really a common test; it just makes use of base helper methods.
		[Test]
		public void Read_DirectDictionaryContainer ()
		{
			var r = GetReader ("DirectDictionaryContainer.xml", ignoreWhitespace: true);
			Read_DirectDictionaryContainer (r);
		}

		// It is not really a common test; it just makes use of base helper methods.
		[Test]
		public void Read_DirectDictionaryContainer2 ()
		{
			var r = GetReader ("DirectDictionaryContainer2.xml", ignoreWhitespace: true);
			Read_DirectDictionaryContainer2 (r);
		}
		
		[Test]
		public void Read_ContentPropertyContainer ()
		{
			var r = GetReader ("ContentPropertyContainer.xml", ignoreWhitespace: true);
			Read_ContentPropertyContainer (r);
		}

		#region non-common tests
		[Test]
		public void Bug680385 ()
		{
			GetReader("CurrentVersion.xaml");
		}
		#endregion

		private void ReadSequence(string fileName, IEnumerable<SequenceItem> sequence)
		{
			//ReadTest(fileName);

			var r = GetReader(fileName);

			var e = sequence.GetEnumerator();
			while (e.MoveNext() && r.Read())
			{
				string message = $"{e.Current.SequenceMember}:{e.Current.SequenceLineNumber}";

				Assert.AreEqual(e.Current.NodeType, r.NodeType, message);

				if (e.Current.MemberType != null)
				{
					Assert.AreEqual(e.Current.MemberType, r.Member?.ToString(), message);
				}

				if (e.Current.MemberName != null)
				{
					Assert.AreEqual(e.Current.MemberName, r.Member?.Name, message);
				}

				if (e.Current.Value != null)
				{
					Assert.AreEqual(e.Current.Value, r.Value, message);
				}

				if (e.Current.TypeName != null)
				{
					Assert.AreEqual(e.Current.TypeName, r.Type?.ToString(), message);
				}
			}
		}
	}
}
