// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// 2019/04/12 (Jerome Laban <jerome.laban@nventive.com>):
//	- Extracted from dotnet.exe
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Channel;

namespace Uno.UI.SourceGenerators.Telemetry.PersistenceChannel
{
	internal sealed class StorageService : BaseStorageService
	{
		private const string DefaultStorageFolderName = "TelemetryStorageService";
		private readonly FixedSizeQueue<string> _deletedFilesQueue = new FixedSizeQueue<string>(10);

		private readonly object _peekLockObj = new object();
		private readonly object _storageFolderLock = new object();
		private string _storageDirectoryPath;
		private string _storageDirectoryPathUsed;
		private long _storageCountFiles;
		private bool _storageFolderInitialized;
		private long _storageSize;
		private uint _transmissionsDropped;

		/// <summary>
		///     Gets the storage's folder name.
		/// </summary>
		internal override string StorageDirectoryPath => _storageDirectoryPath;

		/// <summary>
		///     Gets the storage folder. If storage folder couldn't be created, null will be returned.
		/// </summary>
		private string StorageFolder
		{
			get
			{
				if (!_storageFolderInitialized)
				{
					lock (_storageFolderLock)
					{
						if (!_storageFolderInitialized)
						{
							try
							{
								_storageDirectoryPathUsed = _storageDirectoryPath;

								if (!Directory.Exists(_storageDirectoryPathUsed))
								{
									Directory.CreateDirectory(_storageDirectoryPathUsed);
								}
							}
							catch (Exception e)
							{
								_storageDirectoryPathUsed = null;
								PersistenceChannelDebugLog.WriteException(e, "Failed to create storage folder");
							}

							_storageFolderInitialized = true;
						}
					}
				}

				return _storageDirectoryPathUsed;
			}
		}

		internal override void Init(string storageDirectoryPath)
		{
			PeekedTransmissions = new SnapshottingDictionary<string, string>();

			VerifyOrSetDefaultStorageDirectoryPath(storageDirectoryPath);

			CapacityInBytes = 10 * 1024 * 1024; // 10 MB
			MaxFiles = 100;

			Task.Run(DeleteObsoleteFiles)
				.ContinueWith(
					task =>
					{
						PersistenceChannelDebugLog.WriteException(
							task.Exception,
							"Storage: Unhandled exception in DeleteObsoleteFiles");
					},
					TaskContinuationOptions.OnlyOnFaulted);
		}

		private void VerifyOrSetDefaultStorageDirectoryPath(string desireStorageDirectoryPath)
		{
			if (string.IsNullOrEmpty(desireStorageDirectoryPath))
			{
				_storageDirectoryPath = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
					DefaultStorageFolderName);
			}
			else
			{
				if (!Path.IsPathRooted(desireStorageDirectoryPath))
				{
					throw new ArgumentException($"{nameof(desireStorageDirectoryPath)} need to be rooted (full path)");
				}

				_storageDirectoryPath = desireStorageDirectoryPath;
			}
		}

		/// <summary>
		///     Reads an item from the storage. Order is Last-In-First-Out.
		///     When the Transmission is no longer needed (it was either sent or failed with a non-retryable error) it should be
		///     disposed.
		/// </summary>
		internal override StorageTransmission Peek()
		{
			IEnumerable<string> files = GetFiles("*.trn", 50);

			lock (_peekLockObj)
			{
				foreach (string file in files)
				{
					try
					{
						// if a file was peeked before, skip it (wait until it is disposed).  
						if (PeekedTransmissions.ContainsKey(file) == false &&
							_deletedFilesQueue.Contains(file) == false)
						{
							// Load the transmission from disk.
							StorageTransmission storageTransmissionItem = LoadTransmissionFromFileAsync(file)
								.ConfigureAwait(false).GetAwaiter().GetResult();

							// when item is disposed it should be removed from the peeked list.
							storageTransmissionItem.Disposing = item => OnPeekedItemDisposed(file);

							// add the transmission to the list.
							PeekedTransmissions.Add(file, storageTransmissionItem.FullFilePath);
							return storageTransmissionItem;
						}
					}
					catch (Exception e)
					{
						PersistenceChannelDebugLog.WriteException(
							e,
							"Failed to load an item from the storage. file: {0}",
							file);
					}
				}
			}

			return null;
		}

		internal override void Delete(StorageTransmission item)
		{
			try
			{
				if (StorageFolder == null)
				{
					return;
				}

				// Initial storage size calculation. 
				CalculateSize();

				long fileSize = GetSize(item.FileName);
				File.Delete(Path.Combine(StorageFolder, item.FileName));

				_deletedFilesQueue.Enqueue(item.FileName);

				// calculate size                
				Interlocked.Add(ref _storageSize, -fileSize);
				Interlocked.Decrement(ref _storageCountFiles);
			}
			catch (IOException e)
			{
				PersistenceChannelDebugLog.WriteException(e, "Failed to delete a file. file: {0}", item == null ? "null" : item.FullFilePath);
			}
		}

		internal override async Task EnqueueAsync(Transmission transmission)
		{
			try
			{
				if (transmission == null || StorageFolder == null)
				{
					return;
				}

				// Initial storage size calculation. 
				CalculateSize();

				if ((ulong)_storageSize >= CapacityInBytes || _storageCountFiles >= MaxFiles)
				{
					// if max storage capacity has reached, drop the transmission (but log every 100 lost transmissions). 
					if (_transmissionsDropped++ % 100 == 0)
					{
						PersistenceChannelDebugLog.WriteLine("Total transmissions dropped: " + _transmissionsDropped);
					}

					return;
				}

				// Writes content to a temporary file and only then rename to avoid the Peek from reading the file before it is being written.
				// Creates the temp file name
				string tempFileName = Guid.NewGuid().ToString("N");

				// Now that the file got created we can increase the files count
				Interlocked.Increment(ref _storageCountFiles);

				// Saves transmission to the temp file
				await SaveTransmissionToFileAsync(transmission, tempFileName).ConfigureAwait(false);

				// Now that the file is written increase storage size. 
				long temporaryFileSize = GetSize(tempFileName);
				Interlocked.Add(ref _storageSize, temporaryFileSize);

				// Creates a new file name
				string now = DateTime.UtcNow.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);
				string newFileName = string.Format(CultureInfo.InvariantCulture, "{0}_{1}.trn", now, tempFileName);

				// Renames the file
				File.Move(Path.Combine(StorageFolder, tempFileName), Path.Combine(StorageFolder, newFileName));
			}
			catch (Exception e)
			{
				PersistenceChannelDebugLog.WriteException(e, "EnqueueAsync");
			}
		}

		private async Task SaveTransmissionToFileAsync(Transmission transmission, string file)
		{
			try
			{
				using (Stream stream = File.OpenWrite(Path.Combine(StorageFolder, file)))
				{
					await StorageTransmission.SaveAsync(transmission, stream).ConfigureAwait(false);
				}
			}
			catch (UnauthorizedAccessException)
			{
				string message =
					string.Format(
						CultureInfo.InvariantCulture,
						"Failed to save transmission to file. UnauthorizedAccessException. File path: {0}, FileName: {1}",
						StorageFolder, file);
				PersistenceChannelDebugLog.WriteLine(message);
				throw;
			}
		}

		private async Task<StorageTransmission> LoadTransmissionFromFileAsync(string file)
		{
			try
			{
				using (Stream stream = File.OpenRead(Path.Combine(StorageFolder, file)))
				{
					StorageTransmission storageTransmissionItem =
						await StorageTransmission.CreateFromStreamAsync(stream, file).ConfigureAwait(false);
					return storageTransmissionItem;
				}
			}
			catch (Exception e)
			{
				string message =
					string.Format(
						CultureInfo.InvariantCulture,
						"Failed to load transmission from file. File path: {0}, FileName: {1}, Exception: {2}",
						"storageFolderName", file, e);
				PersistenceChannelDebugLog.WriteLine(message);
				throw;
			}
		}

		/// <summary>
		///     Get files from <see cref="storageFolder" />.
		/// </summary>
		/// <param name="fileQuery">Define the logic for sorting the files.</param>
		/// <param name="filterByExtension">Defines a file extension. This method will return only files with this extension.</param>
		/// <param name="top">
		///     Define how many files to return. This can be useful when the directory has a lot of files, in that case
		///     GetFilesAsync will have a performance hit.
		/// </param>
		private IEnumerable<string> GetFiles(string filterByExtension, int top)
		{
			try
			{
				if (StorageFolder != null)
				{
					return Directory.GetFiles(StorageFolder, filterByExtension).Take(top);
				}
			}
			catch (Exception e)
			{
				PersistenceChannelDebugLog.WriteException(e, "Peek failed while get files from storage.");
			}

			return Enumerable.Empty<string>();
		}

		/// <summary>
		///     Gets a file's size.
		/// </summary>
		private long GetSize(string file)
		{
			using (FileStream stream = File.OpenRead(Path.Combine(StorageFolder, file)))
			{
				return stream.Length;
			}
		}

		/// <summary>
		///     Check the storage limits and return true if they reached.
		///     Storage limits are defined by the number of files and the total size on disk.
		/// </summary>
		private void CalculateSize()
		{
			string[] storageFiles = Directory.GetFiles(StorageFolder, "*.*");

			_storageCountFiles = storageFiles.Length;

			long storageSizeInBytes = 0;
			foreach (string file in storageFiles)
			{
				storageSizeInBytes += GetSize(file);
			}

			_storageSize = storageSizeInBytes;
		}

		/// <summary>
		///     Enqueue is saving a transmission to a <c>tmp</c> file and after a successful write operation it renames it to a
		///     <c>trn</c> file.
		///     A file without a <c>trn</c> extension is ignored by Storage.Peek(), so if a process is taken down before rename
		///     happens
		///     it will stay on the disk forever.
		///     This thread deletes files with the <c>tmp</c> extension that exists on disk for more than 5 minutes.
		/// </summary>
		private void DeleteObsoleteFiles()
		{
			try
			{
				IEnumerable<string> files = GetFiles("*.tmp", 50);
				foreach (string file in files)
				{
					DateTime creationTime = File.GetCreationTimeUtc(Path.Combine(StorageFolder, file));
					// if the file is older then 5 minutes - delete it.
					if (DateTime.UtcNow - creationTime >= TimeSpan.FromMinutes(5))
					{
						File.Delete(Path.Combine(StorageFolder, file));
					}
				}
			}
			catch (Exception e)
			{
				PersistenceChannelDebugLog.WriteException(e, "Failed to delete tmp files.");
			}
		}
	}
}
