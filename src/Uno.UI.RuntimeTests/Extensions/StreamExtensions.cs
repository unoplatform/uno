using System.IO;

namespace System
{
	internal static class StreamExtensions
	{
		public static byte[] ReadAllBytes(this Stream instream)
		{
			if (instream is MemoryStream memory)
			{
				return memory.ToArray();
			}

			using (var memoryStream = new MemoryStream())
			{
				instream.CopyTo(memoryStream);
				return memoryStream.ToArray();
			}
		}
	}
}
