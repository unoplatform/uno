using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Uno.Extensions;
using Uno.Foundation.Logging;
using System.IO;
using System.Reflection;
using Uno.Xaml;

namespace Windows.UI.Xaml.Markup.Reader
{
	internal class XamlStringParser
	{
		private int _depth;

		public XamlStringParser()
		{
		}

		public XamlFileDefinition Parse(string content)
		{
			var document = XmlReader.Create(new StringReader(content));

			// Initialize the reader using an empty context, because when the tasl
			// is run under the BeforeCompile in VS IDE, the loaded assemblies are used 
			// to interpret the meaning of objects, which is not correct in Uno.UI context.
			var context = new XamlSchemaContext(Enumerable.Empty<Assembly>());

			// Force the line info, otherwise it will be enabled only when the debugger is attached.
			var settings = new XamlXmlReaderSettings() { ProvideLineInfo = true };

			using (var reader = new XamlXmlReader(document, context, settings, (_, _) => IsIncludedResult.Default))
			{
				if (reader.Read())
				{
					return Visit(reader);
				}
			}

			return null;
		}

		private XamlFileDefinition Visit(XamlXmlReader reader)
		{
			WriteState(reader);

			var xamlFile = new XamlFileDefinition();

			do
			{
				switch (reader.NodeType)
				{
					case XamlNodeType.StartObject:
						_depth++;
						xamlFile.Objects.Add(VisitObject(reader, null));
						break;

					case XamlNodeType.NamespaceDeclaration:
						xamlFile.Namespaces.Add(reader.Namespace);
						break;

					default:
						throw new InvalidOperationException();
				}
			}
			while (reader.Read());

			return xamlFile;
		}

		private void WriteState(XamlXmlReader reader)
		{
			// Console.WriteLine(
			//	$"{new string(' ', Math.Max(0,_depth))}{reader.NodeType} {reader.Type} {reader.Member} {reader.Value}"
			// );
		}

		private XamlObjectDefinition VisitObject(XamlXmlReader reader, XamlObjectDefinition owner)
		{
			var xamlObject = new XamlObjectDefinition(reader.Type, reader.LineNumber, reader.LinePosition, owner);

			Visit(reader, xamlObject);

			return xamlObject;
		}

		private void Visit(XamlXmlReader reader, XamlObjectDefinition xamlObject)
		{
			while (reader.Read())
			{
				WriteState(reader);

				switch (reader.NodeType)
				{
					case XamlNodeType.StartMember:
						_depth++;
						xamlObject.Members.Add(VisitMember(reader, xamlObject));
						break;

					case XamlNodeType.StartObject:
						_depth++;
						xamlObject.Objects.Add(VisitObject(reader, xamlObject));
						break;

					case XamlNodeType.Value:
						xamlObject.Value = reader.Value;
						break;

					case XamlNodeType.EndObject:
						_depth--;
						return;

					case XamlNodeType.EndMember:
						_depth--;
						break;

					default:
						throw new InvalidOperationException();
				}
			}
		}

		private XamlMemberDefinition VisitMember(XamlXmlReader reader, XamlObjectDefinition owner)
		{
			var member = new XamlMemberDefinition(reader.Member, reader.LineNumber, reader.LinePosition, owner);
			var lastWasLiteralInline = false;
			var lastWasTrimSurroundingWhiteSpace = false;
			while (reader.Read())
			{
				WriteState(reader);

				switch (reader.NodeType)
				{
					case XamlNodeType.EndMember:
						_depth--;
						return member;

					case XamlNodeType.Value:
						if (IsLiteralInlineText(reader.Value, member, owner))
						{
							var run = ConvertLiteralInlineTextToRun(reader, trimStart: !reader.PreserveWhitespace && lastWasTrimSurroundingWhiteSpace);
							member.Objects.Add(run);
							lastWasLiteralInline = true;
							lastWasTrimSurroundingWhiteSpace = false;
						}
						else
						{
							lastWasLiteralInline = false;
							lastWasTrimSurroundingWhiteSpace = false;
							member.Value = reader.Value;
						}
						break;

					case XamlNodeType.StartObject:
						_depth++;
						var obj = VisitObject(reader, owner);
						if (!reader.PreserveWhitespace &&
							lastWasLiteralInline &&
							obj.Type.TrimSurroundingWhitespace &&
							member.Objects.Count > 0 &&
							member.Objects[member.Objects.Count - 1].Members.Single() is { Value: string previousValue } runDefinition)
						{
							runDefinition.Value = previousValue.TrimEnd();
						}

						lastWasLiteralInline = false;
						lastWasTrimSurroundingWhiteSpace = obj.Type.TrimSurroundingWhitespace;
						member.Objects.Add(obj);
						break;

					case XamlNodeType.EndObject:
						lastWasLiteralInline = false;
						lastWasTrimSurroundingWhiteSpace = false;
						_depth--;
						break;

					case XamlNodeType.NamespaceDeclaration:
						lastWasLiteralInline = false;
						lastWasTrimSurroundingWhiteSpace = false;
						// Skip
						break;

					default:
						throw new InvalidOperationException("Unable to process {2} node at Line {0}, position {1}".InvariantCultureFormat(reader.LineNumber, reader.LinePosition, reader.NodeType));
				}
			}

			return member;
		}

		private static bool IsLiteralInlineText(object value, XamlMemberDefinition member, XamlObjectDefinition xamlObject)
		{
			return value is string
				&& (
					xamlObject.Type.Name is nameof(Controls.TextBlock)
						or nameof(Documents.Bold)
						or nameof(Documents.Hyperlink)
						or nameof(Documents.Italic)
						or nameof(Documents.Underline)
						or nameof(Documents.Span)
						or nameof(Documents.Paragraph)
				)
				&& (member.Member.Name == "_UnknownContent" || member.Member.Name == "Inlines");
		}

		private XamlObjectDefinition ConvertLiteralInlineTextToRun(XamlXmlReader reader, bool trimStart)
		{
			var runType = new XamlType(
				XamlConstants.PresentationXamlXmlNamespace,
				"Run",
				new List<XamlType>(),
				new XamlSchemaContext()
			);

			var textMember = new XamlMember("Text", runType, false);

			return new XamlObjectDefinition(runType, reader.LineNumber, reader.LinePosition, null)
			{
				Members =
				{
					new XamlMemberDefinition(textMember, reader.LineNumber, reader.LinePosition)
					{
						Value = trimStart ? ((string)reader.Value).TrimStart() : reader.Value
					}
				}
			};
		}
	}
}
