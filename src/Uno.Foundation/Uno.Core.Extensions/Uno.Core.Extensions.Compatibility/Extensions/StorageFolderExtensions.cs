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
#if HAS_ISTORAGEFILE
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Uno.Extensions
{
	internal static class StorageFolderExtensions
	{
		public static async Task<IStorageFile> TryGetFileAsync(this IStorageFolder folder, CancellationToken ct, string name)
		{
#if HAS_ISTORAGEFILE_ADVANCED
			var folder2 = folder as IStorageFolder2;
			if (folder2 != null)
			{
				return await folder2.TryGetItemAsync(name).AsTask(ct) as IStorageFile;
			}
#endif
#if SILVERLIGHT
			// Prevent exception handling on platforms supporting file existance checking
			if (folder.IsOfType(StorageItemTypes.Folder) && !File.Exists(Path.Combine(folder.Path, name)))
			{
				return null;
			}
#endif
			try
			{
				return await folder.GetFileAsync(name).AsTask(ct);
			}
			catch (FileNotFoundException)
			{
				return null;
			}
		}
	}
}
#endif