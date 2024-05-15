// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//
// 2019/04/12 (Jerome Laban <jerome.laban@nventive.com>):
//	- Extracted from dotnet.exe
//

using System;
using System.Globalization;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.SourceGenerators.Telemetry.PersistenceChannel
{
	/// <summary>
	///     Fetch transmissions from the storage and sends it.
	/// </summary>
	internal class Sender : IDisposable
	{
		/// <summary>
		///     The default sending interval.
		/// </summary>
		private readonly TimeSpan _defaultSendingInterval;

		/// <summary>
		///     A wait handle that flags the sender when to start sending again. The type is protected for unit test.
		/// </summary>
		protected readonly AutoResetEvent DelayHandler;

		/// <summary>
		///     Holds the maximum time for the exponential back-off algorithm. The sending interval will grow on every HTTP
		///     Exception until this max value.
		/// </summary>
		private readonly TimeSpan _maxIntervalBetweenRetries = TimeSpan.FromHours(1);

		/// <summary>
		///     When storage is empty it will be queried again after this interval.
		///     Decreasing to 5 sec to send first data (users and sessions).
		/// </summary>
		private readonly TimeSpan _sendingIntervalOnNoData = TimeSpan.FromSeconds(5);

		/// <summary>
		///     A wait handle that is being set when Sender is no longer sending.
		/// </summary>
		private readonly AutoResetEvent _stoppedHandler;

		/// <summary>
		///     The number of times this object was disposed.
		/// </summary>
		private int _disposeCount;

		/// <summary>
		///     The amount of time to wait, in the stop method, until the last transmission is sent.
		///     If time expires, the stop method will return even if the transmission hasn't been sent.
		/// </summary>
		private readonly TimeSpan _drainingTimeout;

		/// <summary>
		///     A boolean value that indicates if the sender should be stopped. The sender's while loop is checking this boolean
		///     value.
		/// </summary>
		private bool _stopped;

		/// <summary>
		///     The transmissions storage.
		/// </summary>
		private readonly BaseStorageService _storage;

		/// <summary>
		///     Holds the transmitter.
		/// </summary>
		private readonly PersistenceTransmitter _transmitter;

		/// <summary>
		///     Initializes a new instance of the <see cref="Sender" /> class.
		/// </summary>
		/// <param name="storage">The storage that holds the transmissions to send.</param>
		/// <param name="transmitter">
		///     The persistence transmitter that manages this Sender.
		///     The transmitter will be used as a configuration class, it exposes properties like SendingInterval that will be read
		///     by Sender.
		/// </param>
		/// <param name="startSending">
		///     A boolean value that determines if Sender should start sending immediately. This is only
		///     used for unit tests.
		/// </param>
		internal Sender(BaseStorageService storage, PersistenceTransmitter transmitter, bool startSending = true)
		{
			_stopped = false;
			DelayHandler = new AutoResetEvent(false);
			_stoppedHandler = new AutoResetEvent(false);
			_drainingTimeout = TimeSpan.FromSeconds(100);
			_defaultSendingInterval = TimeSpan.FromSeconds(5);

			_transmitter = transmitter;
			_storage = storage;

			if (startSending)
			{
				// It is currently possible for the long - running task to be executed(and thereby block during WaitOne) on the UI thread when
				// called by a task scheduled on the UI thread. Explicitly specifying TaskScheduler.Default 
				// when calling StartNew guarantees that Sender never blocks the main thread.
				Task.Factory.StartNew(SendLoop, CancellationToken.None, TaskCreationOptions.LongRunning,
						TaskScheduler.Default)
					.ContinueWith(
						t => PersistenceChannelDebugLog.WriteException(t.Exception, "Sender: Failure in SendLoop"),
						TaskContinuationOptions.OnlyOnFaulted);
			}
		}

		/// <summary>
		///     Gets the interval between each successful sending.
		/// </summary>
		private TimeSpan SendingInterval
		{
			get
			{
				if (_transmitter.SendingInterval != null)
				{
					return _transmitter.SendingInterval.Value;
				}

				return _defaultSendingInterval;
			}
		}

		/// <summary>
		///     Disposes the managed objects.
		/// </summary>
		public void Dispose()
		{
			if (Interlocked.Increment(ref _disposeCount) == 1)
			{
				StopAsync().ConfigureAwait(false).GetAwaiter().GetResult();

				DelayHandler.Dispose();
				_stoppedHandler.Dispose();
			}
		}

		/// <summary>
		///     Stops the sender.
		/// </summary>
		internal Task StopAsync()
		{
			// After delayHandler is set, a sending iteration will immediately start. 
			// Setting <c>stopped</c> to true, will cause the iteration to skip the actual sending and stop immediately. 
			_stopped = true;
			DelayHandler.Set();

			// if delayHandler was set while a transmission was being sent, the return task will wait for it to finish, for an additional second,
			// before it will mark the task as completed. 
			return Task.Run(() =>
			{
				try
				{
					_stoppedHandler.WaitOne(_drainingTimeout);
				}
				catch (ObjectDisposedException)
				{
				}
			});
		}

		/// <summary>
		///     Send transmissions in a loop.
		/// </summary>
		protected void SendLoop()
		{
			TimeSpan prevSendingInterval = TimeSpan.Zero;
			TimeSpan sendingInterval = _sendingIntervalOnNoData;
			try
			{
				while (!_stopped)
				{
					using (StorageTransmission transmission = _storage.Peek())
					{
						if (_stopped)
						{
							// This second verification is required for cases where 'stopped' was set while peek was happening. 
							// Once the actual sending starts the design is to wait until it finishes and deletes the transmission. 
							// So no extra validation is required.
							break;
						}

						// If there is a transmission to send - send it. 
						if (transmission != null)
						{
							bool shouldRetry = Send(transmission, ref sendingInterval);
							if (!shouldRetry)
							{
								// If retry is not required - delete the transmission.
								_storage.Delete(transmission);
							}
						}
						else
						{
							sendingInterval = _sendingIntervalOnNoData;
						}
					}

					LogInterval(prevSendingInterval, sendingInterval);
					DelayHandler.WaitOne(sendingInterval);
					prevSendingInterval = sendingInterval;
				}

				_stoppedHandler.Set();
			}
			catch (ObjectDisposedException)
			{
			}
		}

		/// <summary>
		///     Sends a transmission and handle errors.
		/// </summary>
		/// <param name="transmission">The transmission to send.</param>
		/// <param name="nextSendInterval">
		///     When this value returns it will hold a recommendation for when to start the next sending
		///     iteration.
		/// </param>
		/// <returns>True, if there was sent error and we need to retry sending, otherwise false.</returns>
		protected virtual bool Send(StorageTransmission transmission, ref TimeSpan nextSendInterval)
		{
			try
			{
				if (transmission != null)
				{
					bool isConnected = NetworkInterface.GetIsNetworkAvailable();

					// there is no internet connection available, return than.
					if (!isConnected)
					{
						PersistenceChannelDebugLog.WriteLine(
							"Cannot send data to the server. Internet connection is not available");
						return true;
					}

					transmission.SendAsync().ConfigureAwait(false).GetAwaiter().GetResult();

					// After a successful sending, try immediately to send another transmission. 
					nextSendInterval = SendingInterval;
				}
			}
			catch (WebException e)
			{
				int? statusCode = GetStatusCode(e);
				nextSendInterval = CalculateNextInterval(statusCode, nextSendInterval, _maxIntervalBetweenRetries);
				return IsRetryable(statusCode, e.Status);
			}
			catch (Exception e)
			{
				nextSendInterval = CalculateNextInterval(null, nextSendInterval, _maxIntervalBetweenRetries);
				PersistenceChannelDebugLog.WriteException(e, "Unknown exception during sending");
			}

			return false;
		}

		/// <summary>
		///     Log next interval. Only log the interval when it changes by more then a minute. So if interval grow by 1 minute or
		///     decreased by 1 minute it will be logged.
		///     Logging every interval will just make the log noisy.
		/// </summary>
		private static void LogInterval(TimeSpan prevSendInterval, TimeSpan nextSendInterval)
		{
			if (Math.Abs(nextSendInterval.TotalSeconds - prevSendInterval.TotalSeconds) > 60)
			{
				PersistenceChannelDebugLog.WriteLine("next sending interval: " + nextSendInterval);
			}
		}

		/// <summary>
		///     Return the status code from the web exception or null if no such code exists.
		/// </summary>
		private static int? GetStatusCode(WebException e)
		{
			if (e.Response is HttpWebResponse httpWebResponse)
			{
				return (int)httpWebResponse.StatusCode;
			}

			return null;
		}

		/// <summary>
		///     Returns true if <paramref name="httpStatusCode" /> or <paramref name="webExceptionStatus" /> are retryable.
		/// </summary>
		private static bool IsRetryable(int? httpStatusCode, WebExceptionStatus webExceptionStatus)
		{
			switch (webExceptionStatus)
			{
				case WebExceptionStatus.ProxyNameResolutionFailure:
				case WebExceptionStatus.NameResolutionFailure:
				case WebExceptionStatus.Timeout:
				case WebExceptionStatus.ConnectFailure:
					return true;
			}

			if (httpStatusCode == null)
			{
				return false;
			}

			switch (httpStatusCode.Value)
			{
				case 503: // Server in maintenance. 
				case 408: // invalid request
				case 500: // Internal Server Error                                                
				case 502: // Bad Gateway, can be common when there is no network. 
				case 511: // Network Authentication Required
					return true;
			}

			return false;
		}

		/// <summary>
		///     Calculates the next interval using exponential back-off algorithm (with the exceptions of few error codes that
		///     reset the interval to <see cref="SendingInterval" />.
		/// </summary>
		private TimeSpan CalculateNextInterval(int? httpStatusCode, TimeSpan currentSendInterval, TimeSpan maxInterval)
		{
			// if item is expired, no need for exponential back-off
			if (httpStatusCode != null && httpStatusCode.Value == 400 /* expired */)
			{
				return SendingInterval;
			}

			// exponential back-off.
			if (Math.Abs(currentSendInterval.TotalSeconds) < 1)
			{
				return TimeSpan.FromSeconds(1);
			}

			double nextIntervalInSeconds = Math.Min(currentSendInterval.TotalSeconds * 2, maxInterval.TotalSeconds);

			return TimeSpan.FromSeconds(nextIntervalInSeconds);
		}
	}
}
