//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.Collections.ObjectModel;
using Windows.Devices.Enumeration;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_Devices.Midi
{
	/// <summary>
	/// DeviceWatcher class to monitor adding/removing MIDI devices on the fly
	/// </summary>
	internal class MidiDeviceWatcher
	{
		internal DeviceWatcher deviceWatcher = null;
		internal DeviceInformationCollection deviceInformationCollection = null;
		bool enumerationCompleted = false;
		ListView portList = null;
		string midiSelector = string.Empty;
		CoreDispatcher coreDispatcher = null;

		/// <summary>
		/// Constructor: Initialize and hook up Device Watcher events
		/// </summary>
		/// <param name="midiSelectorString">MIDI Device Selector</param>
		/// <param name="dispatcher">CoreDispatcher instance, to update UI thread</param>
		/// <param name="portListBox">The UI element to update with list of devices</param>
		internal MidiDeviceWatcher(string midiSelectorString, CoreDispatcher dispatcher, ListView portListBox)
		{
			this.deviceWatcher = DeviceInformation.CreateWatcher(midiSelectorString);
			this.portList = portListBox;
			this.midiSelector = midiSelectorString;
			this.coreDispatcher = dispatcher;

			this.deviceWatcher.Added += DeviceWatcher_Added;
			this.deviceWatcher.Removed += DeviceWatcher_Removed;
			this.deviceWatcher.Updated += DeviceWatcher_Updated;
			this.deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
		}

		/// <summary>
		/// Destructor: Remove Device Watcher events
		/// </summary>
		~MidiDeviceWatcher()
		{
			this.deviceWatcher.Added -= DeviceWatcher_Added;
			this.deviceWatcher.Removed -= DeviceWatcher_Removed;
			this.deviceWatcher.Updated -= DeviceWatcher_Updated;
			this.deviceWatcher.EnumerationCompleted -= DeviceWatcher_EnumerationCompleted;
		}

		/// <summary>
		/// Start the Device Watcher
		/// </summary>
		internal void Start()
		{
			if (this.deviceWatcher.Status != DeviceWatcherStatus.Started)
			{
				this.deviceWatcher.Start();
			}
		}

		/// <summary>
		/// Stop the Device Watcher
		/// </summary>
		internal void Stop()
		{
			if (this.deviceWatcher.Status != DeviceWatcherStatus.Stopped)
			{
				this.deviceWatcher.Stop();
			}
		}

		/// <summary>
		/// Get the DeviceInformationCollection
		/// </summary>
		/// <returns></returns>
		internal DeviceInformationCollection GetDeviceInformationCollection()
		{
			return this.deviceInformationCollection;
		}

		/// <summary>
		/// Add any connected MIDI devices to the list
		/// </summary>
		private async void UpdateDevices()
		{
			// Get a list of all MIDI devices
			this.deviceInformationCollection = await DeviceInformation.FindAllAsync(this.midiSelector);

			// If no devices are found, update the ListBox
			if ((this.deviceInformationCollection == null) || (this.deviceInformationCollection.Count == 0))
			{
				// Start with a clean list
				this.portList.Items.Clear();

				this.portList.Items.Add("No MIDI ports found");
				this.portList.IsEnabled = false;
			}
			// If devices are found, enumerate them and add them to the list
			else
			{
				// Start with a clean list
				this.portList.Items.Clear();

				foreach (var device in deviceInformationCollection)
				{
					this.portList.Items.Add(device.Name);
				}

				this.portList.IsEnabled = true;
			}
		}

		/// <summary>
		/// Update UI on device added
		/// </summary>
		/// <param name="sender">The active DeviceWatcher instance</param>
		/// <param name="args">Event arguments</param>
		private async void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
		{
			// If all devices have been enumerated
			if (this.enumerationCompleted)
			{
				await coreDispatcher.RunAsync(CoreDispatcherPriority.High, () =>
				{
					// Update the device list
					UpdateDevices();
				});
			}
		}

		/// <summary>
		/// Update UI on device removed
		/// </summary>
		/// <param name="sender">The active DeviceWatcher instance</param>
		/// <param name="args">Event arguments</param>
		private async void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
		{
			// If all devices have been enumerated
			if (this.enumerationCompleted)
			{
				await coreDispatcher.RunAsync(CoreDispatcherPriority.High, () =>
				{
					// Update the device list
					UpdateDevices();
				});
			}
		}

		/// <summary>
		/// Update UI on device updated
		/// </summary>
		/// <param name="sender">The active DeviceWatcher instance</param>
		/// <param name="args">Event arguments</param>
		private async void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
		{
			// If all devices have been enumerated
			if (this.enumerationCompleted)
			{
				await coreDispatcher.RunAsync(CoreDispatcherPriority.High, () =>
				{
					// Update the device list
					UpdateDevices();
				});
			}
		}

		/// <summary>
		/// Update UI on device enumeration completed.
		/// </summary>
		/// <param name="sender">The active DeviceWatcher instance</param>
		/// <param name="args">Event arguments</param>
		private async void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
		{
			this.enumerationCompleted = true;
			await coreDispatcher.RunAsync(CoreDispatcherPriority.High, () =>
			{
				// Update the device list
				UpdateDevices();
			});
		}
	}
}
