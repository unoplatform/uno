#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Uno.Roslyn;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	/// <summary>
	/// Abstraction over the inputs/outputs the XAML generator needs at runtime.
	/// Decouples the generation pipeline from <see cref="GeneratorExecutionContext"/>
	/// so the same code can be driven from either the legacy <see cref="ISourceGenerator"/>
	/// execution path or an <see cref="IIncrementalGenerator"/> pipeline.
	/// </summary>
	internal sealed class XamlSourceContext
	{
		private readonly Func<string, string> _propertyLookup;
		private readonly Func<string, IEnumerable<Uno.Roslyn.MSBuildItem>> _itemsLookup;
		private readonly Action<Diagnostic> _reportDiagnostic;
		private readonly Action<string, SourceText> _addSource;
		private readonly Func<IEnumerable<AdditionalText>> _additionalFiles;

		public XamlSourceContext(
			Compilation compilation,
			CancellationToken cancellationToken,
			Func<string, string> propertyLookup,
			Func<string, IEnumerable<Uno.Roslyn.MSBuildItem>> itemsLookup,
			Func<IEnumerable<AdditionalText>> additionalFiles,
			Action<Diagnostic> reportDiagnostic,
			Action<string, SourceText> addSource)
		{
			Compilation = compilation;
			CancellationToken = cancellationToken;
			_propertyLookup = propertyLookup;
			_itemsLookup = itemsLookup;
			_additionalFiles = additionalFiles;
			_reportDiagnostic = reportDiagnostic;
			_addSource = addSource;
		}

		public Compilation Compilation { get; }

		public CancellationToken CancellationToken { get; }

		public IEnumerable<AdditionalText> AdditionalFiles => _additionalFiles();

		public string GetMSBuildPropertyValue(string name, string defaultValue = "")
		{
			var value = _propertyLookup(name);
			return string.IsNullOrEmpty(value) ? defaultValue : value;
		}

		public IEnumerable<Uno.Roslyn.MSBuildItem> GetMSBuildItemsWithAdditionalFiles(string name)
			=> _itemsLookup(name);

		public void ReportDiagnostic(Diagnostic diagnostic) => _reportDiagnostic(diagnostic);

		public void AddSource(string hintName, SourceText sourceText) => _addSource(hintName, sourceText);

		public void AddSource(string hintName, string source)
			=> _addSource(hintName, SourceText.From(source, System.Text.Encoding.UTF8));

		/// <summary>
		/// Builds a <see cref="XamlSourceContext"/> backed by the legacy
		/// <see cref="GeneratorExecutionContext"/> execution path.
		/// </summary>
		public static XamlSourceContext FromGeneratorContext(GeneratorExecutionContext context)
		{
			return new XamlSourceContext(
				compilation: context.Compilation,
				cancellationToken: context.CancellationToken,
				propertyLookup: name => context.GetMSBuildPropertyValue(name),
				itemsLookup: name => context.GetMSBuildItemsWithAdditionalFiles(name),
				additionalFiles: () => context.AdditionalFiles,
				reportDiagnostic: context.ReportDiagnostic,
				addSource: (hint, text) => context.AddSource(hint, text));
		}

		/// <summary>
		/// Builds a <see cref="XamlSourceContext"/> backed by an <see cref="IIncrementalGenerator"/>
		/// pipeline. MSBuild properties and per-file metadata are looked up against the
		/// <see cref="AnalyzerConfigOptionsProvider"/>; <see cref="ReportDiagnostic"/> and
		/// <see cref="AddSource"/> are routed onto a <see cref="SourceProductionContext"/>.
		/// </summary>
		public static XamlSourceContext FromIncrementalInputs(
			Compilation compilation,
			CancellationToken cancellationToken,
			ImmutableArray<AdditionalText> additionalFiles,
			AnalyzerConfigOptionsProvider optionsProvider,
			Action<Diagnostic> reportDiagnostic,
			Action<string, SourceText> addSource)
		{
			string LookupProperty(string name)
			{
				optionsProvider.GlobalOptions.TryGetValue("build_property." + name, out var value);
				return value ?? "";
			}

			IEnumerable<MSBuildItem> LookupItems(string sourceItemGroup)
			{
				foreach (var file in additionalFiles)
				{
					var options = optionsProvider.GetOptions(file);
					if (options.TryGetValue("build_metadata.AdditionalFiles.SourceItemGroup", out var group)
						&& group == sourceItemGroup)
					{
						yield return new MSBuildItem(file, name =>
						{
							optionsProvider.GetOptions(file).TryGetValue("build_metadata.AdditionalFiles." + name, out var v);
							return v ?? "";
						});
					}
				}
			}

			return new XamlSourceContext(
				compilation: compilation,
				cancellationToken: cancellationToken,
				propertyLookup: LookupProperty,
				itemsLookup: LookupItems,
				additionalFiles: () => additionalFiles,
				reportDiagnostic: reportDiagnostic,
				addSource: addSource);
		}
	}
}
