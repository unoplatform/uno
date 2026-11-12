#nullable enable

using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_RichEditBox
	{
		[TestMethod]
		public void When_Unreadable_Stream_Load_Is_Atomic()
		{
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.None, "original");
			document.ClearUndoRedoHistory();
			using var stream = new ContractFaultStream(
				Encoding.Unicode.GetBytes("replacement"),
				canRead: false).AsRandomAccessStream();

			Assert.ThrowsExactly<NotSupportedException>(() =>
				document.LoadFromStream(TextSetOptions.None, stream));

			GetTextWithoutFinalEop(document, out var text);
			Assert.AreEqual("original", text);
			Assert.IsFalse(document.CanUndo());
			Assert.IsFalse(document.CanRedo());
		}

		[TestMethod]
		public void When_Partial_Read_Fails_Document_And_Range_Are_Unchanged()
		{
			var bytes = CombineContractBytes(
				Encoding.Unicode.GetPreamble(),
				Encoding.Unicode.GetBytes("replacement"));

			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.None, "original");
			document.ClearUndoRedoHistory();
			using (var stream = new ContractFaultStream(bytes, failReadAfter: 4).AsRandomAccessStream())
			{
				Assert.ThrowsExactly<IOException>(() =>
					document.LoadFromStream(TextSetOptions.None, stream));
			}
			GetTextWithoutFinalEop(document, out var documentText);
			Assert.AreEqual("original", documentText);
			Assert.IsFalse(document.CanUndo());

			var range = document.GetRange(2, 6);
			using (var stream = new ContractFaultStream(bytes, failReadAfter: 4).AsRandomAccessStream())
			{
				Assert.ThrowsExactly<IOException>(() =>
					range.SetTextViaStream(TextSetOptions.None, stream));
			}
			GetTextWithoutFinalEop(document, out var rangeText);
			Assert.AreEqual("original", rangeText);
			Assert.AreEqual(2, range.StartPosition);
			Assert.AreEqual(6, range.EndPosition);
			Assert.IsFalse(document.CanUndo());
		}

		[TestMethod]
		[DataRow(false, false)]
		[DataRow(false, true)]
		[DataRow(true, false)]
		[DataRow(true, true)]
		public void When_Partial_Write_Fails_Stream_Is_Rolled_Back(
			bool range,
			bool rtf)
		{
			var expectedError = new IOException("Expected write failure.");
			var backing = new ContractFaultStream(
				Encoding.ASCII.GetBytes("keep"),
				failWriteAfter: 2,
				failOnlyOnce: true,
				writeException: expectedError);
			using var stream = backing.AsRandomAccessStream();
			stream.Seek(2);

			var error = Assert.ThrowsExactly<IOException>(() =>
				WriteContractOutput(range, rtf, stream));

			Assert.AreSame(expectedError, error);
			Assert.AreEqual(2ul, stream.Position);
			CollectionAssert.AreEqual(Encoding.ASCII.GetBytes("keep"), backing.ToArray());
			Assert.IsFalse(error.Data.Contains(StreamRollbackFailureDataKey));
		}

		[TestMethod]
		public void When_Size_Assignment_Fails_Stream_Is_Rolled_Back()
		{
			var expectedError = new InvalidOperationException("Expected size failure.");
			var backing = new ContractFaultStream(
				Encoding.ASCII.GetBytes("keep"),
				setLengthException: expectedError);
			using var stream = backing.AsRandomAccessStream();
			stream.Seek(2);

			var error = Assert.ThrowsExactly<InvalidOperationException>(() =>
				WriteContractOutput(range: false, rtf: false, stream: stream));

			Assert.AreSame(expectedError, error);
			Assert.AreEqual(2ul, stream.Position);
			CollectionAssert.AreEqual(Encoding.ASCII.GetBytes("keep"), backing.ToArray());
			Assert.IsFalse(error.Data.Contains(StreamRollbackFailureDataKey));
		}

		[TestMethod]
		public void When_Flush_Fails_Stream_Is_Rolled_Back()
		{
			var expectedError = new IOException("Expected flush failure.");
			var backing = new ContractFaultStream(
				Encoding.ASCII.GetBytes("keep"),
				flushException: expectedError);
			using var stream = backing.AsRandomAccessStream();
			stream.Seek(2);

			var error = Assert.ThrowsExactly<IOException>(() =>
				WriteContractOutput(range: true, rtf: true, stream: stream));

			Assert.AreSame(expectedError, error);
			Assert.AreEqual(2ul, stream.Position);
			CollectionAssert.AreEqual(Encoding.ASCII.GetBytes("keep"), backing.ToArray());
			Assert.IsFalse(error.Data.Contains(StreamRollbackFailureDataKey));
		}

		[TestMethod]
		public void When_Wrapped_Nonfatal_Write_Fails_Stream_Is_Rolled_Back()
		{
			var expectedError = new InvalidOperationException(
				"Expected wrapped write failure.",
				new IOException("Expected inner write failure."));
			var backing = new ContractFaultStream(
				Encoding.ASCII.GetBytes("keep"),
				failWriteAfter: 2,
				failOnlyOnce: true,
				writeException: expectedError);
			using var stream = backing.AsRandomAccessStream();
			stream.Seek(2);

			var error = Assert.ThrowsExactly<InvalidOperationException>(() =>
				WriteContractOutput(range: false, rtf: true, stream: stream));

			Assert.AreSame(expectedError, error);
			Assert.IsInstanceOfType<IOException>(error.InnerException);
			Assert.AreEqual(2ul, stream.Position);
			CollectionAssert.AreEqual(Encoding.ASCII.GetBytes("keep"), backing.ToArray());
			Assert.IsFalse(error.Data.Contains(StreamRollbackFailureDataKey));
		}

		[TestMethod]
		public void When_Stream_Is_Disposed_During_Write_Original_Error_Is_Preserved()
		{
			var expectedError = new ObjectDisposedException("contract", "Expected disposed write failure.");
			var backing = new ContractFaultStream(
				Encoding.ASCII.GetBytes("keep"),
				failWriteAfter: 2,
				writeException: expectedError,
				disposeOnWriteFailure: true);
			using var stream = backing.AsRandomAccessStream();
			stream.Seek(2);

			var error = Assert.ThrowsExactly<ObjectDisposedException>(() =>
				WriteContractOutput(range: true, rtf: false, stream: stream));

			Assert.AreSame(expectedError, error);
			Assert.IsNotNull(GetRollbackFailure(error));
		}

		[TestMethod]
		public void When_Rollback_Fails_Original_Error_Is_Preserved()
		{
			var expectedError = new IOException("Expected write failure.");
			var rollbackError = new InvalidOperationException("Expected rollback failure.");
			var backing = new ContractFaultStream(
				Encoding.ASCII.GetBytes("keep"),
				failWriteAfter: 2,
				failOnlyOnce: true,
				writeException: expectedError,
				rollbackWriteException: rollbackError);
			using var stream = backing.AsRandomAccessStream();
			stream.Seek(2);

			var error = Assert.ThrowsExactly<IOException>(() =>
				WriteContractOutput(range: false, rtf: false, stream: stream));

			Assert.AreSame(expectedError, error);
			Assert.AreSame(rollbackError, GetRollbackFailure(error));
			Assert.AreEqual(2ul, stream.Position);
		}

		[TestMethod]
		public void When_Empty_Rtf_Range_Is_Stream_NoOp()
		{
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.None, "text");
			var backing = new ContractFaultStream(Encoding.ASCII.GetBytes("keep"));
			using var stream = backing.AsRandomAccessStream();
			stream.Seek(2);

			document.GetRange(2, 2).GetTextViaStream(TextGetOptions.FormatRtf, stream);

			Assert.AreEqual(2ul, stream.Position);
			CollectionAssert.AreEqual(Encoding.ASCII.GetBytes("keep"), backing.ToArray());
		}

		[TestMethod]
		public void When_Partial_Write_Cannot_Roll_Back_Limitation_Is_Explicit()
		{
			var expectedError = new IOException("Expected write failure.");
			var backing = new ContractFaultStream(
				Encoding.ASCII.GetBytes("keep"),
				canRead: false,
				failWriteAfter: 2,
				writeException: expectedError);
			using var stream = backing.AsRandomAccessStream();
			stream.Seek(2);

			var error = Assert.ThrowsExactly<IOException>(() =>
				WriteContractOutput(range: false, rtf: false, stream: stream));

			Assert.AreSame(expectedError, error);
			StringAssert.Contains(GetRollbackFailure(error).Message, "does not support bounded rollback");
			Assert.AreEqual(0, backing.ReadCount);
			Assert.AreEqual(2ul, stream.Position);
			Assert.AreNotEqual("6B656570", Convert.ToHexString(backing.ToArray()));
		}

		[TestMethod]
		public void When_Stream_Exceeds_Rollback_Policy_Snapshot_Is_Not_Attempted()
		{
			var expectedError = new IOException("Expected write failure.");
			var backing = new ContractFaultStream(
				Encoding.ASCII.GetBytes("keep"),
				failWriteAfter: 2,
				writeException: expectedError,
				reportedLength: 16 * 1024 * 1024 + 1);
			using var stream = backing.AsRandomAccessStream();
			stream.Seek(2);

			var error = Assert.ThrowsExactly<IOException>(() =>
				WriteContractOutput(range: false, rtf: true, stream: stream));

			Assert.AreSame(expectedError, error);
			StringAssert.Contains(GetRollbackFailure(error).Message, "does not support bounded rollback");
			Assert.AreEqual(0, backing.ReadCount);
			Assert.AreEqual(2ul, stream.Position);
		}

		[TestMethod]
		public void When_Unwritable_Stream_Is_Rejected_Before_Mutation()
		{
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.None, "replacement");
			var backing = new MemoryStream(Encoding.ASCII.GetBytes("keep"), writable: false);
			using var stream = backing.AsRandomAccessStream();

			Assert.ThrowsExactly<NotSupportedException>(() =>
				document.SaveToStream(TextGetOptions.None, stream));

			CollectionAssert.AreEqual(Encoding.ASCII.GetBytes("keep"), backing.ToArray());
		}

		private const string StreamRollbackFailureDataKey = "Uno.RichEditTextDocument.StreamRollbackFailure";

		private static void WriteContractOutput(
			bool range,
			bool rtf,
			Windows.Storage.Streams.IRandomAccessStream stream)
		{
			var document = new RichEditBox().Document;
			document.SetText(TextSetOptions.None, "replacement");
			var options = rtf ? TextGetOptions.FormatRtf : TextGetOptions.None;
			if (range)
			{
				document.GetRange(0, 5).GetTextViaStream(options, stream);
			}
			else
			{
				document.SaveToStream(options, stream);
			}
		}

		private static Exception GetRollbackFailure(Exception error)
		{
			Assert.IsTrue(error.Data.Contains(StreamRollbackFailureDataKey));
			var rollbackFailure = error.Data[StreamRollbackFailureDataKey] as Exception;
			Assert.IsNotNull(rollbackFailure);
			return rollbackFailure;
		}

		private sealed class ContractFaultStream : Stream
		{
			private readonly MemoryStream _inner;
			private readonly bool _canRead;
			private readonly int _failReadAfter;
			private readonly int _failWriteAfter;
			private readonly bool _failOnlyOnce;
			private readonly Exception _writeException;
			private readonly Exception? _flushException;
			private readonly Exception? _setLengthException;
			private readonly Exception? _rollbackWriteException;
			private readonly bool _disposeOnWriteFailure;
			private readonly long? _reportedLength;
			private int _read;
			private int _written;
			private bool _readFailed;
			private bool _writeFailed;
			private bool _flushFailed;
			private bool _setLengthFailed;

			public ContractFaultStream(
				byte[] bytes,
				bool canRead = true,
				int failReadAfter = -1,
				int failWriteAfter = -1,
				bool failOnlyOnce = false,
				Exception? writeException = null,
				Exception? flushException = null,
				Exception? setLengthException = null,
				Exception? rollbackWriteException = null,
				bool disposeOnWriteFailure = false,
				long? reportedLength = null)
			{
				_inner = new MemoryStream();
				_inner.Write(bytes, 0, bytes.Length);
				_inner.Position = 0;
				_canRead = canRead;
				_failReadAfter = failReadAfter;
				_failWriteAfter = failWriteAfter;
				_failOnlyOnce = failOnlyOnce;
				_writeException = writeException ?? new IOException("Expected write failure.");
				_flushException = flushException;
				_setLengthException = setLengthException;
				_rollbackWriteException = rollbackWriteException;
				_disposeOnWriteFailure = disposeOnWriteFailure;
				_reportedLength = reportedLength;
			}

			public override bool CanRead => _canRead;

			public override bool CanSeek => true;

			public override bool CanWrite => true;

			public override long Length => _reportedLength ?? _inner.Length;

			public override long Position
			{
				get => _inner.Position;
				set => _inner.Position = value;
			}

			public byte[] ToArray() => _inner.ToArray();

			public int ReadCount { get; private set; }

			public override void Flush()
			{
				if (_flushException is not null && !_flushFailed)
				{
					_flushFailed = true;
					throw _flushException;
				}

				_inner.Flush();
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				ReadCount++;
				if (!_canRead)
				{
					throw new NotSupportedException();
				}
				if (_failReadAfter >= 0
					&& _read >= _failReadAfter
					&& (!_failOnlyOnce || !_readFailed))
				{
					_readFailed = true;
					throw new IOException("Expected read failure.");
				}

				var allowed = _failReadAfter < 0 || _readFailed
					? count
					: Math.Min(count, _failReadAfter - _read);
				var read = _inner.Read(buffer, offset, allowed);
				_read += read;
				return read;
			}

			public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

			public override void SetLength(long value)
			{
				if (_setLengthException is not null && !_setLengthFailed)
				{
					_setLengthFailed = true;
					throw _setLengthException;
				}

				_inner.SetLength(value);
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				if (_writeFailed && _rollbackWriteException is not null)
				{
					throw _rollbackWriteException;
				}
				if (_failWriteAfter >= 0
					&& _written >= _failWriteAfter
					&& (!_failOnlyOnce || !_writeFailed))
				{
					_writeFailed = true;
					throw _writeException;
				}

				var allowed = _failWriteAfter < 0 || _writeFailed
					? count
					: Math.Min(count, _failWriteAfter - _written);
				_inner.Write(buffer, offset, allowed);
				_written += allowed;
				if (allowed != count)
				{
					_writeFailed = true;
					if (_disposeOnWriteFailure)
					{
						_inner.Dispose();
					}
					throw _writeException;
				}
			}
		}
	}
}
