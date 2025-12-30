extern alias __uno;
#nullable enable
using System.Diagnostics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Uno.Extensions;
using Uno.UI.SourceGenerators.XamlGenerator.XamlRedirection;
using Uno.UI.SourceGenerators.XamlGenerator.Utils;
using Uno.Roslyn;
using Windows.Foundation.Metadata;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal partial class XamlFileParser
	{
		private static readonly ConcurrentDictionary<CachedFileKey, CachedFile> _cachedFiles = new();
		private static readonly TimeSpan _cacheEntryLifetime = new TimeSpan(hours: 1, minutes: 0, seconds: 0);
		private static readonly char[] _splitChars = new char[] { '(', ',', ')' };
		private static readonly XamlType _runXamlType = new XamlType(
			XamlConstants.PresentationXamlXmlNamespace,
			"Run",
			new List<XamlType>(),
			new XamlSchemaContext()
		);
		private readonly string _excludeXamlNamespacesProperty;
		private readonly string _includeXamlNamespacesProperty;
		private readonly string[] _excludeXamlNamespaces;
		private readonly string[] _includeXamlNamespaces;

		/// <summary>
		/// Usages of this field should be careful, since we avoid xaml parse caching if we use it.
		/// If the usage affects the parse result, you need to disable caching for the file.
		/// </summary>
		private readonly RoslynMetadataHelper _metadataHelper;

		private int _depth;

		public XamlFileParser(string excludeXamlNamespacesProperty, string includeXamlNamespacesProperty, string[] excludeXamlNamespaces, string[] includeXamlNamespaces, RoslynMetadataHelper roslynMetadataHelper)
		{
			_excludeXamlNamespacesProperty = excludeXamlNamespacesProperty;
			_excludeXamlNamespaces = excludeXamlNamespaces;

			_includeXamlNamespacesProperty = includeXamlNamespacesProperty;
			_includeXamlNamespaces = includeXamlNamespaces;

			_metadataHelper = roslynMetadataHelper;
		}

		public ParallelQuery<XamlFileDefinition> ParseFiles(IEnumerable<XamlSource> xamlSourceFiles, string projectDirectory, CancellationToken ct)
		{
			return xamlSourceFiles
				.AsParallel()
				.WithCancellation(ct)
				.Select(src => ParseFile(src, GetTargetFilePath(src.Item).Replace("\\", "/"), ct))
				.Where(fileResult => fileResult is not null)!;

			string GetTargetFilePath(MSBuildItem fileItem)
			{
				// Generate an actual TargetPath to be used with BaseUri, so that
				// it maps to the actual path in the app package.

				if (fileItem.GetMetadataValue("TargetPath") is { Length: > 0 } targetPath)
				{
					return targetPath;
				}

				if (fileItem.GetMetadataValue("Link") is { Length: > 0 } link)
				{
					return link;
				}

				return fileItem.GetMetadataValue("Identity").Replace(projectDirectory, "");
			}
		}

		private static void ScavengeCache()
		{
			// DateTimeOffset.Now might be expensive.
			// Investigate if using Environment.TickCount can work and is faster.
			var now = DateTimeOffset.Now;
			_cachedFiles.Remove(kvp => now - kvp.Value.LastTimeUsed > _cacheEntryLifetime);
		}

		private XamlFileDefinition? ParseFile(XamlSource src, string targetFilePath, CancellationToken ct)
		{
			try
			{
				var sourcePath = src.Item.File.Path;
				var sourceText = src.Item.File.GetText(ct) ?? throw new Exception($"Failed to read additional file '{sourcePath}'");
				var sourceLink = src.Link.Replace('\\', '/');

				var cachedFileKey = new CachedFileKey(_includeXamlNamespacesProperty, _excludeXamlNamespacesProperty, sourcePath, sourceLink, sourceText.GetChecksum());
				if (_cachedFiles.TryGetValue(cachedFileKey, out var cached))
				{
					_cachedFiles[cachedFileKey] = cached.WithUpdatedLastTimeUsed();
					ScavengeCache();
					return cached.XamlFileDefinition;
				}

				ScavengeCache();

				// Initialize the reader using an empty context, because when the tasl
				// is run under the BeforeCompile in VS IDE, the loaded assemblies are used
				// to interpret the meaning of objects, which is not correct in Uno.UI context.
				var context = new XamlSchemaContext([]);

				// Force the line info, otherwise it will be enabled only when the debugger is attached.
				var settings = new XamlXmlReaderSettings { ProvideLineInfo = true };

				var document = RewriteForXBind(sourceText);

				using var reader = new XamlXmlReader(document, context, settings, IsIncluded);
				if (!reader.Read())
				{
					return null; // File is empty
				}

				ct.ThrowIfCancellationRequested();

				var xamlFileDefinition = new XamlFileDefinition(sourcePath, sourceLink, targetFilePath, sourceText.ToString(), sourceText.GetChecksum());
				var ctx = new XamlFileParserContext(sourcePath);
				try
				{
					VisitRoot(reader, ref xamlFileDefinition, ref ctx, ct);
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (__uno::Uno.Xaml.XamlParseException e)
				{
					ctx.ReportError(e.Message, e.LineNumber, e.LinePosition, null /* Do not forward inner */);
				}
				catch (XmlException e)
				{
					ctx.ReportError(e.Message, e.LineNumber, e.LinePosition, null /* Do not forward inner */);
				}
				catch (Exception error)
				{
					ctx.ReportError(
						$"Unable to process {reader.NodeType} node at Line {reader.LineNumber}, position {reader.LinePosition}",
						reader.LineNumber,
						reader.LinePosition,
						error);
				}

				xamlFileDefinition = xamlFileDefinition with
				{
					ParsingErrors = ctx.GetErrors()
				};
				if (!reader.DisableCaching)
				{
					_cachedFiles[cachedFileKey] = new CachedFile(DateTimeOffset.Now, xamlFileDefinition);
				}

				return xamlFileDefinition;
			}
			catch (Exception e) when (e is not OperationCanceledException)
			{
				// Catastrophic failure, we are unable to parse the file at all (i.e. no XamlFileDefinition).
				// This will prevent generation of **ALL** XAML files and should be avoided as much as possible.
				throw new XamlParsingException("Failed to parse file", e, 1, 1, src.Item.File.Path);
			}
		}

		private XmlReader RewriteForXBind(SourceText sourceText)
		{
			var originalString = sourceText.ToString();

			var hasxBind = originalString.Contains("{x:Bind", StringComparison.Ordinal);

			if (!hasxBind)
			{
				// No need to modify file
				return XmlReader.Create(new StringReader(originalString));
			}

			// Apply replacements to avoid having issues with the XAML parser which does not
			// support quotes in positional markup extensions parameters.
			// Note that the UWP preprocessor does not need to apply those replacements as the
			// x:Bind expressions are being removed during the first phase and replaced by "connections".
			var adjusted = XBindExpressionParser.RewriteDocumentPaths(originalString);

			return XmlReader.Create(new StringReader(adjusted));
		}

		private static bool IsSkiaNotConditional(string localName, string namespaceUri)
		{
			// Not ideal, but we want to avoid breaking changes.
			// See discussion at https://github.com/unoplatform/uno/issues/17028
			// For xmlns that are named "skia", there are 3 scenarios:
			// 1. If project being built is for Skia, we just include that element.
			// 2. If project being built is not for Skia and namespaceUri is using SkiaSharp, it's not conditional XAML and we should include that element
			// 3. If project being built is not for Skia and namespaceUri is not using SkiaSharp, this is conditional XAML.
			// For case 1, IsIncluded will return ForceInclude
			// For case 2, we'll go through regular rules, as with any xmlns (rely on Ignorable and normal XAML conditional rules).
			// For case 3, this method will return false and IsIncluded will return ForceExclude.
			// Above explains the "current" behavior meant by this code.
			// The ideal behavior is really that we ignore localName completely and just rely on namespaceUris
			// NOTE: We check StartsWith("using") as well as Contains("SkiaSharp") to avoid breaking scenarios like
			// xmlns:skia="http://uno.ui/skia#using:SkiaSharp.Views.Windows"
			return localName == "skia" &&
				namespaceUri.StartsWith("using:", StringComparison.Ordinal) &&
				namespaceUri.Contains("SkiaSharp", StringComparison.Ordinal);
		}

		private __uno::Uno.Xaml.IsIncludedResult IsIncluded(string localName, string namespaceUri)
		{
			if (_includeXamlNamespaces.Contains(localName))
			{
				var result = __uno::Uno.Xaml.IsIncludedResult.ForceInclude;
				return namespaceUri.Contains("using:")
					? result
					: result.WithUpdatedNamespace(XamlConstants.PresentationXamlXmlNamespace);
			}
			else if (_excludeXamlNamespaces.Contains(localName) && !IsSkiaNotConditional(localName, namespaceUri))
			{
				return __uno::Uno.Xaml.IsIncludedResult.ForceExclude;
			}

			var valueSplit = namespaceUri.Split('?');
			if (valueSplit.Length != 2)
			{
				// Not a (valid) conditional
				return __uno::Uno.Xaml.IsIncludedResult.Default;
			}

			namespaceUri = valueSplit[0];
			var elements = valueSplit[1].Split(_splitChars);

			var methodName = elements[0];

			switch (methodName)
			{
				case nameof(ApiInformation.IsApiContractPresent):
				case nameof(ApiInformation.IsApiContractNotPresent):
					if (elements.Length < 4 || !ushort.TryParse(elements[2].Trim(), out var majorVersion))
					{
						throw new InvalidOperationException($"Syntax error while parsing conditional namespace expression {namespaceUri}");
					}

					var isIncluded1 = methodName == nameof(ApiInformation.IsApiContractPresent) ?
						ApiInformation.IsApiContractPresent(elements[1], majorVersion) :
						ApiInformation.IsApiContractNotPresent(elements[1], majorVersion);
					return (isIncluded1
						? __uno::Uno.Xaml.IsIncludedResult.ForceInclude
						: __uno::Uno.Xaml.IsIncludedResult.ForceExclude).WithUpdatedNamespace(namespaceUri);
				case nameof(ApiInformation.IsTypePresent):
				case nameof(ApiInformation.IsTypeNotPresent):
					if (elements.Length < 2)
					{
						throw new InvalidOperationException($"Syntax error while parsing conditional namespace expression {namespaceUri}");
					}
					var expectedType = elements[1];
					var isIncluded2 = methodName == nameof(ApiInformation.IsTypePresent) ?
						ApiInformation.IsTypePresent(elements[1], _metadataHelper) :
						ApiInformation.IsTypeNotPresent(elements[1], _metadataHelper);
					return (isIncluded2
						? __uno::Uno.Xaml.IsIncludedResult.ForceIncludeWithCacheDisabled
						: __uno::Uno.Xaml.IsIncludedResult.ForceExclude).WithUpdatedNamespace(namespaceUri);
				default:
					return __uno::Uno.Xaml.IsIncludedResult.Default.WithUpdatedNamespace(namespaceUri); // TODO: support IsPropertyPresent
			}
		}

		private void VisitRoot(XamlXmlReader reader, ref XamlFileDefinition xamlFile, ref XamlFileParserContext ctx, in CancellationToken ct)
		{
			do
			{
				WriteState(reader);
				ct.ThrowIfCancellationRequested();

				switch (reader.NodeType)
				{
					case XamlNodeType.StartObject:
						_depth++;
						var root = new XamlObjectDefinition(reader, xamlFile);
						xamlFile.Objects.Add(root); // In order to keep alive parsed content if an exception occurs, we make sure to add the child before continuing parsing.
						VisitObject(reader, ref root, ref ctx, ct);
						break;

					case XamlNodeType.NamespaceDeclaration:
						xamlFile.Namespaces.Add(reader.Namespace);
						break;

					default:
						throw new InvalidOperationException($"Unexpected token ({reader.NodeType})");
				}
			}
			while (reader.Read());
		}

		private void WriteState(XamlXmlReader reader)
		{
			// Console.WriteLine(
			//	$"{new string(' ', Math.Max(0,_depth))}{reader.NodeType} {reader.Type} {reader.Member} {reader.Value}"
			// );
		}

		private void VisitObject(XamlXmlReader reader, ref XamlObjectDefinition xamlObject, ref XamlFileParserContext ctx, in CancellationToken ct)
		{
			while (reader.Read())
			{
				WriteState(reader);
				ct.ThrowIfCancellationRequested();

				switch (reader.NodeType)
				{
					case XamlNodeType.StartMember:
						_depth++;
						var pos = reader.LinePosition;
						XamlMemberDefinition? member;

						// When missing commas in Binding expressions, the parser may create members with spaces in their name.
						if (reader.Member.Name.Split([' '], StringSplitOptions.RemoveEmptyEntries) is { Length: > 1 } tokens)
						{
							// We create a positional members for each part except the last one.
							foreach (var token in tokens.SkipLast(1))
							{
								var incompleteMember = new XamlMemberDefinition(new XamlMember(XamlConstants.PositionalParameters, reader.Member.DeclaringType, reader.Member.IsAttachable), reader.LineNumber, pos, xamlObject);
								incompleteMember.Value = token;
								xamlObject.Members.Add(incompleteMember);
								pos += token.Length;
								ctx.ReportError($"Missing comma between members of '{xamlObject.Type.Name}'", reader.LineNumber, pos);
								pos++; // +1 for the space
							}

							// And init the actual member (the one which will be visited) with the last token.
							member = new XamlMemberDefinition(new XamlMember(tokens.Last(), reader.Member.DeclaringType, reader.Member.IsAttachable), reader.LineNumber, pos, xamlObject);
						}
						else
						{
							member = new XamlMemberDefinition(reader.Member, reader.LineNumber, pos, xamlObject);
						}

						xamlObject.Members.Add(member); // In order to keep alive parsed content if an exception occurs, we make sure to add the member before continuing parsing.
						VisitMember(reader, ref member, ref ctx, ct);
						break;

					case XamlNodeType.StartObject:
						_depth++;
						var child = new XamlObjectDefinition(reader, xamlObject);
						xamlObject.Objects.Add(child); // In order to keep alive parsed content if an exception occurs, we make sure to add the child before continuing parsing.
						VisitObject(reader, ref child, ref ctx, ct);
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
						throw new InvalidOperationException($"Unexpected token ({reader.NodeType})");
				}
			}
		}

		private void VisitMember(XamlXmlReader reader, ref XamlMemberDefinition member, ref XamlFileParserContext ctx, in CancellationToken ct)
		{
			var lastWasLiteralInline = false;
			var lastWasTrimSurroundingWhiteSpace = false;
			List<NamespaceDeclaration>? namespaces = null;

			while (reader.Read())
			{
				WriteState(reader);
				ct.ThrowIfCancellationRequested();

				switch (reader.NodeType)
				{
					case XamlNodeType.EndMember:
						_depth--;
						return;

					case XamlNodeType.Value:
						if (IsLiteralInlineText(reader.Value, member))
						{
							var run = ConvertLiteralInlineTextToRun(reader, member, trimStart: !reader.PreserveWhitespace && lastWasTrimSurroundingWhiteSpace);
							member.Objects.Add(run);
							lastWasLiteralInline = true;
							lastWasTrimSurroundingWhiteSpace = false;
						}
						else
						{
							lastWasLiteralInline = false;
							lastWasTrimSurroundingWhiteSpace = false;
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

						var obj = new XamlObjectDefinition(reader, member.Owner, namespaces);
						member.Objects.Add(obj); // In order to keep alive parsed content if an exception occurs, we make sure to add the object before continuing parsing.
						VisitObject(reader, ref obj, ref ctx, ct);

						if (!reader.PreserveWhitespace &&
							lastWasLiteralInline &&
							obj.Type.TrimSurroundingWhitespace &&
							member.Objects.Count >= 2 && // 2 because we just added the `obj` in the list
							member.Objects[member.Objects.Count - 2].Members.Single() is { Value: string previousValue } runDefinition)
						{
							runDefinition.Value = previousValue.TrimEnd();
						}

						lastWasLiteralInline = false;
						lastWasTrimSurroundingWhiteSpace = obj.Type.TrimSurroundingWhitespace;
						break;

					case XamlNodeType.EndObject:
						lastWasLiteralInline = false;
						lastWasTrimSurroundingWhiteSpace = false;
						_depth--;
						break;

					case XamlNodeType.NamespaceDeclaration:
						lastWasLiteralInline = false;
						lastWasTrimSurroundingWhiteSpace = false;
						(namespaces ??= new List<NamespaceDeclaration>()).Add(reader.Namespace);
						// Skip
						break;

					default:
						throw new InvalidOperationException($"Unexpected token ({reader.NodeType})");
				}
			}
		}

		private static bool IsLiteralInlineText(object value, XamlMemberDefinition member)
			=> value is string
				&& member.Owner.Type.Name is "TextBlock"
					or "Bold"
					or "Hyperlink"
					or "Italic"
					or "Underline"
					or "Span"
					or "Paragraph"
				&& member.Member.Name is "_UnknownContent"
					or "Inlines";

		private XamlObjectDefinition ConvertLiteralInlineTextToRun(XamlXmlReader reader, XamlMemberDefinition member, bool trimStart)
		{
			var run = new XamlObjectDefinition(reader, member.Owner) { Type = _runXamlType };
			var runText = new XamlMemberDefinition(new XamlMember("Text", _runXamlType, false), reader.LineNumber, reader.LinePosition, run)
			{
				Value = trimStart ? ((string)reader.Value).TrimStart() : reader.Value
			};
			run.Members.Add(runText);

			return run;
		}
	}
}
