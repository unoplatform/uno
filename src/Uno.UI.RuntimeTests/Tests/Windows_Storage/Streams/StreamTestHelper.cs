using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Uno.UI.RuntimeTests.Tests.Windows_Storage.Streams
{
	public class StreamTestHelper
	{
		public const string Data = "Hello world";

		public static async Task Test(Stream writeTo, IInputStream readOn, bool directWrapper = false)
		{
			var ras = readOn as IRandomAccessStream;

			// Clear
			writeTo.Position = 0;
			writeTo.SetLength(0);
			ras?.Seek(0);

			if (directWrapper && ras is {})
			{
				Assert.AreEqual((ulong)0, ras.Position);
				Assert.AreEqual((ulong)writeTo.Position, ras.Position); // Those should always be equals
				Assert.AreEqual((ulong)0, ras.Size);
			}

			await WriteData(writeTo);

			if (directWrapper && ras is {})
			{
				Assert.AreEqual((ulong)0, ras.Position);
				Assert.AreEqual((ulong)writeTo.Position, ras.Position); // Those should always be equals
				Assert.AreEqual((ulong)writeTo.Length, ras.Size);
			}

			var actual = await ReadData(readOn); // The data read on the wrapper should be the data written on the src

			Assert.AreEqual<string>(Data, actual);

			if (directWrapper && ras is {})
			{
				Assert.AreEqual((ulong)writeTo.Position, ras.Position); // Those should always be equals
				Assert.AreEqual((ulong)writeTo.Length, ras.Size);
			}
		}

		public static async Task Test(IOutputStream writeTo, Stream readOn, bool directWrapper = false)
		{
			var ras = writeTo as IRandomAccessStream;

			// Clear
			readOn.Position = 0;
			readOn.SetLength(0);
			ras?.Seek(0);

			if (directWrapper && ras is { })
			{
				Assert.AreEqual((ulong)0, ras.Position);
				Assert.AreEqual((ulong)readOn.Position, ras.Position); // Those should always be equals
				Assert.AreEqual((ulong)0, ras.Size);
			}

			await WriteData(writeTo);

			if (directWrapper && ras is { })
			{
				Assert.AreEqual((ulong)0, ras.Position);
				Assert.AreEqual((ulong)readOn.Position, ras.Position); // Those should always be equals
				Assert.AreEqual((ulong)readOn.Length, ras.Size);
			}

			var actual = await ReadData(readOn); // The data read on the source should be the data written on the wrapper

			Assert.AreEqual<string>(Data, actual);

			if (directWrapper && ras is { })
			{
				Assert.AreEqual((ulong)readOn.Position, ras.Position); // Those should always be equals
				Assert.AreEqual((ulong)readOn.Length, ras.Size);
			}
		}

		public static async Task WriteData(Stream stream, bool resetPos = true)
		{
			if (resetPos)
			{
				stream.Seek(0, SeekOrigin.Begin);
			}

			var bytes = Encoding.UTF8.GetBytes(Data);
			await stream.WriteAsync(bytes, 0, bytes.Length);
			await stream.FlushAsync();

			if (resetPos)
			{
				stream.Seek(0, SeekOrigin.Begin);
			}
		}

		public static async Task WriteData(IOutputStream stream, bool resetPos = true)
		{
			var ras = stream as IRandomAccessStream;
			if (resetPos)
			{
				ras?.Seek(0);
			}

			var bytes = Encoding.UTF8.GetBytes(Data);
			var buffer = new Windows.Storage.Streams.Buffer((uint)bytes.Length);
			bytes.CopyTo(buffer);
			await stream.WriteAsync(buffer);
			await stream.FlushAsync();

			if (resetPos)
			{
				ras?.Seek(0);
			}
		}

		public static async Task<string> ReadData(Stream stream, bool resetPos = true)
		{
			if (resetPos)
			{
				stream.Seek(0, SeekOrigin.Begin);
			}

			var raw = new byte[512];
			var read = await stream.ReadAsync(raw, 0, raw.Length);
			var data = Encoding.UTF8.GetString(raw, 0, read);

			if (resetPos)
			{
				stream.Seek(0, SeekOrigin.Begin);
			}

			return data;
		}

		public static async Task<string> ReadData(IInputStream stream, bool resetPos = true)
		{
			var ras = stream as IRandomAccessStream;
			if (resetPos)
			{
				ras?.Seek(0);
			}

			var raw = await stream.ReadAsync(new Windows.Storage.Streams.Buffer(512), 512, InputStreamOptions.None);
			var data = Encoding.UTF8.GetString(raw.ToArray(), 0, (int)raw.Length);

			if (resetPos)
			{
				ras?.Seek(0);
			}

			return data;
		}
	}
}
