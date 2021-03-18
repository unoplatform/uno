namespace Windows.Storage.Streams
{
	/// <summary>
	/// This class is no longer needed and can be removed
	/// as part of breaking changes batch (all its occurences can be replaced by
	/// Buffer class, which has equivalent functionality.
	/// </summary>
	public class InMemoryBuffer : Buffer, IBuffer
	{
		internal InMemoryBuffer(int capacity)
			: base((uint)capacity)
		{			
		}

		internal InMemoryBuffer(byte[] data)
			: base(data)
		{
		}
	}
}
