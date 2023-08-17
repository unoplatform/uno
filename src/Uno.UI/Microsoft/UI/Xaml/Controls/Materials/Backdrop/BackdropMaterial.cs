// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference BackdropMaterial.cpp, commit 82feccf

using System;
using System.Threading;
using Uno.Disposables;
using Uno.UI.Helpers.WinUI;
using Windows.UI;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls
{
	/// <summary>
	/// Helper class to apply a backdrop material to the root of the XAML content.
	/// </summary>
	public partial class BackdropMaterial
	{
		private readonly static ThreadLocal<int> _connectedBrushCount = new ThreadLocal<int>();
		private readonly static ThreadLocal<MicaController> _micaController = new ThreadLocal<MicaController>();

		internal static DependencyProperty StateProperty { get; } =
			DependencyProperty.RegisterAttached("State", typeof(BackdropMaterialState), typeof(BackdropMaterial), new FrameworkPropertyMetadata(null));

		private static void OnApplyToRootOrPageBackgroundChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			if (sender is Control control)
			{
				// When the ApplyToRootOrPageBackgroundChanged property is set on a control, create a
				// object to attach to that element in a "secret" slot called BackdropMaterial.State.
				// This object's lifetime manages the MicaController registration or ownership of the
				// assignment of the Background property.
				if (GetApplyToRootOrPageBackground(control))
				{
					control.SetValue(StateProperty, new BackdropMaterialState(control));
				}
				else
				{
					if (control.GetValue(StateProperty) is BackdropMaterialState state)
					{
						state.Dispose();
					}

					control.ClearValue(StateProperty);
				}
			}
		}

		private static void CreateOrDestroyMicaController()
		{
			// If we are connecting the first BackdropMaterial on this thread, create and configure the MicaController.
			// Or if we're disconnecting the last one, clean up the shared MicaController.
			if (_connectedBrushCount.Value > 0 && _micaController.Value == null)
			{
				var currentWindow = Window.IShouldntUseCurrentWindow;
				_micaController.Value = new MicaController();
				if (!_micaController.Value.SetTarget(currentWindow))
				{
					_micaController.Value = null;
				}
			}
			else if (_connectedBrushCount.Value == 0 && _micaController.Value != null)
			{
				_micaController.Value = null;
			}
		}

		/// <summary>
		/// This object gets attached to the target of the ApplyToRootOrPageBackground property to track additional
		/// state that needs to be cleaned up if that target ever goes away.
		/// </summary>
		private partial class BackdropMaterialState : DependencyObject, IDisposable
		{
			private readonly DispatcherHelper _dispatcherHelper;
			private readonly WeakReference<Control> _target;
			private readonly IDisposable _themeChangedRevoker;
			private readonly IDisposable _colorValuesChangedRevoker;
			private readonly UISettings _uiSettings = new UISettings();
			private readonly IDisposable _highContrastChangedRevoker;

			private bool _isHighContrast;
			private bool _isDisposed;

			public BackdropMaterialState(Control target)
			{
				_dispatcherHelper = new DispatcherHelper(this);
				_target = new WeakReference<Control>(target);

				// Track whether we're connected and update the number of connected BackdropMaterial on this thread.
				_connectedBrushCount.Value++;
				CreateOrDestroyMicaController();

				// Normally QI would be fine, but .NET is lying about implementing this interface (e.g. C# TestFrame derives from Frame and this QI
				// returns success even on RS2, but it's not implemented by XAML until RS3).
				if (SharedHelpers.IsRS3OrHigher())
				{
					if (target is FrameworkElement targetThemeChanged)
					{
						void OnActualThemeChanged(FrameworkElement sender, object args)
						{
							UpdateFallbackBrush();
						}

						targetThemeChanged.ActualThemeChanged += OnActualThemeChanged;
						_themeChangedRevoker = Disposable.Create(() => targetThemeChanged.ActualThemeChanged -= OnActualThemeChanged);
					}
				}

				void OnColorValuesChanged(UISettings uiSettings, object args)
				{
					_dispatcherHelper.RunAsync(() => UpdateFallbackBrush());
				}

				_uiSettings.ColorValuesChanged += OnColorValuesChanged;
				_colorValuesChangedRevoker = Disposable.Create(() => _uiSettings.ColorValuesChanged -= OnColorValuesChanged);

				// Listen for High Contrast changes
				var accessibilitySettings = new AccessibilitySettings();
				_isHighContrast = accessibilitySettings.HighContrast;

				void OnHighContrastChanged(AccessibilitySettings sender, object args)
				{
					_dispatcherHelper.RunAsync(() =>
					{
						_isHighContrast = accessibilitySettings.HighContrast;
						UpdateFallbackBrush();
					});
				}

				accessibilitySettings.HighContrastChanged += OnHighContrastChanged;
				_highContrastChangedRevoker = Disposable.Create(() => accessibilitySettings.HighContrastChanged -= OnHighContrastChanged);

				UpdateFallbackBrush();
			}

			public BackdropMaterialState() => Dispose();

			public void Dispose()
			{
				if (!_isDisposed)
				{
					_isDisposed = true;
					_connectedBrushCount.Value--;
					CreateOrDestroyMicaController();

					_highContrastChangedRevoker.Dispose();
					_themeChangedRevoker.Dispose();
					_colorValuesChangedRevoker.Dispose();
				}
			}

			private void UpdateFallbackBrush()
			{
				if (_target.TryGetTarget(out var target))
				{
					if (_micaController.Value == null)
					{
						// When not using mica, use the theme and high contrast states to determine the fallback color.
						ElementTheme GetTheme()
						{
							// See other IsRS3OrHigher usage for comment explaining why the version check and QI.
							if (SharedHelpers.IsRS3OrHigher())
							{
								if (target is FrameworkElement targetTheme)
								{
									return targetTheme.ActualTheme;
								}
							}

							var value = _uiSettings.GetColorValue(UIColorType.Background);
							if (value.B == 0)
							{
								return ElementTheme.Dark;
							}

							return ElementTheme.Light;
						}
						var theme = GetTheme();

						Color GetColor()
						{
							if (_isHighContrast)
							{
								return _uiSettings.GetColorValue(UIColorType.Background);
							}

							if (theme == ElementTheme.Dark)
							{
								return MicaController.DarkThemeColor;
							}
							else
							{
								return MicaController.LightThemeColor;
							}
						}
						var color = GetColor();

						target.Background = new SolidColorBrush(color);
					}
					else
					{
						// When Mica is involved, use transparent for the background (this is so that the hit testing
						// behavior is consistent with/without the material).
						target.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
					}
				}
			}
		}
	}
}
