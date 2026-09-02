namespace Windows.Storage.Streams
{
	public partial interface IBuffer
	{
		uint Capacity { get; }

		uint Length { get; set; }
	}
}
