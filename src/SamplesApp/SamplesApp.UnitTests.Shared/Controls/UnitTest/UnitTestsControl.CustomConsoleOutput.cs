#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Uno.UI.Samples.Tests
{
	public sealed partial class UnitTestsControl
	{
		private class ConsoleOutputRecorder : IDisposable
		{
			private readonly TextWriterDuplicator _duplicator;
			private readonly TextWriter _originalOutput;

			private int _isDisposed;

			public static ConsoleOutputRecorder Start()
				=> new ConsoleOutputRecorder();

			private ConsoleOutputRecorder()
			{
				_originalOutput = Console.Out;
				_duplicator = new TextWriterDuplicator(_originalOutput);

				Console.SetOut(_duplicator);
			}

			internal string GetContentAndReset()
				=> _duplicator.GetContentAndReset();

			/// <inheritdoc />
			public void Dispose()
			{
				if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0)
				{
					GC.SuppressFinalize(this);
					Console.SetOut(_originalOutput);
				}
			}

			~ConsoleOutputRecorder()
			{
				Dispose();
			}
		}

		private class TextWriterDuplicator : TextWriter
		{
			private readonly TextWriter _inner;
			private readonly StringBuilder _accumulator = new StringBuilder();

			public TextWriterDuplicator(TextWriter inner)
			{
				_inner = inner;
			}

			internal string GetContentAndReset()
			{
				var result = _accumulator.ToString();
				Reset();
				return result;
			}

			internal void Reset() => _accumulator.Clear();

			public override Encoding Encoding { get; }

			public override void Write(char value)
			{
				_inner.Write(value);
				_accumulator.Append(value);
			}
		}
	}
}
