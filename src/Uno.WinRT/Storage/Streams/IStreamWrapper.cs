#nullable enable

using System;
using System.IO;
using System.Linq;

namespace Windows.Storage.Streams
{
	internal interface IStreamWrapper
	{
		Stream? FindStream();
	}

	interface IInputStreamWrapper
	{
		IInputStream? FindStream();
	}

	interface IOutputStreamWrapper
	{
		IOutputStream? FindStream();
	}

	internal interface IRandomStreamWrapper : IInputStreamWrapper, IOutputStreamWrapper
	{
		new IRandomAccessStream? FindStream();
	}
}
