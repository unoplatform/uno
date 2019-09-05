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
		private IDisposable _applyDisposable;
		private readonly string _applyPrefix;
		private readonly string _delegateType;

		public XamlLazyApplyBlockIIndentedStringBuilder(IIndentedStringBuilder source, string closureName, string applyPrefix, string delegateType)
		{
			_closureName = closureName;
			_source = source;
			_applyPrefix = applyPrefix;
			_delegateType = delegateType;
		}

		private void TryWriteApply()
		{
			if (!_applyOpened)
			{
				_applyOpened = true;

				IDisposable blockDisposable;

				var delegateString = _delegateType.HasValue() ? "(" + _delegateType + ")" : "";

				if (_applyPrefix != null)
				{
					blockDisposable = _source.BlockInvariant(".{0}_XamlApply({2}({1} => ", _applyPrefix, _closureName, delegateString);
				}
				else
				{
					blockDisposable = _source.BlockInvariant(".Apply({1}({0} => ", _closureName, delegateString);
				}

				_applyDisposable = new DisposableAction(() =>
				{
					blockDisposable.Dispose();
					_source.AppendLineInvariant("))");
				});
			}
		}
		public int CurrentLevel => _source.CurrentLevel;

		public void Append(string text)
		{
			TryWriteApply();
			_source.Append(text);
		}

		public void AppendFormat(IFormatProvider formatProvider, string pattern, params object[] replacements)
		{
			TryWriteApply();
			_source.AppendFormat(formatProvider, pattern, replacements);
		}

		public void AppendLine()
		{
			TryWriteApply();
			_source.AppendLine();
		}

		public void AppendLine(string text)
		{
			TryWriteApply();
			_source.AppendLine(text);
		}

		public IDisposable Block(IFormatProvider formatProvider, string pattern, params object[] parameters)
		{
			TryWriteApply();
			return _source.Block(formatProvider, pattern, parameters);
		}

		public IDisposable Block(int count = 1)
		{
			TryWriteApply();
			return _source.Block(count);
		}

		public IDisposable Indent(int count = 1)
		{
			TryWriteApply();
			return _source.Indent(count);
		}

		public void Dispose()
		{
			_applyDisposable?.Dispose();
		}

		public override string ToString() => _source.ToString();
	}

}
