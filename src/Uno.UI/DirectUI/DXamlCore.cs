﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// DXamlCore.h, DXamlCore.cpp

#nullable enable

using System;
using System.Collections.Generic;
using Uno.UI.Helpers.WinUI;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace DirectUI
{
	internal class DXamlCore
	{
		private static readonly Lazy<DXamlCore> _current = new Lazy<DXamlCore>(() => new DXamlCore());

		private Dictionary<string, List<WeakReference<RadioButton>>>? _radioButtonGroupsByName;
		private ContentDialogMetadata? _contentDialogMetadata;
		
		private BuildTreeService? _buildTreeService;
		private BudgetManager? _budgetManager;

		// UNO: This should **NOT** create the singleton!
		//		_but_ if we do return a 'null' the 'OnApplyTemplate' of the `CalendarView` will fail.
		//		As for now our implementation of the 'DXamlCore' is pretty light and stored as a basic singleton,
		//		we accept to create it even with the "NoCreate" overload.
		public static DXamlCore Current => _current.Value;

		public static DXamlCore GetCurrentNoCreate() => Current;

		public Window Window => Window.Current; // TODO: Uno specific: This is a very simple solution for now until we have multi-window support.

		public Uno.UI.Xaml.Core.CoreServices GetHandle() => Uno.UI.Xaml.Core.CoreServices.Instance;

		public Rect DipsToPhysicalPixels(float scale, Rect dipRect)
		{
			var physicalRect = dipRect;
			physicalRect.X = dipRect.X * scale;
			physicalRect.Y = dipRect.Y * scale;
			physicalRect.Width = dipRect.Width * scale;
			physicalRect.Height = dipRect.Height * scale;
			return physicalRect;
		}

		// TODO Uno: Application-wide bar is not supported yet.
		public ApplicationBarService? TryGetApplicationBarService() => null;

		public string GetLocalizedResourceString(string key)
			=> ResourceAccessor.GetLocalizedStringResource(key);

		public BuildTreeService GetBuildTreeService()
			=> _buildTreeService ??= new BuildTreeService();

		public BudgetManager GetBudgetManager()
			=> _budgetManager ??= new BudgetManager();

		public ElementSoundPlayerService GetElementSoundPlayerServiceNoRef()
			=> ElementSoundPlayerService.Instance;

		internal Dictionary<string, List<WeakReference<RadioButton>>>? GetRadioButtonGroupsByName(bool ensure)
		{
			if (_radioButtonGroupsByName == null && ensure)
			{
				_radioButtonGroupsByName = new Dictionary<string, List<WeakReference<RadioButton>>>();
			}

			return _radioButtonGroupsByName;
		}

		internal ContentDialogMetadata ContentDialogMetadata => _contentDialogMetadata ??= new ContentDialogMetadata();
	}
}
