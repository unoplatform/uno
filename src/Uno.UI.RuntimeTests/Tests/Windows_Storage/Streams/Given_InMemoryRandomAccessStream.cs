#nullable disable

using System.IO;
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
}
