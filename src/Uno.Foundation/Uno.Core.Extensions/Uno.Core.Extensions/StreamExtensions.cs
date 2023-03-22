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
	}
}
