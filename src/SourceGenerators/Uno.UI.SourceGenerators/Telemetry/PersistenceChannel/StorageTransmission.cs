#nullable disable

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// 2019/04/12 (Jerome Laban <jerome.laban@nventive.com>):
//	- Extracted from dotnet.exe
//

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Channel;

namespace Uno.UI.SourceGenerators.Telemetry.PersistenceChannel
{
    internal class StorageTransmission : Transmission, IDisposable
    {
        internal Action<StorageTransmission> Disposing;

        protected StorageTransmission(string fullPath, Uri address, byte[] content, string contentType,
            string contentEncoding)
            : base(address, content, contentType, contentEncoding)
        {
            FullFilePath = fullPath;
            FileName = Path.GetFileName(fullPath);
        }

        internal string FileName { get; }

        internal string FullFilePath { get; }

        /// <summary>
        ///     Disposing the storage transmission.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Creates a new transmission from the specified <paramref name="stream" />.
        /// </summary>
        /// <returns>Return transmission loaded from file; return null if the file is corrupted.</returns>
        internal static async Task<StorageTransmission> CreateFromStreamAsync(Stream stream, string fileName)
        {
            StreamReader reader = new StreamReader(stream);
            Uri address = await ReadAddressAsync(reader).ConfigureAwait(false);
            string contentType = await ReadHeaderAsync(reader, "Content-Type").ConfigureAwait(false);
            string contentEncoding = await ReadHeaderAsync(reader, "Content-Encoding").ConfigureAwait(false);
            byte[] content = await ReadContentAsync(reader).ConfigureAwait(false);
            return new StorageTransmission(fileName, address, content, contentType, contentEncoding);
        }

        /// <summary>
        ///     Saves the transmission to the specified <paramref name="stream" />.
        /// </summary>
        internal static async Task SaveAsync(Transmission transmission, Stream stream)
        {
            StreamWriter writer = new StreamWriter(stream);
            try
            {
                await writer.WriteLineAsync(transmission.EndpointAddress.ToString()).ConfigureAwait(false);
                await writer.WriteLineAsync("Content-Type" + ":" + transmission.ContentType).ConfigureAwait(false);
                await writer.WriteLineAsync("Content-Encoding" + ":" + transmission.ContentEncoding)
                    .ConfigureAwait(false);
                await writer.WriteLineAsync(string.Empty).ConfigureAwait(false);
                await writer.WriteAsync(Convert.ToBase64String(transmission.Content)).ConfigureAwait(false);
            }
            finally
            {
                writer.Flush();
            }
        }

        private static async Task<string> ReadHeaderAsync(TextReader reader, string headerName)
        {
            string line = await reader.ReadLineAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(line))
            {
                throw new FormatException(string.Format(CultureInfo.InvariantCulture, "{0} header is expected.",
                    headerName));
            }

            string[] parts = line.Split(':');
            if (parts.Length != 2)
            {
                throw new FormatException(string.Format(CultureInfo.InvariantCulture,
                    "Unexpected header format. {0} header is expected. Actual header: {1}", headerName, line));
            }

            if (parts[0] != headerName)
            {
                throw new FormatException(string.Format(CultureInfo.InvariantCulture,
                    "{0} header is expected. Actual header: {1}", headerName, line));
            }

            return parts[1].Trim();
        }

        private static async Task<Uri> ReadAddressAsync(TextReader reader)
        {
            string addressLine = await reader.ReadLineAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(addressLine))
            {
                throw new FormatException("Transmission address is expected.");
            }

            Uri address = new Uri(addressLine);
            return address;
        }

        private static async Task<byte[]> ReadContentAsync(TextReader reader)
        {
            string content = await reader.ReadToEndAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(content) || content == Environment.NewLine)
            {
                throw new FormatException("Content is expected.");
            }

            return Convert.FromBase64String(content);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                Action<StorageTransmission> disposingDelegate = Disposing;
                disposingDelegate?.Invoke(this);
            }
        }
    }
}
