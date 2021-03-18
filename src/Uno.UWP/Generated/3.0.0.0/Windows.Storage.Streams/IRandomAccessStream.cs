#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.Storage.Streams
{
	#if false
	[global::Uno.NotImplemented]
	#endif
	public  partial interface IRandomAccessStream : global::System.IDisposable,global::Windows.Storage.Streams.IInputStream,global::Windows.Storage.Streams.IOutputStream
	{
		#if false
		bool CanRead
		{
			get;
		}
		#endif
		#if false
		bool CanWrite
		{
			get;
		}
		#endif
		#if false
		ulong Position
		{
			get;
		}
		#endif
		#if false
		ulong Size
		{
			get;
			set;
		}
		#endif
		// Forced skipping of method Windows.Storage.Streams.IRandomAccessStream.Size.get
		// Forced skipping of method Windows.Storage.Streams.IRandomAccessStream.Size.set
		#if false
		global::Windows.Storage.Streams.IInputStream GetInputStreamAt( ulong position);
		#endif
		#if false
		global::Windows.Storage.Streams.IOutputStream GetOutputStreamAt( ulong position);
		#endif
		// Forced skipping of method Windows.Storage.Streams.IRandomAccessStream.Position.get
		#if false
		void Seek( ulong position);
		#endif
		#if false
		global::Windows.Storage.Streams.IRandomAccessStream CloneStream();
		#endif
		// Forced skipping of method Windows.Storage.Streams.IRandomAccessStream.CanRead.get
		// Forced skipping of method Windows.Storage.Streams.IRandomAccessStream.CanWrite.get
	}
}
