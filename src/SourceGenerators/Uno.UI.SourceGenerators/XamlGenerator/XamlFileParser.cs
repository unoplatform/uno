extern alias __uno;
#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using Microsoft.CodeAnalysis;
using Uno.Extensions;
using Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection;
using Uno.UI.SourceGenerators.XamlGenerator.Utils;
using Uno.Roslyn;
using Windows.Foundation.Metadata;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

#if NETFRAMEWORK
using GeneratorExecutionContext = Uno.SourceGeneration.GeneratorExecutionContext;
#endif

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal partial class XamlFileParser
	{
		private static readonly ConcurrentDictionary<CachedFileKey, CachedFile> _cachedFiles = new();
		private static readonly TimeSpan _cacheEntryLifetime = new TimeSpan(hours: 1, minutes: 0, seconds: 0);
		private readonly string _excludeXamlNamespacesProperty;
		private readonly string _includeXamlNamespacesProperty;
		private readonly string[] _excludeXamlNamespaces;
		private readonly string[] _includeXamlNamespaces;
		private readonly RoslynMetadataHelper _metadataHelper;
		private int _depth;

		public XamlFileParser(string excludeXamlNamespaces, string includeXamlNamespaces, RoslynMetadataHelper roslynMetadataHelper)
		{
			_excludeXamlNamespacesProperty = excludeXamlNamespaces;
			_excludeXamlNamespaces = excludeXamlNamespaces.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

			_includeXamlNamespacesProperty = includeXamlNamespaces;
			_includeXamlNamespaces = includeXamlNamespaces.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			
			_metadataHelper = roslynMetadataHelper;
		}

		public XamlFileDefinition[] ParseFiles(Uno.Roslyn.MSBuildItem[] xamlSourceFiles, CancellationToken cancellationToken)
		{
			return xamlSourceFiles
				.AsParallel()
				.WithCancellation(cancellationToken)
				.Select(f => ParseFile(f.File, cancellationToken))
				.Where(f => f != null)
				.ToArray()!;
		}

		private static void ClearCache()
		{
			_cachedFiles.Remove(kvp => DateTimeOffset.Now - kvp.Value.LastTimeUsed > _cacheEntryLifetime);
		}

		private XamlFileDefinition? ParseFile(AdditionalText file, CancellationToken cancellationToken)
		{
			try
			{
#if DEBUG
				Console.WriteLine("Pre-processing XAML file: {0}", file);
#endif

				var document = ApplyIgnorables(file, cancellationToken, out var sourceText);
				var cachedFileKey = new CachedFileKey(_includeXamlNamespacesProperty, _excludeXamlNamespacesProperty, file.Path, sourceText.GetChecksum());
				if (_cachedFiles.TryGetValue(cachedFileKey, out var cached))
				{
					_cachedFiles[cachedFileKey] = cached.WithUpdatedLastTimeUsed();
					ClearCache();
					return cached.XamlFileDefinition;
				}

				ClearCache();

				// Initialize the reader using an empty context, because when the tasl
				// is run under the BeforeCompile in VS IDE, the loaded assemblies are used 
				// to interpret the meaning of objects, which is not correct in Uno.UI context.
				var context = new XamlSchemaContext(Enumerable.Empty<Assembly>());

				// Force the line info, otherwise it will be enabled only when the debugger is attached.
				var settings = new XamlXmlReaderSettings() { ProvideLineInfo = true };

				using (var reader = new XamlXmlReader(document, context, settings))
				{
					if (reader.Read())
					{
						cancellationToken.ThrowIfCancellationRequested();

						var xamlFileDefinition = Visit(reader, file.Path);
						_cachedFiles[cachedFileKey] = new CachedFile(DateTimeOffset.Now, xamlFileDefinition);
						return xamlFileDefinition;
					}
				}

				return null;
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (__uno::Uno.Xaml.XamlParseException e)
			{
				throw new XamlParsingException(e.Message, null, e.LineNumber, e.LinePosition, file.Path);
			}
			catch (XmlException e)
			{
				throw new XamlParsingException(e.Message, null, e.LineNumber, e.LinePosition, file.Path);
			}
			catch (Exception e)
			{
				throw new XamlParsingException($"Failed to parse file", e, 1, 1, file.Path);
			}
		}

		private XmlReader ApplyIgnorables(AdditionalText file, CancellationToken cancellationToken, out SourceText sourceTextOut)
		{
			var sourceText = file.GetText(cancellationToken)!;
			if (sourceText is null)
			{
				throw new Exception($"Failed to read additional file '{file.Path}'");
			}

			sourceTextOut = sourceText;

			var originalString = sourceText.ToString();
			StringBuilder adjusted;

			var document = new XmlDocument();
			document.LoadXml(originalString);

			var (ignorables, shouldCreateIgnorable) = FindIgnorables(document);
			var conditionals = FindConditionals(document);

			shouldCreateIgnorable |= conditionals.ExcludedConditionals.Count > 0;

			var hasxBind = originalString.Contains("{x:Bind", StringComparison.Ordinal);

			if (ignorables == null && !shouldCreateIgnorable && !hasxBind)
			{
				// No need to modify file
				return XmlReader.Create(new StringReader(originalString));
			}

			var originalIgnorables = ignorables?.Value ?? "";

			var ignoredNs = originalIgnorables.Split(' ');

			var newIgnored = ignoredNs
				.Except(_includeXamlNamespaces)
				.Concat(_excludeXamlNamespaces.Where(n => document.DocumentElement?.GetNamespaceOfPrefix(n).HasValue() ?? false))
				.Concat(conditionals.ExcludedConditionals.Select(a => a.LocalName))
				.ToArray();
			var newIgnoredFlat = newIgnored.JoinBy(" ");

			if (ignorables != null)
			{
				ignorables.Value = newIgnoredFlat;

#if DEBUG
				Console.WriteLine("Ignorable XAML namespaces: {0} for {1}", ignorables.Value, file);
#endif
				adjusted = new StringBuilder(originalString);

				// change the namespaces using textreplace, to keep the formatting and have proper
				// line/position reporting.
				adjusted
					.Replace(
						"Ignorable=\"{0}\"".InvariantCultureFormat(originalIgnorables),
						"Ignorable=\"{0}\"".InvariantCultureFormat(ignorables.Value)
					);
			}
			else
			{
				// No existing Ignorable node, create one
				var targetLine = sourceText.Lines.Select(l => sourceText.ToString(l.Span)).First(l => !l.IsNullOrWhiteSpace() && !l.Trim().StartsWith("<!"))!;
				if (targetLine.EndsWith(">"))
				{
					targetLine = targetLine.TrimEnd(">");
				}

				var mcName = document.DocumentElement?
					.Attributes
					.Cast<XmlAttribute>()
					.FirstOrDefault(a => a.Prefix == "xmlns" && a.Value == "http://schemas.openxmlformats.org/markup-compatibility/2006")
					?.LocalName;

				var mcString = "";
				if (mcName == null)
				{
					mcName = "mc";
					mcString = " xmlns:mc=\"http://schemas.openxmlformats.org/markup-compatibility/2006\"";
				}

				var replacement = "{0}{1} {2}:Ignorable=\"{3}\"".InvariantCultureFormat(targetLine, mcString, mcName, newIgnoredFlat);
				adjusted = ReplaceFirst(
					originalString,
					targetLine,
					replacement
				);
			}

			// Replace the ignored namespaces with unique urns so that same urn that are placed in Ignored attribute
			// are ignored independently.
			foreach (var n in newIgnored)
			{
				adjusted
					.Replace(
						"xmlns:{0}=\"{1}\"".InvariantCultureFormat(n, document.DocumentElement?.GetNamespaceOfPrefix(n)),
						"xmlns:{0}=\"{1}\"".InvariantCultureFormat(n, Guid.NewGuid())
					);
			}

			// Put all the included namespaces in the same default namespace, so that the properties get their
			// DeclaringType properly set.
			foreach (var n in _includeXamlNamespaces)
			{
				if (document.DocumentElement != null)
				{
					var originalPrefix = document.DocumentElement.GetNamespaceOfPrefix(n);

					if (!originalPrefix.StartsWith("using:"))
					{
						adjusted
							.Replace(
								"xmlns:{0}=\"{1}\"".InvariantCultureFormat(n, document.DocumentElement.GetNamespaceOfPrefix(n)),
								"xmlns:{0}=\"{1}\"".InvariantCultureFormat(n, document.DocumentElement.GetNamespaceOfPrefix(""))
							);
					}
				}
			}

			foreach (var includedCond in conditionals.IncludedConditionals)
			{
				var valueSplit = includedCond.Value.Split('?');
				// Strip the conditional part, so the namespace can be parsed correctly by the Xaml reader
				adjusted
					.Replace(
						includedCond.OuterXml,
						"{0}=\"{1}\"".InvariantCultureFormat(includedCond.Name, valueSplit[0])
					);
			}

			if (hasxBind)
			{
				// Apply replacements to avoid having issues with the XAML parser which does not
				// support quotes in positional markup extensions parameters.
				// Note that the UWP preprocessor does not need to apply those replacements as the
				// x:Bind expressions are being removed during the first phase and replaced by "connections".
				adjusted = new(XBindExpressionParser.RewriteDocumentPaths(adjusted.ToString()));
			}

			return XmlReader.Create(new StringReader(adjusted.ToString().TrimEnd("\r\n")));
		}

		private static StringBuilder ReplaceFirst(string targetString, string oldValue, string newValue)
		{
			var index = targetString.IndexOf(oldValue, StringComparison.InvariantCulture);
			if (index < 0)
			{
				throw new InvalidOperationException();
			}

			var result = new StringBuilder(targetString.Length + newValue.Length);

			result.Append(targetString, 0, index);
			result.Append(newValue);

			var secondBlockStart = index + oldValue.Length;
			result.Append(targetString, secondBlockStart, targetString.Length - secondBlockStart);

			return result;
		}

		private (XmlNode? Ignorables, bool ShouldCreateIgnorable) FindIgnorables(XmlDocument document)
		{
			var ignorables = document.DocumentElement?.Attributes.GetNamedItem("Ignorable", "http://schemas.openxmlformats.org/markup-compatibility/2006") as XmlAttribute;

			var excludeNamespaces = _excludeXamlNamespaces
				.Select(n => new { Name = n, Namespace = document.DocumentElement?.GetNamespaceOfPrefix(n) })
				.Where(n => n.Namespace.HasValue());

			var shouldCreateIgnorable = false;

			foreach (var nspace in excludeNamespaces)
			{
				var excludeNodes = document
					.DocumentElement
					?.SelectNodes("//* | //@*")
					?.OfType<XmlNode>()
					.Where(e => e.Prefix == nspace.Name);

				if (ignorables == null && (excludeNodes?.Any() ?? false))
				{
					shouldCreateIgnorable = true;
				}
			}

			return (ignorables, shouldCreateIgnorable);
		}

		/// <summary>
		/// Returns those XAML namespace definitions for which a conditional is set, grouped by those for which the conditional returns true and
		/// should be included, and those for which it returns fales and should be excluded.
		/// </summary>
		private (List<XmlAttribute> IncludedConditionals, List<XmlAttribute> ExcludedConditionals) FindConditionals(XmlDocument document)
		{
			var included = new List<XmlAttribute>();
			var excluded = new List<XmlAttribute>();

			foreach (XmlAttribute attr in document.DocumentElement!.Attributes)
			{
				if (attr.Prefix != "xmlns")
				{
					// Not a namespace
					continue;
				}

				var valueSplit = attr.Value.Split('?');
				if (valueSplit.Length != 2)
				{
					// Not a (valid) conditional
					continue;
				}

				if (ShouldInclude() is bool shouldInclude)
				{
					if (shouldInclude)
					{
						included.Add(attr);
					}
					else
					{
						excluded.Add(attr);
					}
				}

				bool? ShouldInclude()
				{
					var elements = valueSplit[1].Split('(', ',', ')');

					var methodName = elements[0];

					switch (methodName)
					{
						case nameof(ApiInformation.IsApiContractPresent):
						case nameof(ApiInformation.IsApiContractNotPresent):
							if (elements.Length < 4 || !ushort.TryParse(elements[2].Trim(), out var majorVersion))
							{
								throw new InvalidOperationException($"Syntax error while parsing conditional namespace expression {attr.Value}");
							}

							return methodName == nameof(ApiInformation.IsApiContractPresent) ?
								ApiInformation.IsApiContractPresent(elements[1], majorVersion) :
								ApiInformation.IsApiContractNotPresent(elements[1], majorVersion);
						case nameof(ApiInformation.IsTypePresent):
						case nameof(ApiInformation.IsTypeNotPresent):
							if (elements.Length < 2)
							{
								throw new InvalidOperationException($"Syntax error while parsing conditional namespace expression {attr.Value}");
							}
							var expectedType = elements[1];
							return methodName == nameof(ApiInformation.IsTypePresent) ?
								ApiInformation.IsTypePresent(elements[1], _metadataHelper) :
								ApiInformation.IsTypeNotPresent(elements[1], _metadataHelper);
						default:
							return null;// TODO: support IsPropertyPresent
					}
				}
			}

			return (included, excluded);
		}

		private XamlFileDefinition Visit(XamlXmlReader reader, string file)
		{
			WriteState(reader);

			var xamlFile = new XamlFileDefinition(file);

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

		private XamlObjectDefinition VisitObject(XamlXmlReader reader, XamlObjectDefinition? owner)
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
							var run = ConvertLiteralInlineTextToRun(reader);
							member.Objects.Add(run);
						}
						else
						{
							if (member.Value is string s)
							{
								member.Value += ", " + reader.Value;
							}
							else
							{
								member.Value = reader.Value;
							}
						}
						break;

					case XamlNodeType.StartObject:
						_depth++;
						member.Objects.Add(VisitObject(reader, owner));
						break;

					case XamlNodeType.EndObject:
						_depth--;
						break;

					case XamlNodeType.NamespaceDeclaration:
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
					xamlObject.Type.Name is "TextBlock"
						or "Bold"
						or "Hyperlink"
						or "Italic"
						or "Underline"
						or "Span"
						or "Paragraph"
				)
				&& (member.Member.Name == "_UnknownContent" || member.Member.Name == "Inlines");
		}

		private XamlObjectDefinition ConvertLiteralInlineTextToRun(XamlXmlReader reader)
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
						Value = reader.Value
					}
				}
			};
		}
	}
}
