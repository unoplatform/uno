#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using MonoTests.System.Xaml;
using NUnit.Framework;

namespace System.Xaml.Tests.MS
{
	[TestFixture]
	[Ignore("Disabled until Xaml Writer is fixed")]
	public class MicrosoftXamlOriginal
    {
		[Test]

		public void Original_Read_SystemResourcesResources()
		{
			var s = WriteTest("Simple_SemanticStylesResources.xaml");

			var sequence = new SequenceItem[] {
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ResourceDictionary"},
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

		public void Original_Read_IgnoreDirective()
		{
			var s = WriteTest("IgnoreDirective.xaml");

			var sequence = new SequenceItem[] {
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ResourceDictionary"},
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
		public void Read_SemanticStylesResources()
		{
			var s = WriteTest("SemanticStylesResources.xaml");

			var sequence = new SequenceItem[] {
			};

			ReadSequence("SemanticStylesResources.xaml", sequence);
		}

		[Test]
		public void Read_GenericNative()
		{
			var s = WriteTest("Generic.Native.xaml");

			var sequence = new SequenceItem[] {
			};

			ReadSequence("Generic.Native.xaml", sequence);
		}

		[Test]
		public void Read_GenericNative2()
		{
			var s = WriteTest("GenericNative2.xaml");

			var sequence = new SequenceItem[] {
			};

			ReadSequence("GenericNative2.xaml", sequence);
		}

		[Test]
		public void Read_GenericNative3()
		{
			var s = WriteTest("GenericNative3.xaml");

			var sequence = new SequenceItem[] {
			};

			ReadSequence("GenericNative2.xaml", sequence);
		}

		[Test]
		public void Read_Binding_SemanticStylesResource()
		{
			var s = WriteTest("Binding_SemanticStylesResources.xaml");

			var sequence = new SequenceItem[] {
			};

			ReadSequence("Binding_SemanticStylesResources.xaml", sequence);
		}

		[Test]
		public void Read_Binding2_SemanticStylesResource()
		{
			var s = WriteTest("Binding2_SemanticStylesResources.xaml");

			var sequence = new SequenceItem[] {
			};

			ReadSequence("Binding2_SemanticStylesResources.xaml", sequence);
		}

		[Test]
		public void Read_GenericSimple()
		{
			var s = WriteTest("GenericSimple.xaml");

			var sequence = new SequenceItem[] {
			};

			ReadSequence("TextContent.xaml", sequence);
		}
		[Test]
		public void Read_TextContent()
		{
			var s = WriteTest("TextContent.xaml");

			var sequence = new SequenceItem[] {
			};

			ReadSequence("TextContent.xaml", sequence);
		}

		[Test]
		public void Read_AttachedProperty()
		{
			var s = WriteTest("AttachedProperty.xaml");

			var sequence = new SequenceItem[] {
			};

			ReadSequence("AttachedProperty.xaml", sequence);
		}

		[Test]
		public void Read_TextLiteral()
		{
			var s = WriteTest("TextLiteral.xaml");

			var sequence = new SequenceItem[] {
			};

			ReadSequence("TextLiteral.xaml", sequence);
		}

		[Test]
		public void Read_WhiteSpacePreservation()
		{
			var s = WriteTest("WhiteSpacePreservation.xaml");

			var sequence = new SequenceItem[] {
			};

			ReadSequence("WhiteSpacePreservation.xaml", sequence);
		}

		[Test]
		public void Read_xBind()
		{
			var s = WriteTest("xBind.xaml");

			var sequence = new SequenceItem[] {
			};

			ReadSequence("xBind.xaml", sequence);
		}

		[Test]
		public void Read_RunSpace()
		{
			var s = WriteTest("RunSpace02.xaml");  

			var sequence = new SequenceItem[] {
			};

			ReadSequence("RunSpace02.xaml", sequence);
		}

		[Test]
		public void Read_AttachedPropertyWithNamespace()
		{
			var s = WriteTest("AttachedPropertyWithNamespace.xaml");

			var sequence = new SequenceItem[] {
			};

			ReadSequence("AttachedPropertyWithNamespace.xaml", sequence);
		}

		[Test]
		public void Read_AttachedPropertyWithoutNamespace()
		{
			var s = WriteTest("AttachedPropertyWithoutNamespace.xaml");

			var sequence = new SequenceItem[] {
			};

			ReadSequence("AttachedPropertyWithoutNamespace.xaml", sequence);
		}

		[Test]
		public void Read_GenericWithProperty()
		{
			var s = WriteTest("GenericWithProperty.xaml");

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
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ResourceDictionary"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Style"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "DefaultComboBoxItemStyle", },
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
		public void Read_EmptyAttachedPropertyNode()
		{
			var s = WriteTest("EmptyAttachedPropertyNode.xaml");

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
				new SequenceItem { NodeType = XamlNodeType.NamespaceDeclaration, },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}ResourceDictionary"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}_UnknownContent", },
				new SequenceItem { NodeType = XamlNodeType.StartObject, TypeName = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Style"},
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml}Key", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "DefaultComboBoxItemStyle", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.StartMember, MemberType = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}Style.TargetType", },
				new SequenceItem { NodeType = XamlNodeType.Value, Value = "SelectorItem", },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.EndMember, },
				new SequenceItem { NodeType = XamlNodeType.EndObject, },
				new SequenceItem { NodeType = XamlNodeType.None, },
			};

			ReadSequence("EmptyAttachedPropertyNode.xaml", sequence);
		}

		private string WriteTest(string filename)
		{
			var writer = new StringBuilder();
			var r = GetReader(filename);
			while (!r.IsEof)
			{
				r.Read();

				writer.Append($"new SequenceItem {{ NodeType = XamlNodeType.{r.NodeType}, ");

				if (r.Value != null)
				{
					writer.Append($"Value = {ToLiteral(r.Value.ToString())}, ");
				}

				if (r.Member != null)
				{
					writer.Append($"MemberType = \"{r.Member}\", ");
				}

				if (r.Type != null)
				{
					writer.Append($"TypeName = \"{r.Type}\"");
				}

				writer.Append($"}},");

				writer.AppendLine();
			}

			return writer.ToString();
		}

		private void ReadTest(string filename)
		{
			var r = GetReader(filename);
			while (!r.IsEof)
				r.Read();
		}

		XamlReader GetReader(string filename)
		{
			var directory = Path.GetDirectoryName(new Uri(GetType().Assembly.CodeBase).LocalPath);
			string xml = File.ReadAllText(Path.Combine(directory, "XmlFiles", filename)).Replace("System.Xaml_test_net_4_0", "Uno.Xaml.Tests");
			var xmlReader = XmlReader.Create(new StringReader(xml));
			return new XamlXmlReader(xmlReader);
		}


		private void ReadSequence(string fileName, IEnumerable<SequenceItem> sequence)
		{
			ReadTest(fileName);

			var r = GetReader(fileName);

			var e = sequence.GetEnumerator();
			while (e.MoveNext() && r.Read())
			{
				Assert.AreEqual(e.Current.NodeType, r.NodeType, $"{e.Current.SequenceMember}:{e.Current.SequenceLineNumber}");

				if (e.Current.MemberType != null)
				{
					Assert.AreEqual(e.Current.MemberType, r.Member);
				}
				if (e.Current.Value != null)
				{
					Assert.AreEqual(e.Current.Value, r.Value);
				}
				if (e.Current.TypeName != null)
				{
					Assert.AreEqual(e.Current.TypeName, r.Type?.ToString());
				}
			}
		}

		public static string ToLiteral(string input)
		{
			var literal = new StringBuilder(input.Length + 2);
			literal.Append("\"");
			foreach (var c in input)
			{
				switch (c)
				{
					case '\'': literal.Append(@"\'"); break;
					case '\"': literal.Append("\\\""); break;
					case '\\': literal.Append(@"\\"); break;
					case '\0': literal.Append(@"\0"); break;
					case '\a': literal.Append(@"\a"); break;
					case '\b': literal.Append(@"\b"); break;
					case '\f': literal.Append(@"\f"); break;
					case '\n': literal.Append(@"\n"); break;
					case '\r': literal.Append(@"\r"); break;
					case '\t': literal.Append(@"\t"); break;
					case '\v': literal.Append(@"\v"); break;
					default:
						if (Char.GetUnicodeCategory(c) != Globalization.UnicodeCategory.Control)
						{
							literal.Append(c);
						}
						else
						{
							literal.Append(@"\u");
							literal.Append(((ushort)c).ToString("x4"));
						}
						break;
				}
			}
			literal.Append("\"");
			return literal.ToString();
		}
	}
}
