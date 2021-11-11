// ******************************************************************
// Copyright � 2015-2018 nventive inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// ******************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace Uno.Extensions
{
	internal static class StreamExtensions
	{
		public static async Task<byte[]> ReadBytesAsync(this Stream stream)
		{
			byte[] readBuffer = new byte[stream.CanSeek ? (stream.Length - stream.Position) : 4096];

			int totalBytesRead = 0;
			int bytesRead;

			while ((bytesRead = await stream.ReadAsync(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
			{
				totalBytesRead += bytesRead;

				if (totalBytesRead == readBuffer.Length)
				{
					var nextBytes = new byte[1];
					var read = await stream.ReadAsync(nextBytes, 0, 1);

					if (read == 1)
					{
						byte[] temp = new byte[readBuffer.Length * 2];
						System.Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
						System.Buffer.SetByte(temp, totalBytesRead, (byte)nextBytes[0]);
						readBuffer = temp;
						totalBytesRead++;
					}
				}
			}

			byte[] buffer = readBuffer;
			if (readBuffer.Length != totalBytesRead)
			{
				buffer = new byte[totalBytesRead];
				System.Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
			}
			return buffer;
		}

		public static byte[] ReadBytes(this Stream stream)
		{
			if (stream.CanSeek && (stream.Position == 0))
			{
				var sourceMemoryStream = stream as MemoryStream;
				if (sourceMemoryStream != null)
				{
					// MemoryStream.ToArray() is already optimized, so use it when possible
					return sourceMemoryStream.ToArray();
				}
			}

			byte[] readBuffer = new byte[stream.CanSeek ? (stream.Length - stream.Position) : 4096];

			int totalBytesRead = 0;
			int bytesRead;

			while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
			{
				totalBytesRead += bytesRead;

				if (totalBytesRead == readBuffer.Length)
				{
					int nextByte = stream.ReadByte();
					if (nextByte != -1)
					{
						byte[] temp = new byte[readBuffer.Length * 2];
						System.Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
						System.Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
						readBuffer = temp;
						totalBytesRead++;
					}
				}
			}

			byte[] buffer = readBuffer;
			if (readBuffer.Length != totalBytesRead)
			{
				buffer = new byte[totalBytesRead];
				System.Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
			}
			return buffer;
		}

		/// <summary>
		/// Reads the text container into the specified stream.
		/// </summary>
		/// <param name="stream"></param>
		/// <returns>The string using the default encoding.</returns>
		/// <remarks>The stream will be disposed when calling this method.</remarks>
		public static string ReadToEnd(this Stream stream)
		{
			string value;
			using (var reader = new StreamReader(stream))
			{
				value = reader.ReadToEnd();
			}
			return value;
		}


		/// <summary>
		/// Reads the text container into the specified stream.
		/// </summary>
		/// <param name="stream"></param>
		/// <returns>The string using the default encoding.</returns>
		/// <remarks>The stream will be disposed when calling this method.</remarks>
		public static string ReadToEnd(this Stream stream, Encoding encoding)
		{
			string value;
			using (var reader = new StreamReader(stream, encoding))
			{
				value = reader.ReadToEnd();
			}
			return value;
		}

		/// <summary>
		/// Warning, if stream cannot be seek, will read from current position!
		/// Warning, stream position will not been restored!
		/// </summary>
		/// <param name="stream"></param>
		/// <param name="start"></param>
		/// <returns></returns>
		public static bool StartsWith(this Stream stream, byte[] start)
		{
			if (stream.CanSeek)
			{
				stream.Position = 0;
			}

			var buffer = new byte[start.Length];

			stream.Read(buffer, 0, buffer.Length);

			return start.SequenceEqual(buffer);
		}

		/// <summary>
		/// Create a MemoryStream, copy <see cref="source"/> to it, and set position to 0.
		/// </summary>
		/// <param name="source">Stream to copy</param>
		/// <returns>Newly created memory stream, position set to 0</returns>
		public static MemoryStream ToMemoryStream(this Stream source)
		{
			var stream = new MemoryStream();
			source.CopyTo(stream);
			stream.Position = 0;

			return stream;
		}

		/// <summary>
		/// Check if <see cref="stream"/> is seekable (CanSeek), if not copy it to a MemoryStream. 
		/// WARNING: Some stream (like UnmanagedMemoryStream) return CanSeek = true but are not seekable. Prefer using ToMemoryStream() to be 100% safe.
		/// </summary>
		/// <param name="stream">A stream</param>
		/// <returns>A seekable stream (orginal if seekable, a MemoryStream copy of <see cref="stream"/> else)</returns>
		public static Stream ToSeekable(this Stream stream)
		{
			return stream.CanSeek ? stream : stream.ToMemoryStream();
		}
	}
}
