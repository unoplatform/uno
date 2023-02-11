// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// 2019/04/12 (Jerome Laban <jerome.laban@nventive.com>):
//	- Extracted from dotnet.exe
//

using System;
using System.Threading;
using Microsoft.ApplicationInsights.Channel;
using IChannelTelemetry = Microsoft.ApplicationInsights.Channel.ITelemetry;

namespace Uno.UI.SourceGenerators.Telemetry.PersistenceChannel
{
	/// <summary>
	///     Represents a communication channel for sending telemetry to Application Insights via HTTPS.
	/// </summary>
	internal sealed class PersistenceChannel : ITelemetryChannel
	{
		internal const string TelemetryServiceEndpoint = "https://dc.services.visualstudio.com/v2/track";

		private readonly FlushManager _flushManager;

		private int _disposeCount;
		private readonly BaseStorageService _storage;
		private readonly PersistenceTransmitter _transmitter;

		/// <summary>
		///     Initializes a new instance of the <see cref="PersistenceChannel" /> class.
		/// </summary>
		/// <param name="storageDirectoryPath">
		///     Full path of a directory name. Under this folder all the transmissions will be saved.
		///     Setting this value groups channels, even from different processes.
		///     If 2 (or more) channels has the same <c>storageFolderName</c> only one channel will perform the sending even if the
		///     channel is in a different process/AppDomain/Thread.
		/// </param>
		/// <param name="sendersCount">
		///     Defines the number of senders. A sender is a long-running thread that sends telemetry batches in intervals defined
		///     by <see cref="SendingInterval" />.
		///     So the amount of senders also defined the maximum amount of http channels opened at the same time.
		/// </param>
		public PersistenceChannel(string storageDirectoryPath = null, int sendersCount = 1)
		{
			_storage = new StorageService();
			_storage.Init(storageDirectoryPath);
			_transmitter = new PersistenceTransmitter(_storage, sendersCount);
			_flushManager = new FlushManager(_storage);
			EndpointAddress = TelemetryServiceEndpoint;
		}

		/// <summary>
		///     Gets or sets an interval between each successful sending.
		/// </summary>
		/// <remarks>
		///     On error scenario this value is ignored and the interval will be defined using an exponential back-off
		///     algorithm.
		/// </remarks>
		public TimeSpan? SendingInterval
		{
			get => _transmitter.SendingInterval;
			set => _transmitter.SendingInterval = value;
		}


		/// <summary>
		///     Gets or sets the maximum amount of files allowed in storage. When the limit is reached telemetries will be dropped.
		/// </summary>
		public uint MaxTransmissionStorageFilesCapacity
		{
			get => _storage.MaxFiles;
			set => _storage.MaxFiles = value;
		}

		/// <summary>
		///     This flag has no effect. But it is required by base class
		/// </summary>
		public bool? DeveloperMode { get; set; }

		/// <summary>
		///     Gets or sets the HTTP address where the telemetry is sent.
		/// </summary>
		public string EndpointAddress
		{
			get => _flushManager.EndpointAddress.ToString();

			set
			{
				string address = value ?? TelemetryServiceEndpoint;
				_flushManager.EndpointAddress = new Uri(address);
			}
		}

		/// <summary>
		///     Releases unmanaged and - optionally - managed resources.
		/// </summary>
		public void Dispose()
		{
			if (Interlocked.Increment(ref _disposeCount) == 1)
			{
				_transmitter?.Dispose();
			}
		}

		/// <summary>
		///     Sends an instance of ITelemetry through the channel.
		/// </summary>
		public void Send(IChannelTelemetry item)
		{
			_flushManager.Flush(item);
		}

		/// <summary>
		///     No operation, send will always flush. So nothing will be in memory
		/// </summary>
		public void Flush()
		{
		}
	}
}
