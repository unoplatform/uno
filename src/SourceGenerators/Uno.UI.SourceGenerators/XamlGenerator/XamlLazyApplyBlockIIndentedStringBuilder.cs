#nullable enable

using Uno;
using Uno.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;

namespace Uno.UI.SourceGenerators.XamlGenerator
{

	internal class XamlLazyApplyBlockIIndentedStringBuilder : IIndentedStringBuilder, IDisposable
	{
		private bool _applyOpened;
		private readonly string _closureName;
		private readonly IIndentedStringBuilder _source;
		private readonly IndentedStringBuilder _inner = new();
		private IDisposable? _applyDisposable;
		private readonly string? _applyPrefix;
		private readonly string? _delegateType;
		private readonly IDisposable? _parentDisposable;
		private readonly bool _exposeContext;
		private readonly Action<string> _onRegisterApplyMethodBody;
		private readonly string _exposeContextMethod;
		private readonly string _appliedType;
		private readonly string _topLevelType;

		public string? MethodName => _exposeContext ? _exposeContextMethod : null;

		public XamlLazyApplyBlockIIndentedStringBuilder(
			IIndentedStringBuilder source,
			string closureName,
			string? applyPrefix,
			string? delegateType,
			bool exposeContext,
			Action<string> onRegisterApplyMethodBody,
			string appliedType,
			string topLevelType,
			string exposeContextMethod,
			IDisposable? parentDisposable = null)
		{
			_closureName = closureName;
			_source = source;
			_applyPrefix = applyPrefix;
			_delegateType = delegateType;
			_parentDisposable = parentDisposable;
			_exposeContext = exposeContext;
			_onRegisterApplyMethodBody = onRegisterApplyMethodBody;
			_exposeContextMethod = exposeContextMethod;
			_appliedType = appliedType;
			_topLevelType = topLevelType;
		}

		private void TryWriteApply()
		{
			if (!_applyOpened)
			{
				_applyOpened = true;

				IDisposable? blockDisposable;

				var delegateString = !_delegateType.IsNullOrEmpty() ? "(" + _delegateType + ")" : "";

				if (_applyPrefix != null)
				{
					_inner.Indent(_source.CurrentLevel);
					blockDisposable = _source.BlockInvariant(".{0}_XamlApply({2}({1} => ", _applyPrefix, _closureName, delegateString);
				}
				else if (_exposeContext)
				{
					// This syntax is used to avoid closing on __that and __namescope when running in HotReload.
					_source.AppendIndented($".GenericApply(__that, __nameScope, ({_exposeContextMethod}");

					AnalyzerSuppressionsGenerator.GenerateTrimExclusions(_inner);
					blockDisposable = _inner.BlockInvariant($"private void {_exposeContextMethod}({_appliedType} {_closureName}, {_topLevelType} __that, global::Microsoft.UI.Xaml.NameScope __nameScope)");
				}
				else
				{
					_inner.Indent(_source.CurrentLevel);
					blockDisposable = _source.BlockInvariant(".GenericApply({1}(({0}) => ", _closureName, delegateString);
				}

				_applyDisposable = new DisposableAction(() =>
				{
					if (_applyPrefix != null || !_exposeContext)
					{
						// lambda block
						_source.Append(_inner.ToString());
						blockDisposable.Dispose();
						_source.AppendLineIndented("))");
					}
					else if (_exposeContext)
					{
						// named method
						_source.Append("))");
						_source.AppendLine();

						blockDisposable.Dispose();
						_onRegisterApplyMethodBody(_inner.ToString());
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
			_parentDisposable?.Dispose();
		}

		public override string ToString() => _inner.ToString();
	}

}
