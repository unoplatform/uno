#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Uno;
using Uno.Extensions;
using Uno.UI.SourceGenerators.XamlGenerator.Utils;

namespace Uno.UI.SourceGenerators.XamlGenerator
{

	internal class XamlLazyApplyBlockIIndentedStringBuilder : IIndentedStringBuilder, IDisposable
	{
		private bool _applyOpened;
		private readonly IIndentedStringBuilder _source;
		private readonly NameScope _scope;
		private readonly IndentedStringBuilder _inner = new();
		private IDisposable? _applyDisposable;
		private readonly string? _xamlApplyPrefix;
		private readonly string? _delegateType;
		private readonly bool _exposeContext;
		private readonly string? _exposeContextMethod;
		private readonly string _appliedType;

		/// <summary>
		/// Name of the callback method generated in the scope that is being used for this apply, if any.
		/// </summary>
		public string? MethodName => _exposeContextMethod;

		/// <summary>
		/// The name of the element on which this apply method will be applied.
		/// </summary>
		public string AppliedParameterName => "__p1";

		/// <summary>
		/// 
		/// </summary>
		/// <param name="source"></param>
		/// <param name="scope">The scope into which this apply method is being generated.</param>
		/// <param name="declaringObject">The XAML object definition for which this apply method is being generated.</param>
		/// <param name="appliedType">Type of the <see cref="AppliedParameterName"/>.</param>
		/// <param name="xamlApplyPrefix">Enable strongly typed XamlApply with the given method prefix.</param>
		/// <param name="delegateType"></param>
		/// <param name="exposeContext">Indicates if the scope should be provided as '__that' parameter in the callback method.</param>
		public XamlLazyApplyBlockIIndentedStringBuilder(
			IIndentedStringBuilder source,
			NameScope scope,
			XamlObjectDefinition declaringObject,
			INamedTypeSymbol? appliedType, // Note: Applied type might be different that the declaringObject.Type when declared object is wrapped into a another type, like for the ElementSub (deferred loading).
			string? xamlApplyPrefix,
			string? delegateType,
			bool exposeContext)
		{
			_source = source;
			_scope = scope;
			_xamlApplyPrefix = xamlApplyPrefix;
			_delegateType = delegateType;
			_exposeContext = exposeContext;
			_appliedType = appliedType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "global::System.Object";

			_exposeContextMethod = _xamlApplyPrefix is null && exposeContext
				? _scope.RegisterMethod(
					$"ApplyTo_{declaringObject.Key.TrimStart(scope.ClassName).TrimStart('_')}", // Note: We trim the scope.ClassName sor for Template methods path will be relative to the scope class itself inst of the full path.
					(_, sb) => sb.AppendMultiLineIndented(_inner.ToString()))
				: null; // DUmped inline (delegate)
		}

		private void TryWriteApply()
		{
			if (!_applyOpened)
			{
				_applyOpened = true;

				IDisposable? blockDisposable;

				var delegateString = !_delegateType.IsNullOrEmpty() ? "(" + _delegateType + ")" : "";

				if (_xamlApplyPrefix is not null)
				{
					_inner.Indent(_source.CurrentLevel);
					blockDisposable = _source.BlockInvariant($".{_xamlApplyPrefix}_XamlApply({delegateString}({AppliedParameterName} => ");
				}
				else if (_exposeContext)
				{
					// This syntax is used to avoid closing on __that and __namescope when running in HotReload.
					_source.AppendIndented($".GenericApply(__that, __nameScope, {_exposeContextMethod}");

					AnalyzerSuppressionsGenerator.GenerateTrimExclusions(_inner);
					blockDisposable = _inner.BlockInvariant($"private void {_exposeContextMethod}({_appliedType} {AppliedParameterName}, {_scope.ClassName} __that, global::Microsoft.UI.Xaml.NameScope __nameScope)");
				}
				else
				{
					_inner.Indent(_source.CurrentLevel);
					blockDisposable = _source.BlockInvariant($".GenericApply({delegateString}(({AppliedParameterName}) => ");
				}

				_applyDisposable = new DisposableAction(() =>
				{
					if (_xamlApplyPrefix != null || !_exposeContext)
					{
						// lambda block
						_source.Append(_inner.ToString());
						blockDisposable.Dispose();
						_source.AppendLineIndented("))");
					}
					else if (_exposeContext)
					{
						// named method
						_source.Append(")");
						_source.AppendLine();

						blockDisposable.Dispose();
					}
					else
					{
						_source.AppendLineIndented("))");
					}
				});
			}
		}
		public int CurrentLevel => _inner.CurrentLevel;

		public void Append(string text)
		{
			TryWriteApply();
			_inner.Append(text);
		}

		public void AppendLine()
		{
			TryWriteApply();
			_inner.AppendLine();
		}

		public void AppendMultiLineIndented(string text)
		{
			TryWriteApply();
			_inner.AppendMultiLineIndented(text);
		}

		public IDisposable Block(IFormatProvider formatProvider, string pattern, params object[] parameters)
		{
			TryWriteApply();
			return _inner.Block(formatProvider, pattern, parameters);
		}

		public IDisposable Block(int count = 1)
		{
			TryWriteApply();
			return _inner.Block(count);
		}

		public IDisposable Indent(int count = 1)
		{
			TryWriteApply();
			return _inner.Indent(count);
		}

		public void AppendIndented(string text)
		{
			TryWriteApply();
			_inner.AppendIndented(text);
		}

		public void AppendIndented(ReadOnlySpan<char> text)
		{
			TryWriteApply();
			_inner.AppendIndented(text);
		}

		public void AppendFormatIndented(IFormatProvider formatProvider, string text, params object[] replacements)
		{
			TryWriteApply();
			_inner.AppendFormatIndented(formatProvider, text, replacements);
		}

		public void Dispose()
		{
			_applyDisposable?.Dispose();
		}

		public override string ToString() => _inner.ToString();
	}

}
