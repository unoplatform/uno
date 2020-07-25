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

		internal InMemoryBuffer(byte[] data)
		{
			Data = data;
		}

		internal byte[] Data { get; set; }

		public uint Capacity => (uint)Data.Length;

		public uint Length
		{
			get => (uint)Data.Length;
			set => throw new NotSupportedException();
		}
	}
}
