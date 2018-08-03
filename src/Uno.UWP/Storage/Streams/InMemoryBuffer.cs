using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.Storage.Streams
{
	public class InMemoryBuffer : IBuffer
	{
		internal InMemoryBuffer(int capacity)
		{
			Data = new byte[capacity];
		}

		internal byte[] Data { get; }

		public uint Capacity => (uint)Data.Length;

		public uint Length
		{
			get => (uint)Data.Length;
			set => throw new NotSupportedException();
		}
	}
}
