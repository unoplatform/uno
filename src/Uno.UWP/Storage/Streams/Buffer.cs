using System;

namespace Windows.Storage.Streams
{
	public partial class Buffer : IBuffer
	{
		public Buffer(uint capacity)
		{
			Data = new byte[capacity];
		}

		internal Buffer(byte[] data)
		{
			Data = data;
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
