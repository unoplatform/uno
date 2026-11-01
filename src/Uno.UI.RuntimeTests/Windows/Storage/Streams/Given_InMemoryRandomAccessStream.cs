using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage.Streams;
namespace Uno.UI.RuntimeTests.Tests.Windows_Storage.Streams;

[TestClass]
public class Given_InMemoryRandomAccessStream
{
	[TestMethod]
	public async Task When_Open_ReadWrite()
	{
		var stream = new InMemoryRandomAccessStream();
		var direct = stream.AsStream();

		await StreamTestHelper.Test(writeTo: direct, readOn: stream);
		await StreamTestHelper.Test(writeTo: stream, readOn: direct);
	}

	[TestMethod]
	public async Task When_GetInputStream()
	{
		var stream = new InMemoryRandomAccessStream();
		var inputstream = stream.GetInputStreamAt(0);
		var direct = stream.AsStream();

		await StreamTestHelper.Test(writeTo: direct, readOn: inputstream);
	}

	[TestMethod]
	public async Task When_GetOutputStream()
	{
		var stream = new InMemoryRandomAccessStream();
		var outputstream = stream.GetOutputStreamAt(0);
		var direct = stream.AsStream();

		await StreamTestHelper.Test(writeTo: outputstream, readOn: direct);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/25866")]
	public async Task When_CloneStream_Then_Clone_Starts_At_Zero_And_Source_Does_Not_Move()
	{
		byte[] content = [0x10, 0x11, 0x12, 0x13, 0x14, 0x15];
		using var source = new InMemoryRandomAccessStream();
		await source.WriteAsync(content.AsBuffer());
		source.Seek(4);

		using var clone = source.CloneStream();

		Assert.AreEqual(0ul, clone.Position);
		Assert.AreEqual(4ul, source.Position);
		var read = await clone.ReadAsync(new global::Windows.Storage.Streams.Buffer((uint)content.Length), (uint)content.Length, InputStreamOptions.None);
		CollectionAssert.AreEqual(content, read.ToArray());
		Assert.AreEqual(4ul, source.Position);
	}
}
