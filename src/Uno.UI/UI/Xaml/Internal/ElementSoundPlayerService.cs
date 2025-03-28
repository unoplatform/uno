// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// ElementSoundPlayerService.cpp

#nullable enable

using System;
using Windows.UI.Xaml;
using static Microsoft/* UWP don't rename */.UI.Xaml.Controls._Tracing;

namespace Uno.UI.Xaml.Core
{
	internal class ElementSoundPlayerService
	{
		private readonly static Lazy<ElementSoundPlayerService> _instance =
			new Lazy<ElementSoundPlayerService>(() => new ElementSoundPlayerService());

		private double _volume = 1.0;
		private ElementSoundPlayerState _playerState = ElementSoundPlayerState.Auto;
		private ElementSpatialAudioMode _spatialAudioMode = ElementSpatialAudioMode.Auto;

		/// <summary>
		/// Gets the default instance of ElementSoundPlayerService.
		/// </summary>
		/// <remarks>
		/// While WinUI stores instance using CoreServices, we use singleton
		/// for simplification.
		/// </remarks>
		internal static ElementSoundPlayerService Instance => _instance.Value;

		internal double Volume
		{
			get => _volume;
			set
			{
				MUX_ASSERT(value >= 0 && value <= 1.0);

				if (_volume != value)
				{
					_volume = value;

					//TODO Uno: Implement
					//if (ShouldPlaySound())
					//{
					//	IFC_RETURN(EnsureWorkerThread());
					//}
					//else
					//{
					//	IFC_RETURN(TearDownAudioGraph());
					//}
				}
			}
		}

		internal void Play(ElementSoundKind sound)
		{
			//TODO Uno: Implement
		}

		internal ElementSoundPlayerState PlayerState
		{
			get => _playerState;
			set
			{
				if (_playerState != value)
				{
					_playerState = value;

					//TODO Uno: Implement
					//if (ShouldPlaySound())
					//{
					//	IFC_RETURN(EnsureWorkerThread());
					//}
					//else
					//{
					//	IFC_RETURN(TearDownAudioGraph());
					//}
				}
			}
		}

		public ElementSpatialAudioMode SpatialAudioMode
		{
			get => _spatialAudioMode;
			set
			{
				if (_spatialAudioMode != value)
				{
					_spatialAudioMode = value;

					//TODO Uno: Implement
					//// Calculate spatial audio setting from the mode and platform
					//_isSpatialAudioEnabled = CalculateSpatialAudioSetting();

					//// Tear down the AudioGraph and clear cached AudioInputNodes
					//TearDownAudioGraph();
				}
			}
		}

		internal ElementSoundMode GetEffectiveSoundMode(DependencyObject? dependencyObject)
		{
			//TODO Uno: Implement
			return ElementSoundMode.Off;
		}

		internal void RequestInteractionSoundForElement(ElementSoundKind soundKind, DependencyObject? dependencyObject)
		{
			//TODO Uno: Implement
		}

		internal static void RequestInteractionSoundForElementStatic(ElementSoundKind soundKind, DependencyObject? dependencyObject)
		{
			//TODO Uno: Implement
		}
	}
}
