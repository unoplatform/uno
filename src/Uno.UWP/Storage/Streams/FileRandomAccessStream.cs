using System;
using System.IO;

namespace Windows.Storage.Streams
{
	public partial class FileRandomAccessStream : IRandomAccessStream, IInputStream, IOutputStream, IDisposable, IStreamWrapper
	{
		private ImplementationBase _implementation;

        private FileRandomAccessStream(ImplementationBase implementation)
		{
			_implementation = implementation;
		}

		public Stream FindStream() => throw new NotImplementedException();

		internal FileRandomAccessStream GetLocal(string path, FileAccess access, FileShare share)
		{
			var localImplementation = new Local(path, access, share);
			return new FileRandomAccessStream(localImplementation);
		}
    }
}
