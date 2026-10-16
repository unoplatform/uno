#nullable enable

using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.Wayland;
using Uno.UI.Runtime.Wayland.Native;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Streams;

namespace Uno.UI.Runtime.Wayland.Tests;

[TestClass]
public sealed partial class DataTransferTests
{
	private static readonly string[] ParsedUriList = ["/a", "/b", "/c", "/f"];
	private static readonly string[] ParsedCut = ["/x"];

	[TestMethod]
	public async Task BitmapSerializationPreservesCallerOwnedStream()
	{
		var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aC1sAAAAASUVORK5CYII=");
		using var original = new MemoryStream(png);
		using var stream = original.AsRandomAccessStream();
		var package = new DataPackage();
		package.SetBitmap(RandomAccessStreamReference.CreateFromStream(stream));
		var view = package.GetView();
		stream.Seek(7);

		var first = await WaylandDataFormats.GetBytesAsync(view, "image/png");
		Assert.AreEqual(7ul, stream.Position);
		var second = await WaylandDataFormats.GetBytesAsync(view, "image/png");
		Assert.AreEqual(7ul, stream.Position);

		CollectionAssert.AreEqual(png, first);
		CollectionAssert.AreEqual(first, second);
		Assert.IsTrue(original.CanRead);
		Assert.IsTrue(original.CanSeek);
		stream.Seek(0);
		Assert.AreEqual(0x89, original.ReadByte());
	}

	[TestMethod]
	public async Task BitmapSerializationRestoresPositionAfterReadFailure()
	{
		using var original = new FailingBitmapStream();
		using var stream = original.AsRandomAccessStream();
		var package = new DataPackage();
		package.SetBitmap(RandomAccessStreamReference.CreateFromStream(stream));
		stream.Seek(7);

		await Assert.ThrowsAsync<IOException>(() => WaylandDataFormats.GetBytesAsync(package.GetView(), "image/png"));
		Assert.AreEqual(7ul, stream.Position);
		await Assert.ThrowsAsync<IOException>(() => WaylandDataFormats.GetBytesAsync(package.GetView(), "image/png"));
		Assert.AreEqual(7ul, stream.Position);
		Assert.IsTrue(original.CanRead);
	}

	private sealed class FailingBitmapStream() : MemoryStream(new byte[16])
	{
		public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			Position = 4;
			return Task.FromException(new IOException("Bitmap read failed after advancing the stream."));
		}
	}

	[TestMethod]
	public void UriListRoundTripsLocalPaths()
	{
		var paths = new[] { "/tmp/plain.txt", "/home/user/My Files/100% #1+ä.png" };
		var uriList = WaylandFileUris.FormatUriList(paths);
		Assert.AreEqual("file:///tmp/plain.txt\r\nfile:///home/user/My%20Files/100%25%20%231%2B%C3%A4.png\r\n", uriList);
		CollectionAssert.AreEqual(paths, WaylandFileUris.ParseUriList(uriList));
	}

	[TestMethod]
	public void UriTextJoinsFileUrisWithBareNewlines()
	{
		Assert.AreEqual("", WaylandFileUris.FormatUriText([]));
		Assert.AreEqual("file:///a%20b\nfile:///c", WaylandFileUris.FormatUriText(["/a b", "/c"]));
	}

	[TestMethod]
	public void UriListSkipsCommentsRemoteHostsAndOtherSchemes()
	{
		var text = "# comment\nfile:///a\nfile://localhost/b\nfile:/c\nfile://server/d\nhttps://example.com/e\n\nfile:///f\0";
		CollectionAssert.AreEqual(ParsedUriList, WaylandFileUris.ParseUriList(text));
	}

	[TestMethod]
	public void GnomeCopiedFilesRoundTrips()
	{
		var paths = new[] { "/tmp/a b", "/tmp/c" };
		var text = WaylandFileUris.FormatGnomeCopiedFiles(paths);
		Assert.AreEqual("copy\nfile:///tmp/a%20b\nfile:///tmp/c", text);
		CollectionAssert.AreEqual(paths, WaylandFileUris.ParseGnomeCopiedFiles(text));
		CollectionAssert.AreEqual(ParsedCut, WaylandFileUris.ParseGnomeCopiedFiles("cut\nfile:///x\n"));
		Assert.AreEqual(0, WaylandFileUris.ParseGnomeCopiedFiles("copy").Count);
	}

	[TestMethod]
	public void PipeTransfersLargePayloadsAcrossThreads()
	{
		RequireLinux();
		var data = new byte[3 * 1024 * 1024 + 17];
		new Random(1).NextBytes(data);
		var (read, write) = WaylandPipe.Create();
		var writer = Task.Run(() => WaylandPipe.WriteAll(write, data, TimeSpan.FromSeconds(5)));
		var received = WaylandPipe.ReadToEnd(read, TimeSpan.FromSeconds(5));
		Assert.IsTrue(writer.Result);
		CollectionAssert.AreEqual(data, received);
	}

	[TestMethod]
	public void PipeReadTimesOutWhenTheWriterStaysSilent()
	{
		RequireLinux();
		var (read, write) = WaylandPipe.Create();
		try
		{
			Assert.IsNull(WaylandPipe.ReadToEnd(read, TimeSpan.FromMilliseconds(50)));
		}
		finally
		{
			_ = Posix.close(write);
		}
	}

	[TestMethod]
	public void PipeWriteFailsWhenTheReaderIsGone()
	{
		RequireLinux();
		var (read, write) = WaylandPipe.Create();
		_ = Posix.close(read);
		Assert.IsFalse(WaylandPipe.WriteAll(write, Encoding.UTF8.GetBytes("text"), TimeSpan.FromSeconds(1)));
	}
	[TestMethod]
	public async Task PipeWriteTimesOutWithANonReadingPeer()
	{
		RequireLinux();
		var (read, write) = WaylandPipe.Create();
		try
		{
			var writer = Task.Run(() => WaylandPipe.WriteAll(write, new byte[3 * 1024 * 1024], TimeSpan.FromMilliseconds(100)));
			Assert.IsFalse(await writer.WaitAsync(TimeSpan.FromSeconds(3)));
		}
		finally
		{
			_ = Posix.close(read);
		}
	}

	[TestMethod]
	public async Task PipeRejectsTransfersAboveTheSizeLimit()
	{
		RequireLinux();
		var (read, write) = WaylandPipe.Create();
		var writer = Task.Run(() => WaylandPipe.WriteAll(write, new byte[128 * 1024], TimeSpan.FromSeconds(1)));
		Assert.IsNull(WaylandPipe.ReadToEnd(read, TimeSpan.FromSeconds(1), maximumSize: 1024));
		await writer.WaitAsync(TimeSpan.FromSeconds(3));
	}

	[TestMethod]
	public void DropFinishRequiresAcceptedMimeDropAndMatchingSelectedAction()
	{
		var negotiation = new WaylandDropNegotiation { AcceptedActions = 3, SelectedAction = 2 };
		Assert.IsFalse(negotiation.CanDrop);
		negotiation.AcceptedMime = true;
		Assert.IsTrue(negotiation.CanDrop);
		Assert.IsFalse(negotiation.CanFinish(2));
		negotiation.Dropped = true;
		Assert.IsFalse(negotiation.CanFinish(1));
		Assert.IsTrue(negotiation.CanFinish(2));
		negotiation.SelectedAction = 0;
		Assert.IsFalse(negotiation.CanFinish(0));
		negotiation.SelectedAction = 3;
		Assert.IsFalse(negotiation.CanFinish(3));
		negotiation.SelectedAction = 4;
		Assert.IsFalse(negotiation.CanDrop);
		negotiation.SelectedAction = 2;
		negotiation.AcceptedActions = 1;
		Assert.IsFalse(negotiation.CanDrop);
		negotiation.AcceptedActions = 3;
		negotiation.AcceptedMime = false;
		Assert.IsFalse(negotiation.CanFinish(2));
	}

	[TestMethod]
	public void ChosenDropActionIsSingleAndSupportedBySource()
	{
		Assert.AreEqual(1u, WaylandDropNegotiation.ChooseAction(3, 3));
		Assert.AreEqual(2u, WaylandDropNegotiation.ChooseAction(3, 2));
		Assert.AreEqual(2u, WaylandDropNegotiation.ChooseAction(2, 3));
		Assert.AreEqual(0u, WaylandDropNegotiation.ChooseAction(1, 2));
		Assert.AreEqual(0u, WaylandDropNegotiation.ChooseAction(0, 3));
	}

	[TestMethod]
	public async Task PipeReadDeadlineIsExtendedByProgress()
	{
		RequireLinux();
		var (read, write) = WaylandPipe.Create();
		using var cancellation = new CancellationTokenSource();
		var writer = Task.Run(async () =>
		{
			try
			{
				while (!cancellation.IsCancellationRequested)
				{
					if (!WriteByte(write))
					{
						break;
					}
					await Task.Delay(10);
				}
			}
			finally
			{
				_ = Posix.close(write);
			}
		});
		try
		{
			// A writer that keeps sending must outlive the idle timeout; only its own end of file completes the read.
			var reader = Task.Run(() => WaylandPipe.ReadToEnd(read, TimeSpan.FromMilliseconds(100)));
			await Task.Delay(400);
			Assert.IsFalse(reader.IsCompleted);
			cancellation.Cancel();
			await writer;
			var received = await reader.WaitAsync(TimeSpan.FromSeconds(3));
			Assert.IsNotNull(received);
			Assert.IsGreaterThanOrEqualTo(10, received.Length);
		}
		finally
		{
			cancellation.Cancel();
			await writer;
		}
	}

	[TestMethod]
	public async Task PipeReadTotalDeadlineCutsOffTricklingWriter()
	{
		RequireLinux();
		var (read, write) = WaylandPipe.Create();
		using var cancellation = new CancellationTokenSource();
		var writes = 0;
		var writer = Task.Run(async () =>
		{
			try
			{
				while (!cancellation.IsCancellationRequested && WriteByte(write))
				{
					Interlocked.Increment(ref writes);
					await Task.Delay(10);
				}
			}
			finally
			{
				_ = Posix.close(write);
			}
		});
		try
		{
			var reader = Task.Run(() => WaylandPipe.ReadToEnd(read, TimeSpan.FromSeconds(5), totalTimeout: TimeSpan.FromMilliseconds(500)));
			Assert.IsNull(await reader.WaitAsync(TimeSpan.FromSeconds(3)), "Progress must not reset the total deadline.");
			Assert.IsTrue(Volatile.Read(ref writes) > 1, "The peer must make repeated progress before cutoff.");
			Assert.AreEqual(-1, Posix.fcntl(read, 3, 0));
		}
		finally
		{
			cancellation.Cancel();
			await writer.WaitAsync(TimeSpan.FromSeconds(3));
		}
	}

	[TestMethod]
	public async Task PipeWriteTotalDeadlineCutsOffTricklingReader()
	{
		RequireLinux();
		var (read, write) = WaylandPipe.Create();
		using var cancellation = new CancellationTokenSource();
		var reads = 0;
		var reader = Task.Run(async () =>
		{
			try
			{
				while (!cancellation.IsCancellationRequested && ReadChunk(read) > 0)
				{
					Interlocked.Increment(ref reads);
					await Task.Delay(10);
				}
			}
			finally
			{
				_ = Posix.close(read);
			}
		});
		try
		{
			var writer = Task.Run(() => WaylandPipe.WriteAll(write, new byte[3 * 1024 * 1024], TimeSpan.FromSeconds(5), totalTimeout: TimeSpan.FromMilliseconds(500)));
			Assert.IsFalse(await writer.WaitAsync(TimeSpan.FromSeconds(3)), "Progress must not reset the total deadline.");
			Assert.IsTrue(Volatile.Read(ref reads) > 1);
			Assert.AreEqual(-1, Posix.fcntl(write, 3, 0));
		}
		finally
		{
			cancellation.Cancel();
			await reader.WaitAsync(TimeSpan.FromSeconds(3));
		}
	}

	private static unsafe nint ReadChunk(int fd)
	{
		byte* bytes = stackalloc byte[4096];
		return Posix.read(fd, bytes, 4096);
	}

	[TestMethod]
	public async Task PipeReadTimesOutWhenTheWriterStallsAfterProgress()
	{
		RequireLinux();
		var (read, write) = WaylandPipe.Create();
		try
		{
			Assert.IsTrue(WriteByte(write));
			var reader = Task.Run(() => WaylandPipe.ReadToEnd(read, TimeSpan.FromMilliseconds(100)));
			Assert.IsNull(await reader.WaitAsync(TimeSpan.FromSeconds(3)));
		}
		finally
		{
			_ = Posix.close(write);
		}
	}

	[TestMethod]
	public void OversizedWriteClosesTheSendDescriptor()
	{
		RequireLinux();
		var (read, write) = WaylandPipe.Create();
		try
		{
			Assert.IsFalse(WaylandPipe.WriteAll(write, new byte[WaylandPipe.MaximumTransferSize + 1], TimeSpan.FromSeconds(1)));
			Assert.AreEqual(-1, Posix.fcntl(write, 3, 0));
		}
		finally
		{
			_ = Posix.close(read);
		}
	}

	[TestMethod]
	public async Task CancelledWriteClosesTheSendDescriptor()
	{
		RequireLinux();
		var (read, write) = WaylandPipe.Create();
		using var cancellation = new CancellationTokenSource();
		try
		{
			var writer = Task.Run(() => WaylandPipe.WriteAll(write, new byte[3 * 1024 * 1024], TimeSpan.FromSeconds(5), cancellation.Token));
			cancellation.Cancel();
			Assert.IsFalse(await writer.WaitAsync(TimeSpan.FromSeconds(3)));
			Assert.AreEqual(-1, Posix.fcntl(write, 3, 0));
		}
		finally
		{
			_ = Posix.close(read);
		}
	}

	[TestMethod]
	public async Task PipeReadCancellationStopsATricklingSource()
	{
		RequireLinux();
		using var source = new TricklingSource();
		using var cancellation = new CancellationTokenSource();
		var reader = Task.Run(() => source.Receive("text/plain", cancellation.Token));
		await source.Started.Task.WaitAsync(TimeSpan.FromSeconds(3));
		await source.FirstByte.Task.WaitAsync(TimeSpan.FromSeconds(3));
		await Task.Delay(200);
		Assert.IsTrue(source.BytesWritten > 1);
		Assert.IsFalse(reader.IsCompleted);
		cancellation.Cancel();
		Assert.IsNull(await reader.WaitAsync(TimeSpan.FromSeconds(3)));
		await source.Writer.WaitAsync(TimeSpan.FromSeconds(3));
		Assert.IsTrue(source.ReaderClosed, "Cancellation must close the descriptor even while bytes keep arriving.");
	}

	[TestMethod]
	public async Task AbandonedOfferReadStopsATricklingSource()
	{
		RequireLinux();
		using var source = new TricklingSource();
		using var reads = new WaylandDataOfferReads(source.Receive);
		using var cancellation = new CancellationTokenSource();
		var reader = reads.ReadAsync("text/plain", cancellation.Token);
		await source.Started.Task.WaitAsync(TimeSpan.FromSeconds(3));
		await source.FirstByte.Task.WaitAsync(TimeSpan.FromSeconds(3));
		await Task.Delay(200);
		Assert.IsTrue(source.BytesWritten > 1);
		Assert.IsFalse(reader.IsCompleted);
		cancellation.Cancel();
		await Assert.ThrowsAsync<OperationCanceledException>(() => reader);
		Assert.IsFalse(await reads.WhenReadsCompleted().WaitAsync(TimeSpan.FromSeconds(3)));
		await source.Writer.WaitAsync(TimeSpan.FromSeconds(3));
		Assert.IsTrue(source.ReaderClosed);
	}

	[TestMethod]
	public async Task CancellingOneSameMimeReaderKeepsTheSharedTransferAlive()
	{
		RequireLinux();
		using var source = new TricklingSource();
		using var reads = new WaylandDataOfferReads(source.Receive);
		using var cancellation = new CancellationTokenSource();
		var first = reads.ReadAsync("text/plain", cancellation.Token);
		var second = reads.ReadAsync("text/plain", CancellationToken.None);
		await source.Started.Task.WaitAsync(TimeSpan.FromSeconds(3));
		cancellation.Cancel();
		await Assert.ThrowsAsync<OperationCanceledException>(() => first);
		await Task.Delay(200);
		Assert.IsFalse(second.IsCompleted);
		Assert.IsFalse(source.ReaderClosed);
		Assert.AreEqual(1, source.ReceiveCount);
		source.Stop();
		var received = await second.WaitAsync(TimeSpan.FromSeconds(3));
		Assert.IsNotNull(received);
		Assert.IsTrue(received.Length > 0);
		Assert.IsTrue(await reads.WhenReadsCompleted().WaitAsync(TimeSpan.FromSeconds(3)));
		CollectionAssert.AreEqual(received, await reads.ReadAsync("text/plain", CancellationToken.None));
		Assert.AreEqual(1, source.ReceiveCount, "Completed same-MIME transfers remain cached.");
	}

	[TestMethod]
	public async Task AbandonedOfferReadCanBeRequestedAgain()
	{
		RequireLinux();
		using var firstSource = new TricklingSource();
		using var secondSource = new TricklingSource();
		var receives = 0;
		using var reads = new WaylandDataOfferReads((mimeType, token) =>
			(Interlocked.Increment(ref receives) == 1 ? firstSource : secondSource).Receive(mimeType, token));
		using var cancellation = new CancellationTokenSource();
		var first = reads.ReadAsync("text/plain", cancellation.Token);
		await firstSource.Started.Task.WaitAsync(TimeSpan.FromSeconds(3));
		cancellation.Cancel();
		await Assert.ThrowsAsync<OperationCanceledException>(() => first);
		await firstSource.Writer.WaitAsync(TimeSpan.FromSeconds(3));

		var second = reads.ReadAsync("text/plain", CancellationToken.None);
		await secondSource.Started.Task.WaitAsync(TimeSpan.FromSeconds(3));
		await secondSource.FirstByte.Task.WaitAsync(TimeSpan.FromSeconds(3));
		secondSource.Stop();
		var received = await second.WaitAsync(TimeSpan.FromSeconds(3));
		Assert.IsNotNull(received);
		Assert.IsTrue(received.Length > 0);
		Assert.AreEqual(2, receives, "An abandoned transfer must not poison the MIME cache.");
	}

	[TestMethod]
	[DataRow(false, true, DataPackageOperation.None)]
	[DataRow(false, false, DataPackageOperation.None)]
	[DataRow(true, true, DataPackageOperation.None)]
	[DataRow(true, false, DataPackageOperation.None)]
	[DataRow(true, false, DataPackageOperation.Move)]
	public async Task LeavingOrRejectingDropCancelsATricklingTransfer(bool dropped, bool leave, DataPackageOperation result)
	{
		RequireLinux();
		using var source = new TricklingSource();
		using var reads = new WaylandDataOfferReads(source.Receive);
		var waits = 0;
		var destroyed = 0;
		var session = new WaylandDropSession(
			() => { waits++; return reads.WhenReadsCompleted(); },
			_ => Assert.Fail("An aborted or rejected drop must not send finish."),
			() => { destroyed++; reads.Dispose(); })
		{
			Dropped = dropped,
			DropAction = dropped ? WaylandDataFormats.DndActionCopy : 0,
		};
		var reader = reads.ReadAsync("text/plain", CancellationToken.None);
		await source.Started.Task.WaitAsync(TimeSpan.FromSeconds(3));
		await source.Progressed.Task.WaitAsync(TimeSpan.FromSeconds(3));
		Assert.IsTrue(source.BytesWritten > 1);
		Assert.IsFalse(reader.IsCompleted);

		if (leave)
		{
			session.Leave();
		}
		else
		{
			session.Complete(result);
		}
		Assert.AreEqual(1, destroyed, "Ending the drag must destroy the offer synchronously.");
		Assert.AreEqual(0, waits, "An unsuccessful drop must not wait for a progressing source.");
		Assert.IsNull(await reader.WaitAsync(TimeSpan.FromSeconds(3)));
		await source.Writer.WaitAsync(TimeSpan.FromSeconds(3));
		Assert.IsTrue(source.ReaderClosed);
		Assert.IsTrue(source.ReadDescriptorReleased);
		session.Complete(DataPackageOperation.Copy);
		Assert.AreEqual(1, destroyed, "A late completion must not finish or destroy the offer again.");
	}

	[TestMethod]
	public async Task SuccessfulDropWaitsForReadsBeforeFinishing()
	{
		var reads = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var finished = new TaskCompletionSource<uint>(TaskCreationOptions.RunContinuationsAsynchronously);
		var session = new WaylandDropSession(() => reads.Task, action => finished.SetResult(action), () => Assert.Fail("Successful reads must finish the drop."))
		{
			Dropped = true,
			DropAction = WaylandDataFormats.DndActionMove,
		};
		session.Complete(DataPackageOperation.Move);
		Assert.IsFalse(finished.Task.IsCompleted);
		reads.SetResult(true);
		Assert.AreEqual(WaylandDataFormats.DndActionMove, await finished.Task.WaitAsync(TimeSpan.FromSeconds(3)));
	}

	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public async Task SuccessfulDropWithFailedReadsDestroysWithoutFinishing(bool throws)
	{
		var reads = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		var destroyed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var finishes = 0;
		var session = new WaylandDropSession(() => reads.Task, _ => finishes++, () => destroyed.SetResult())
		{
			Dropped = true,
			DropAction = WaylandDataFormats.DndActionMove,
		};
		session.Complete(DataPackageOperation.Move);
		Assert.IsFalse(destroyed.Task.IsCompleted);
		if (throws)
		{
			reads.SetException(new IOException("Transfer failed."));
		}
		else
		{
			reads.SetResult(false);
		}
		await destroyed.Task.WaitAsync(TimeSpan.FromSeconds(3));
		Assert.AreEqual(0, finishes, "Failed transfers must not let the source delete moved data.");
	}

	[TestMethod]
	public async Task DestroyedOfferCancelsAllInFlightReads()
	{
		RequireLinux();
		using var firstSource = new TricklingSource();
		using var secondSource = new TricklingSource();
		using var reads = new WaylandDataOfferReads((mimeType, token) =>
			(mimeType == "text/plain" ? firstSource : secondSource).Receive(mimeType, token));
		var first = reads.ReadAsync("text/plain", CancellationToken.None);
		var second = reads.ReadAsync("text/html", CancellationToken.None);
		await Task.WhenAll(firstSource.Started.Task, secondSource.Started.Task).WaitAsync(TimeSpan.FromSeconds(3));
		reads.Dispose();
		Assert.IsNull(await first.WaitAsync(TimeSpan.FromSeconds(3)));
		Assert.IsNull(await second.WaitAsync(TimeSpan.FromSeconds(3)));
		Assert.IsFalse(await reads.WhenReadsCompleted().WaitAsync(TimeSpan.FromSeconds(3)));
		await Task.WhenAll(firstSource.Writer, secondSource.Writer).WaitAsync(TimeSpan.FromSeconds(3));
		Assert.IsTrue(firstSource.ReaderClosed);
		Assert.IsTrue(secondSource.ReaderClosed);
		Assert.IsNull(await reads.ReadAsync("text/plain", CancellationToken.None));
	}

	[TestMethod]
	public async Task PipeWriteDeadlineIsExtendedByASlowReader()
	{
		RequireLinux();
		var data = new byte[512 * 1024];
		new Random(2).NextBytes(data);
		var (read, write) = WaylandPipe.Create();
		var timeout = TimeSpan.FromMilliseconds(500);
		var elapsed = Stopwatch.StartNew();
		var writer = Task.Run(() => WaylandPipe.WriteAll(write, data, timeout));
		var reader = Task.Run(() => ReadSlowly(read));
		var sent = await writer.WaitAsync(TimeSpan.FromSeconds(10));
		var received = await reader.WaitAsync(TimeSpan.FromSeconds(10));
		Assert.IsTrue(sent);
		CollectionAssert.AreEqual(data, received);
		Assert.IsTrue(elapsed.Elapsed > timeout, "The transfer must exceed the original deadline while the reader keeps making progress.");
	}

	private static unsafe byte[] ReadSlowly(int fd)
	{
		try
		{
			using var received = new MemoryStream();
			var chunk = new byte[4096];
			while (true)
			{
				nint count;
				fixed (byte* pointer = chunk)
				{
					count = Posix.read(fd, pointer, (nuint)chunk.Length);
				}
				if (count == 0)
				{
					return received.ToArray();
				}
				Assert.IsTrue(count > 0);
				received.Write(chunk, 0, (int)count);
				Thread.Sleep(15);
			}
		}
		finally
		{
			_ = Posix.close(fd);
		}
	}

	private sealed class TricklingSource : IDisposable
	{
		private readonly CancellationTokenSource _stop = new();
		private readonly int _read;
		private int _bytesWritten;
		internal readonly TaskCompletionSource Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
		internal readonly TaskCompletionSource FirstByte = new(TaskCreationOptions.RunContinuationsAsynchronously);
		internal readonly TaskCompletionSource Progressed = new(TaskCreationOptions.RunContinuationsAsynchronously);
		internal Task Writer { get; }
		internal bool ReaderClosed { get; private set; }
		internal bool ReadDescriptorReleased => Posix.fcntl(_read, 3, 0) == -1;
		internal int ReceiveCount { get; private set; }
		internal int BytesWritten => Volatile.Read(ref _bytesWritten);

		internal TricklingSource()
		{
			var (read, write) = WaylandPipe.Create();
			_read = read;
			Writer = Task.Run(async () =>
			{
				try
				{
					while (!_stop.IsCancellationRequested)
					{
						if (!WriteByte(write))
						{
							ReaderClosed = true;
							break;
						}
						if (Interlocked.Increment(ref _bytesWritten) > 1)
						{
							Progressed.TrySetResult();
						}
						FirstByte.TrySetResult();
						await Task.Delay(10);
					}
				}
				finally
				{
					_ = Posix.close(write);
				}
			});
		}

		internal byte[]? Receive(string mimeType, CancellationToken cancellationToken)
		{
			ReceiveCount++;
			Started.TrySetResult();
			return WaylandPipe.ReadToEnd(_read, TimeSpan.FromSeconds(5), cancellationToken: cancellationToken);
		}

		internal void Stop() => _stop.Cancel();

		public void Dispose()
		{
			Stop();
			Writer.GetAwaiter().GetResult();
			_stop.Dispose();
		}
	}

	private static void RequireLinux()
	{
		if (!OperatingSystem.IsLinux())
		{
			Assert.Inconclusive("Linux pipe interop is required for this transfer test.");
		}
	}

	private static unsafe bool WriteByte(int fd)
	{
		byte value = 1;
		return Posix.write(fd, &value, 1) == 1;
	}

	[TestMethod]
	public async Task StalledProductionClosesTheSendDescriptor()
	{
		RequireLinux();
		var (read, write) = WaylandPipe.Create();
		try
		{
			var production = new TaskCompletionSource<byte[]?>();
			await Assert.ThrowsAsync<TimeoutException>(() => WaylandDataTransfer.SendAsync(write, production.Task, TimeSpan.FromMilliseconds(50)));
			Assert.AreEqual(-1, Posix.fcntl(write, 3, 0));
			CollectionAssert.AreEqual(Array.Empty<byte>(), WaylandPipe.ReadToEnd(read, TimeSpan.FromSeconds(1)));
			read = -1;
		}
		finally
		{
			if (read >= 0)
			{
				_ = Posix.close(read);
			}
		}
	}

	[TestMethod]
	public async Task FaultedProductionClosesTheSendDescriptor()
	{
		RequireLinux();
		var (read, write) = WaylandPipe.Create();
		try
		{
			await Assert.ThrowsAsync<InvalidOperationException>(() => WaylandDataTransfer.SendAsync(write, Task.FromException<byte[]?>(new InvalidOperationException()), TimeSpan.FromSeconds(1)));
			Assert.AreEqual(-1, Posix.fcntl(write, 3, 0));
		}
		finally
		{
			_ = Posix.close(read);
		}
	}

	[TestMethod]
	public async Task CancelledProductionClosesTheSendDescriptor()
	{
		RequireLinux();
		var (read, write) = WaylandPipe.Create();
		using var cancellation = new CancellationTokenSource();
		try
		{
			var production = new TaskCompletionSource<byte[]?>();
			var transfer = WaylandDataTransfer.SendAsync(write, production.Task, TimeSpan.FromSeconds(5), cancellation.Token);
			cancellation.Cancel();
			await Assert.ThrowsAsync<OperationCanceledException>(() => transfer);
			Assert.AreEqual(-1, Posix.fcntl(write, 3, 0));
		}
		finally
		{
			_ = Posix.close(read);
		}
	}

}
