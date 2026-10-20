#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Web.WebView2.Core;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace Uno.UI.Tests.WebView2Tests;

[UITestClass]
public class Given_WebResourceResponseSnapshot
{
	[TestMethod]
	public async Task When_Managed_Response_Status_Headers_And_Body_Are_Copied()
	{
		var bytes = new MemoryStream();
		bytes.Write(Encoding.UTF8.GetBytes("<html>body</html>"));
		var content = bytes.AsRandomAccessStream();
		var response = new ManagedWebResourceResponse(content, 404, "Not Found", ManagedHttpResponseHeaders.Parse("Content-Type: text/html\r\nSet-Cookie: a=1\r\nset-cookie: b=2"));

		var snapshot = await WebResourceResponseSnapshot.CreateAsync(response, CancellationToken.None);

		Assert.AreEqual(404, snapshot.StatusCode);
		Assert.AreEqual("Not Found", snapshot.ReasonPhrase);
		CollectionAssert.AreEqual(
			new[]
			{
				new KeyValuePair<string, string>("Content-Type", "text/html"),
				new KeyValuePair<string, string>("Set-Cookie", "a=1"),
				new KeyValuePair<string, string>("set-cookie", "b=2"),
			},
			new List<KeyValuePair<string, string>>(snapshot.Headers));
		Assert.AreEqual("<html>body</html>", Encoding.UTF8.GetString(snapshot.Body));
	}

	[TestMethod]
	public async Task When_Seekable_Body_It_Is_Read_From_The_Start_And_Position_Is_Restored()
	{
		var bytes = new MemoryStream(new byte[] { 1, 2, 3, 4 }) { Position = 3 };
		var response = new ManagedWebResourceResponse(bytes.AsRandomAccessStream(), 200, "OK", new ManagedHttpResponseHeaders());

		var syncSnapshot = WebResourceResponseSnapshot.Create(response);
		var asyncSnapshot = await WebResourceResponseSnapshot.CreateAsync(response, CancellationToken.None);

		CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, syncSnapshot.Body);
		CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4 }, asyncSnapshot.Body);
		Assert.AreEqual(3L, bytes.Position);
	}

	[TestMethod]
	public async Task When_Body_Is_Not_Seekable_It_Is_Read_From_Where_It_Is()
	{
		var inner = new NonSeekableStream(new byte[] { 1, 2, 3, 4, 5 });
		inner.ReadByte();
		var response = new ManagedWebResourceResponse(new StreamBackedRandomAccessStream(inner), 200, "OK", new ManagedHttpResponseHeaders());

		var snapshot = await WebResourceResponseSnapshot.CreateAsync(response, CancellationToken.None);

		CollectionAssert.AreEqual(new byte[] { 2, 3, 4, 5 }, snapshot.Body);
	}

	[TestMethod]
	public void When_No_Body_Or_Reason_They_Are_Empty()
	{
		var response = new ManagedWebResourceResponse(null, 204, null, new ManagedHttpResponseHeaders());

		var snapshot = WebResourceResponseSnapshot.Create(response);

		Assert.AreEqual(204, snapshot.StatusCode);
		Assert.AreEqual("", snapshot.ReasonPhrase);
		Assert.AreEqual(0, snapshot.Body.Length);
		Assert.AreEqual(0, snapshot.Headers.Count);
	}

	private sealed class NonSeekableStream(byte[] data) : MemoryStream(data)
	{
		public override bool CanSeek => false;

		public override long Position
		{
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		public override long Seek(long offset, SeekOrigin loc) => throw new NotSupportedException();
	}

	// Stands in for a platform stream over a non-seekable source: AsStream() unwraps it to the .NET stream.
	private sealed class StreamBackedRandomAccessStream(Stream stream) : IRandomAccessStream, IStreamWrapper
	{
		public Stream FindStream() => stream;

		public bool CanRead => true;

		public bool CanWrite => false;

		public ulong Position => throw new NotSupportedException();

		public ulong Size
		{
			get => throw new NotSupportedException();
			set => throw new NotSupportedException();
		}

		public IRandomAccessStream CloneStream() => throw new NotSupportedException();

		public void Dispose() => stream.Dispose();

		public IAsyncOperation<bool> FlushAsync() => throw new NotSupportedException();

		public IInputStream GetInputStreamAt(ulong position) => throw new NotSupportedException();

		public IOutputStream GetOutputStreamAt(ulong position) => throw new NotSupportedException();

		public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options) => throw new NotSupportedException();

		public void Seek(ulong position) => throw new NotSupportedException();

		public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer) => throw new NotSupportedException();
	}
}
