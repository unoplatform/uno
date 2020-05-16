using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace Uno.UI.RuntimeTests.Extensions
{
    internal static class ByteArrayExtensions
    {
        public static IBuffer ToBuffer(this byte[] bytes)
		{
			using var writer = new DataWriter();
			writer.WriteBytes(bytes);
			return writer.DetachBuffer();
		}
    }
}
