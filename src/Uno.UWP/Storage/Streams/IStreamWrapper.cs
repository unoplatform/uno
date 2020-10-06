#nullable enable

using System;
using System.IO;
using System.Linq;

namespace Windows.Storage.Streams
{
	internal interface IStreamWrapper
	{
		Stream GetStream();
	}

	interface IInputStreamWrapper
	{
		IInputStream GetStream();
	}

	interface IOutputStreamWrapper
	{
		IOutputStream GetStream();
	}

	internal interface IRandomStreamWrapper : IInputStreamWrapper, IOutputStreamWrapper
	{
		new IRandomAccessStream GetStream();
	}
}
