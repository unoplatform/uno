using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;

namespace Windows.ApplicationModel.DataTransfer
{
	public static partial class Clipboard
	{
		[DataContract]
		private class MarshalledData: IDisposable
		{
			private bool _disposed;
			private IntPtr _ptr;

			[DataMember(Name = "ptr")]
			private long _longPtr
			{
				get => (long)_ptr;
				set => _ptr = (IntPtr)value;
			}

			[DataMember(Name = "len")]
			private int _len;

			public MarshalledData()
			{
			}

			public MarshalledData(byte[] arr)
			{
				_ptr = Marshal.AllocHGlobal(arr.Length);
				_len = arr.Length;
				Marshal.Copy(arr, 0, _ptr, _len);
			}

			public MarshalledData(string str)
				: this(Encoding.UTF8.GetBytes(str))
			{
			}

			public unsafe override string ToString()
			{
				return Encoding.UTF8.GetString((byte*)_ptr, _len);
			}

			public byte[] ToArray()
			{
				var arr = new byte[_len];
				Marshal.Copy(_ptr, arr, 0, _len);
				return arr;
			}

			protected virtual void Dispose(bool disposing)
			{
				if (!_disposed)
				{
					var toFree = Interlocked.Exchange(ref _ptr, IntPtr.Zero);
					Marshal.FreeHGlobal(toFree);
					_disposed = true;
				}
			}

			~MarshalledData()
			{
				Dispose(disposing: false);
			}

			public void Dispose()
			{
				Dispose(disposing: true);
				GC.SuppressFinalize(this);
			}
		}

		[DataContract]
		private class MarshalledDataWithMime: MarshalledData
		{
			[DataMember(Name = "mime")]
			private string _mime;

			public MarshalledDataWithMime(byte[] arr, string mime)
				: base(arr)
			{
				_mime = mime;
			}

			public MarshalledDataWithMime(string str, string mime)
				: base(str)
			{
				_mime = mime;
			}
		}
    }
}
