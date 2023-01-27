// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// 2019/04/12 (Jerome Laban <jerome.laban@nventive.com>):
//	- Extracted from dotnet.exe
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Channel;

namespace Uno.UI.SourceGenerators.Telemetry.PersistenceChannel
{
	internal abstract class BaseStorageService
	{
		/// <summary>
		///     Peeked transmissions dictionary (maps file name to its full path). Holds all the transmissions that were peeked.
		/// </summary>
		/// <remarks>
		///     Note: The value (=file's full path) is not required in the Storage implementation.
		///     If there was a concurrent Abstract Data Type Set it would have been used instead.
		///     However, since there is no concurrent Set, dictionary is used and the second value is ignored.
		/// </remarks>
		protected IDictionary<string, string> PeekedTransmissions;

		/// <summary>
		///     Gets or sets the maximum size of the storage in bytes. When limit is reached, the Enqueue method will drop new
		///     transmissions.
		/// </summary>
		internal ulong CapacityInBytes { get; set; }

		/// <summary>
		///     Gets or sets the maximum number of files. When limit is reached, the Enqueue method will drop new transmissions.
		/// </summary>
		internal uint MaxFiles { get; set; }

		internal abstract string StorageDirectoryPath { get; }

		/// <summary>
		///     Initializes the <see cref="BaseStorageService" />
		/// </summary>
		/// <param name="desireStorageDirectoryPath">A folder name. Under this folder all the transmissions will be saved.</param>
		internal abstract void Init(string desireStorageDirectoryPath);

		internal abstract StorageTransmission Peek();

		internal abstract void Delete(StorageTransmission transmission);

		internal abstract Task EnqueueAsync(Transmission transmission);

		protected void OnPeekedItemDisposed(string fileName)
		{
			try
			{
				if (PeekedTransmissions.ContainsKey(fileName))
				{
					PeekedTransmissions.Remove(fileName);
				}
			}
			catch (Exception e)
			{
				PersistenceChannelDebugLog.WriteException(e, "Failed to remove the item from storage items.");
			}
		}
	}
}
