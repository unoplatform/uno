// Copyright (c) Microsoft Corporation. All rights reserved.
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
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Xaml.Core.Scaling;
using Windows.Devices.Input;

namespace DirectUI
{
	internal class DXamlCore
	{
		private static readonly Lazy<DXamlCore> _current = new Lazy<DXamlCore>(() => new DXamlCore());

		private Dictionary<string, List<WeakReference<RadioButton>>>? _radioButtonGroupsByName;

		private KeyboardCapabilities? _keyboardCapabilities;
		private BuildTreeService? _buildTreeService;
		private BudgetManager? _budgetManager;

		// UNO: This should **NOT** create the singleton!
		//		_but_ if we do return a 'null' the 'OnApplyTemplate' of the `CalendarView` will fail.
		//		As for now our implementation of the 'DXamlCore' is pretty light and stored as a basic singleton,
		//		we accept to create it even with the "NoCreate" overload.
		public static DXamlCore Current => _current.Value;

		public static DXamlCore GetCurrentNoCreate() => Current;

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

		public Microsoft.UI.Xaml.Window? GetAssociatedWindow(Microsoft.UI.Xaml.UIElement element)
		{
			if (element == null)
			{
				throw new ArgumentNullException(nameof(element));
			}

			return element.XamlRoot?.HostWindow;
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

		internal void OnCompositionContentStateChangedForUWP()
		{
			var contentRootCoordinator = Uno.UI.Xaml.Core.CoreServices.Instance.ContentRootCoordinator;
			var root = contentRootCoordinator.Unsafe_IslandsIncompatible_CoreWindowContentRoot;
			if (root is null)
			{
				// The CoreWindow is not initialized, ignore the content state change.
				return;
			}

			var rootScale = RootScale.GetRootScaleForContentRoot(root);
			if (rootScale is null) // Check that we still have an active tree
			{
				return;
			}
			rootScale.UpdateSystemScale();

			// TODO Uno: Adjusting for visibility changes on CoreWindow is not supported yet.
			root?.AddPendingXamlRootChangedEvent(default);
			root?.RaisePendingXamlRootChangedEventIfNeeded(false);

			// TODO Uno: Not needed now.
			// OnUWPWindowSizeChanged();
		}

		internal bool BackButtonSupported => false; // TODO Uno: Implement system back button detection

		internal Rect GetContentBoundsForElement(Microsoft.UI.Xaml.UIElement element)
		{
			// Returns the window/content bounds in which the element resides.
			var xamlRoot = element?.XamlRoot;
			if (xamlRoot is not null)
			{
				return new Rect(0, 0, xamlRoot.Size.Width, xamlRoot.Size.Height);
			}

			return Rect.Empty;
		}

		internal Rect GetContentLayoutBoundsForElement(Microsoft.UI.Xaml.UIElement element)
		{
			// Returns the layout bounds (excluding system chrome like status bar).
			// For now, same as content bounds since Uno handles safe areas differently.
			return GetContentBoundsForElement(element);
		}

		internal bool IsKeyboardPresent
		{
			get
			{
				_keyboardCapabilities ??= new KeyboardCapabilities();
				return _keyboardCapabilities.KeyboardPresent != 0;
			}
		}
	}
}
