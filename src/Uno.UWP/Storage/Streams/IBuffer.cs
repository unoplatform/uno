namespace Windows.Storage.Streams
{
	public partial interface IBuffer
	{
		uint Capacity
		{
			get;
		}

		uint Length
		{
			get;
			set;
		}

		// should be `internal`, but it requires C# 8.0
		byte[] Data { get; }

	}
}
