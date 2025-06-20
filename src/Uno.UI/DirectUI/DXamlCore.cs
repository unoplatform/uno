// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// DXamlCore.h, DXamlCore.cpp

#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Helpers.WinUI;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Core.Scaling;
using Uno.UI.Xaml.Islands;
using Windows.Foundation;
using static Microsoft.UI.Xaml.Controls._Tracing;

namespace DirectUI
{
	internal class DXamlCore
	{
		private static readonly Lazy<DXamlCore> _current = new Lazy<DXamlCore>(() => new DXamlCore());

		private Dictionary<string, List<WeakReference<RadioButton>>>? _radioButtonGroupsByName;

		private KeyboardCapabilities? _keyboardCapabilities;
		private BuildTreeService? _buildTreeService;
		private BudgetManager? _budgetManager;

		private Window? m_uwpWindowNoRef => Window.IsCurrentSet ? Window.CurrentSafe : null; // TODO Uno: This is different implementation, but UWP is not going to be supported.

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

		internal bool IsKeyboardPresent
		{
			get
			{
				_keyboardCapabilities ??= new KeyboardCapabilities();
				return _keyboardCapabilities.KeyboardPresent != 0;
			}
		}

		internal Rect GetContentBoundsForElement(DependencyObject dependencyObject)
		{
			if (dependencyObject is not null && TryGetXamlIslandBoundsForElement(dependencyObject, out var bounds))
			{
				return bounds;
			}

			if (m_uwpWindowNoRef is not null) //&& m_uwpWindowNoRef.HasBounds())
			{
				return m_uwpWindowNoRef.Bounds;
			}
			else
			{
				// Except Islands/ Win32, HasBounds should always be called before retrieving bounds. In win32/ Islands, it's okay to have no bounds
				MUX_ASSERT(this.GetHandle().InitializationType == InitializationType.IslandsOnly);
				return new();
			}
		}

		// Returns the XamlIslandRoot bounds if Xaml is in XamlIslandRoot host mode. Some elements such as connected animations
		// will always have a non-XamlIslandRoot root and will never have XamlIslandRoot bounds.
		private bool TryGetXamlIslandBoundsForElement(DependencyObject? dependencyObject, out Rect bounds)
		{
			bounds = default;
			XamlIsland? xamlIslandRoot = VisualTree.GetXamlIslandRootForElement(dependencyObject);
			if (xamlIslandRoot is not null)
			{
				var size = xamlIslandRoot.GetSize();
				bounds = new(0, 0, size.Width, size.Height);
				return true;
			}

			return false;
		}
	}
}
