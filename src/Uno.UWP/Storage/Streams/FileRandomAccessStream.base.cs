namespace Windows.Storage.Streams
{
	public partial class FileRandomAccessStream
	{
		private abstract class ImplementationBase
		{
			protected ImplementationBase()
			{
			}

			public abstract ulong Size { get; set; }
			
			public abstract bool CanRead { get; }

			public abstract bool CanWrite { get; }

			public abstract ulong Position { get; }
		}
	}
}
