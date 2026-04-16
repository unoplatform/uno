using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.DataBinding;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Core;
using Windows.Foundation;

namespace Microsoft.UI.Xaml;

// Theming partial – element-level theme propagation, foreground freezing,
// and ActualTheme bookkeeping.  Mirrors WinUI's CDependencyObject/CFrameworkElement
// theme walk (Theming.cpp, framework.cpp).
public partial class FrameworkElement
{
	// MUX Reference: corep.h TextFormatting::m_pForeground / m_freezeForeground
	// In WinUI, every FrameworkElement has a TextFormatting structure that stores
	// an inherited foreground brush and a freeze flag. The freeze flag prevents
	// parent foreground inheritance at theme boundaries (RequestedTheme != Default).
	// Children pull the parent's frozen foreground lazily via EnsureTextFormatting.
	//
	// In Uno, we emulate this with two fields on FrameworkElement:
	// - _themeForeground: the frozen brush from the nearest themed ancestor
	// - _isForegroundFrozen: whether this element's foreground is frozen (theme boundary)
	private Brush _themeForeground;
	private bool _isForegroundFrozen;

	/// <summary>
	/// Returns true when this element is a theme boundary that should block automatic
	/// DP inheritance cascade for Foreground-type properties.
	/// </summary>
	/// <remarks>
	/// In WinUI, Foreground is not an inherited DP — it propagates through the
	/// TextFormatting system which has a freeze flag at theme boundaries.
	/// In Uno, Foreground IS inherited (FrameworkPropertyMetadataOptions.Inherits),
	/// so a parent setting Foreground auto-cascades to ALL descendants, bypassing
	/// the theme walk's early-return at elements with explicit RequestedTheme.
	/// This property lets DependencyObjectStore block that cascade at theme boundaries.
	/// </remarks>
	internal bool IsForegroundInheritanceBlocked => _isForegroundFrozen;

	#region Requested theme dependency property

	public ElementTheme RequestedTheme
	{
		get => (ElementTheme)GetValue(RequestedThemeProperty);
		set => SetValue(RequestedThemeProperty, value);
	}

	public static DependencyProperty RequestedThemeProperty { get; } =
		DependencyProperty.Register(
			nameof(RequestedTheme),
			typeof(ElementTheme),
			typeof(FrameworkElement),
			new FrameworkPropertyMetadata(
				ElementTheme.Default,
				(o, e) => ((FrameworkElement)o).OnRequestedThemeChanged((ElementTheme)e.OldValue, (ElementTheme)e.NewValue)));

	// MUX Reference framework.cpp, lines 3501-3559
	private void OnRequestedThemeChanged(ElementTheme oldValue, ElementTheme newValue)
	{
#if UNO_HAS_ENHANCED_LIFECYCLE
		var theme = Theme.None;

		switch (newValue)
		{
			case ElementTheme.Dark:
				theme = Theme.Dark;
				break;
			case ElementTheme.Light:
				theme = Theme.Light;
				break;
			case ElementTheme.Default:
				// Use parent's requested theme if this property was cleared
				var parent = this.GetParent() as UIElement;
				if (parent != null)
				{
					theme = parent.GetTheme();
				}
				// If there is no parent, or parent's theme has not been set,
				// default to the application's requested theme.
				if (theme == Theme.None)
				{
					theme = Theming.FromElementTheme(
						Application.Current?.ActualElementTheme ?? ElementTheme.Light);
				}
				// Unfreeze inherited properties at this element's level
				NotifyThemeChangedForInheritedProperties(theme, freeze: false);
				break;
		}

		// Apply theme to this element and its subtree
		NotifyThemeChanged(theme);
#else
		SyncRootRequestedTheme();
		if (ActualThemeChanged != null)
		{
			var actualThemeChanged =
				(oldValue == ElementTheme.Default && Application.Current?.ActualElementTheme != newValue) ||
				(oldValue != ElementTheme.Default && oldValue != ActualTheme);
			if (actualThemeChanged)
			{
				ActualThemeChanged?.Invoke(this, null);
			}
		}
#endif
	}

	#endregion

#if !UNO_HAS_ENHANCED_LIFECYCLE
	private void SyncRootRequestedTheme()
	{
		if (XamlRoot?.Content == this)
		{
			Application.Current.SyncRequestedThemeFromXamlRoot(XamlRoot);
		}
	}
#endif

	#region Theme propagation

	// MUX Reference framework.cpp, lines 3969-3994
	/// <summary>
	/// Gets the UI theme that is currently used by the element.
	/// </summary>
	/// <remarks>
	/// This is always either Dark or Light, never Default.
	/// </remarks>
	public ElementTheme ActualTheme
	{
		get
		{
#if UNO_HAS_ENHANCED_LIFECYCLE
			// Get base (non-HighContrast) theme.
			// Fall back to default (system or app) theme if the local theme isn't set.
			var baseTheme = Theming.GetBaseValue(GetTheme());
			if (baseTheme == Theme.None)
			{
				baseTheme = Theming.FromElementTheme(
					Application.Current?.ActualElementTheme ?? ElementTheme.Light);
			}
			return Theming.ToElementTheme(baseTheme);
#else
			return RequestedTheme == ElementTheme.Default
				? (Application.Current?.ActualElementTheme ?? ElementTheme.Light)
				: RequestedTheme;
#endif
		}
	}

	/// <summary>
	/// Occurs when the ActualTheme property value has changed.
	/// </summary>
	public event TypedEventHandler<FrameworkElement, object> ActualThemeChanged;

	// MUX Reference framework.cpp, lines 3313-3333
	/// <summary>
	/// Notifies this element and its subtree that the theme has changed.
	/// </summary>
	internal void NotifyThemeChanged(Theme theme, bool forceRefresh = false)
	{
#if !UNO_HAS_ENHANCED_LIFECYCLE
		return;
#else
		// Cycle protection - prevent re-entrant theme notifications
		if (IsProcessingThemeWalk)
		{
			return;
		}

		// MUX Reference: Theming.cpp line 128, framework.cpp lines 3269-3288
		// Override incoming theme with element's own RequestedTheme if set.
		// This matches WinUI's GetRequestedThemeOverride pattern where each
		// element with explicit RequestedTheme overrides the incoming theme
		// rather than being skipped during propagation.
		if (RequestedTheme != ElementTheme.Default)
		{
			theme = Theming.FromElementTheme(RequestedTheme);
		}

		// MUX Reference: Theming.cpp line 132
		// Early-out if theme hasn't changed and not forcing refresh
		if (theme == GetTheme() && !forceRefresh)
		{
			return;
		}

		SetIsProcessingThemeWalk(true);
		// MUX Reference: CCoreServices::NotifyThemeChange() calls
		// BeginCachingThemeResources() before the theme walk.
		// Reentrant-safe: nested calls (recursive child walks) get no-op guards.
		using var cacheGuard = ThemeWalkResourceCache.Instance.BeginCachingThemeResources();
		try
		{
			NotifyThemeChangedCore(theme, forceRefresh);
		}
		finally
		{
			SetIsProcessingThemeWalk(false);
		}
#endif
	}

	// MUX Reference Theming.cpp CDependencyObject::NotifyThemeChanged (line 110)
	// WinUI uses a push-process-pop pattern where each element temporarily
	// "owns" the global theme context during its processing.
	/// <summary>
	/// Core implementation of theme change notification using WinUI's push-pop pattern.
	/// </summary>
	private protected virtual void NotifyThemeChangedCore(Theme theme, bool forceRefresh)
	{
		// 1. Determine if ActualTheme is changing
		var oldTheme = GetTheme();
		var appBaseTheme = Theming.FromElementTheme(
			Application.Current?.ActualElementTheme ?? ElementTheme.Light);
		var oldBase = oldTheme == Theme.None ? appBaseTheme : Theming.GetBaseValue(oldTheme);
		var newBase = Theming.GetBaseValue(theme);

		bool themeChanged = oldBase != newBase || forceRefresh;

		// 2. PUSH this element's theme to global context
		// The push/pop is still needed because ResourceDictionary.GetActiveThemeDictionary()
		// uses the global theme stack to determine which theme sub-dictionary to return.
		// Even though ThemeResourceReference pins the target dictionary, the dictionary's
		// TryGetValue still needs to know the active theme to select Light/Dark sub-dict.
		var currentActiveTheme = ResourceDictionary.GetActiveTheme();
		string themeKey;
		bool needsPush;
		if (newBase == Theme.None)
		{
			themeKey = currentActiveTheme.Key;
			needsPush = false;
		}
		else
		{
			themeKey = newBase == Theme.Light ? "Light" : "Dark";
			needsPush = !themeKey.Equals(currentActiveTheme.Key);
		}

		if (needsPush)
		{
			ResourceDictionary.PushRequestedThemeForSubTree(themeKey);
		}

		try
		{
			// 3. FREEZE inherited properties first (WinUI: framework.cpp line 3301-3307)
			if (RequestedTheme != ElementTheme.Default)
			{
				NotifyThemeChangedForInheritedProperties(theme, freeze: true);
			}
			else if (themeChanged)
			{
				var parent = this.GetParent() as FrameworkElement;
				var parentFg = parent?._themeForeground;
				if (parentFg is not null)
				{
					EnsureThemeForeground(parentFg);
				}
				else if (_themeForeground is not null)
				{
					_themeForeground = null;
					_isForegroundFrozen = false;

					var fgProp = GetForegroundProperty();
					if (fgProp is not null)
					{
						DependencyObjectExtensions.SetValue(
							this, fgProp, DependencyProperty.UnsetValue,
							DependencyPropertyValuePrecedences.Inheritance);
					}
				}
			}

			// 4. Update theme resources via pinned-dictionary path
			if (themeChanged)
			{
				UpdateThemeBindings(ResourceUpdateReason.ThemeResource);
			}

			// 5. Propagate to children (they may push their own context)
			PropagateThemeToChildren(theme, forceRefresh);

			// 6. Raise event LAST (WinUI: framework.cpp line 3317)
			if (themeChanged)
			{
				RaiseActualThemeChanged();
			}

			// 7. Persist theme AFTER core walk (MUX Reference: Theming.cpp line 155)
			SetTheme(theme);
		}
		finally
		{
			// 8. POP the global context
			if (needsPush)
			{
				ResourceDictionary.PopRequestedThemeForSubTree();
			}
		}
	}

	// MUX Reference uielement.cpp, lines 14469-14495
	/// <summary>
	/// Propagates theme changes to child elements.
	/// </summary>
	private void PropagateThemeToChildren(Theme theme, bool forceRefresh)
	{
		var childCount = VisualTreeHelper.GetChildrenCount(this);
		for (var i = 0; i < childCount; i++)
		{
			var child = VisualTreeHelper.GetChild(this, i);
			if (child is FrameworkElement fe)
			{
				// All children participate in the theme walk.
				// GetRequestedThemeOverride in NotifyThemeChanged will override
				// the theme for children with explicit RequestedTheme.
				fe.NotifyThemeChanged(theme, forceRefresh);
			}
		}
	}

	// MUX Reference framework.cpp, lines 3346-3386
	/// <summary>
	/// Raises the ActualThemeChanged event.
	/// </summary>
	private void RaiseActualThemeChanged()
	{
		try
		{
			ActualThemeChanged?.Invoke(this, null);
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("ActualThemeChanged handler threw an exception", e);
			}
		}
	}

	// MUX Reference: CFrameworkElement::PullInheritedTextFormatting / EnsureTextFormatting
	// In WinUI, when a child's TextFormatting is out of date (generation counter mismatch),
	// it pulls the parent's foreground unless freezeForeground is set.
	// In Uno, we apply the parent's frozen foreground to this element's Foreground DP.
	/// <summary>
	/// Applies the inherited theme foreground from the nearest themed ancestor.
	/// Mirrors WinUI's PullInheritedTextFormatting for the Foreground property.
	/// </summary>
	/// <param name="parentThemeForeground">The foreground brush from the parent's theme context.</param>
	private void EnsureThemeForeground(Brush parentThemeForeground)
	{
		if (parentThemeForeground is null)
		{
			return;
		}

		// MUX Reference: CControl::PullInheritedTextFormatting (Control.cpp line 134)
		//   if (IsPropertyDefaultByIndex(KnownPropertyIndex::Control_Foreground)
		//       && !m_pTextFormatting->m_freezeForeground)
		// MUX Reference: CTextBlock::PullInheritedTextFormatting (TextBlock.cpp line 647)
		//   if (IsPropertyDefaultByIndex(KnownPropertyIndex::TextBlock_Foreground)
		//       && !m_pTextFormatting->m_freezeForeground)
		DependencyProperty foregroundProperty = GetForegroundProperty();

		if (foregroundProperty is null)
		{
			// Elements without Foreground (e.g., Panel, Border) just store
			// the inherited brush so their children can pull it.
			_themeForeground = parentThemeForeground;
			return;
		}

		// Only inherit if Foreground is at default and this element's foreground is not frozen.
		// Matches WinUI: IsPropertyDefaultByIndex(Foreground) && !m_freezeForeground
		if (_isForegroundFrozen)
		{
			return;
		}

		var precedence = this.GetCurrentHighestValuePrecedence(foregroundProperty);
		if (precedence != DependencyPropertyValuePrecedences.DefaultValue
			&& precedence != DependencyPropertyValuePrecedences.Inheritance)
		{
			return;
		}

		// Store the inherited foreground and apply to the DP at Inheritance precedence.
		// Using Inheritance precedence ensures Local/Style values take priority.
		_themeForeground = parentThemeForeground;
		this.SetValue(foregroundProperty, parentThemeForeground, DependencyPropertyValuePrecedences.Inheritance);
	}

	// MUX Reference framework.cpp, lines 3401-3492
	/// <summary>
	/// Handles inherited property behavior at theme boundaries.
	/// When freeze is true, resolves and freezes the theme's default foreground brush
	/// so children inherit it instead of the foreground from ancestors outside this subtree.
	/// When freeze is false, clears the freeze to allow normal inheritance.
	/// </summary>
	/// <remarks>
	/// Mirrors WinUI's TextFormatting freeze mechanism:
	/// - SetFreezeForeground(true): store brush in TextFormatting, block parent inheritance
	/// - SetFreezeForeground(false): unblock, children re-inherit from parent
	/// - MarkInheritedPropertyDirty: bumps generation counter so children re-pull
	/// In Uno, we store the brush in _themeForeground and the flag in _isForegroundFrozen,
	/// then propagate to children during the theme walk (PropagateThemeToChildren).
	/// </remarks>
	internal void NotifyThemeChangedForInheritedProperties(Theme theme, bool freeze)
	{
		// Trigger re-evaluation of theme resources for this element
		if (this is IThemeChangeAware themeAware)
		{
			themeAware.OnThemeChanged();
		}

		if (freeze)
		{
			DependencyProperty foregroundProperty = GetForegroundProperty();

			// MUX Reference framework.cpp line 3423-3429:
			// WinUI skips the entire freeze when Foreground is set locally or by style,
			// relying on PullInheritedTextFormatting to propagate the styled value.
			// In Uno, Foreground is an inherited DP, so children auto-cascade from
			// the parent. We still need _themeForeground for children that don't
			// inherit via DP (e.g., popup/template content), but we skip the SetValue
			// to avoid overriding the styled value on THIS element.
			bool skipSetValue = false;
			if (foregroundProperty is not null)
			{
				var precedence = this.GetCurrentHighestValuePrecedence(foregroundProperty);
				if (precedence != DependencyPropertyValuePrecedences.DefaultValue
					&& precedence != DependencyPropertyValuePrecedences.Inheritance)
				{
					skipSetValue = true;
				}
			}

			// Resolve the theme's default text foreground brush
			var themeKey = Theming.GetBaseValue(theme) == Theme.Light ? "Light" : "Dark";
			ResourceDictionary.PushRequestedThemeForSubTree(themeKey);
			try
			{
				var brush = (Brush)ResourceResolver.ResolveTopLevelResource(
					"DefaultTextForegroundThemeBrush", null);
				if (brush is not null)
				{
					// Always store for child inheritance (popup content, template children)
					_themeForeground = brush;
					_isForegroundFrozen = true;

					// Only set the DP when Foreground isn't already set by a higher
					// precedence (local/style). This matches WinUI's skip behavior
					// while preserving _themeForeground for child propagation.
					if (foregroundProperty is not null && !skipSetValue)
					{
						this.SetValue(
							foregroundProperty, brush,
							DependencyPropertyValuePrecedences.Inheritance);
					}
				}
			}
			finally
			{
				ResourceDictionary.PopRequestedThemeForSubTree();
			}
		}
		else
		{
			// MUX Reference framework.cpp line 3453-3465:
			// SetFreezeForeground(false), m_cInheritedPropGenerationCounter++
			_isForegroundFrozen = false;
			_themeForeground = null;

			// Clear the value we set at Inheritance precedence
			DependencyProperty foregroundProperty = GetForegroundProperty();
			if (foregroundProperty is not null)
			{
				DependencyObjectExtensions.SetValue(
					this, foregroundProperty, DependencyProperty.UnsetValue,
					DependencyPropertyValuePrecedences.Inheritance);
			}
		}
	}

	/// <summary>
	/// Gets the Foreground DependencyProperty for this element, if it has one.
	/// </summary>
	private DependencyProperty GetForegroundProperty()
	{
		if (this is Controls.Control)
		{
			return Controls.Control.ForegroundProperty;
		}
		else if (this is Controls.TextBlock)
		{
			return Controls.TextBlock.ForegroundProperty;
		}
		else if (this is Controls.IconElement)
		{
			return Controls.IconElement.ForegroundProperty;
		}
		else if (this is Controls.ContentPresenter)
		{
			return Controls.ContentPresenter.ForegroundProperty;
		}
		else if (this is Controls.RichTextBlock)
		{
			return Controls.RichTextBlock.ForegroundProperty;
		}
		return null;
	}

#if UNO_HAS_ENHANCED_LIFECYCLE
	/// <summary>
	/// Clears theme foreground state when unloading from the visual tree.
	/// Called from <see cref="OnUnloadedPartial"/> to prevent stale inherited
	/// values from leaking across runtime tests or re-parenting scenarios.
	/// </summary>
	private void ClearThemeStateOnUnloaded()
	{
		// Clear inherited theme foreground when leaving the tree to prevent
		// stale values from leaking to elements loaded later in the same
		// parent (e.g., between runtime tests). Elements with explicit
		// RequestedTheme (_isForegroundFrozen) retain their brush because
		// they own the theme boundary.
		if (!_isForegroundFrozen)
		{
			_themeForeground = null;
		}

		// Clear stored theme so that when this element re-enters the tree
		// (possibly under a different theme region), OnLoadingPartial will
		// correctly inherit the new parent's theme. Elements with explicit
		// RequestedTheme don't need this because OnLoadingPartial calls
		// NotifyThemeChanged for them.
		if (RequestedTheme == ElementTheme.Default)
		{
			SetTheme(Theme.None);
		}
	}
#endif

	/// <summary>
	/// Update ThemeResource references.
	/// </summary>
	internal virtual void UpdateThemeBindings(ResourceUpdateReason updateReason)
	{
#if UNO_HAS_ENHANCED_LIFECYCLE
		// Pass element's ActualTheme to resource binding updates
		TryGetResources()?.UpdateThemeBindings(updateReason);
		(this as IDependencyObjectStoreProvider).Store.UpdateResourceBindings(
			updateReason,
			resourceContextProvider: this);

		// Don't fire ActualThemeChanged here anymore - it's now fired
		// in NotifyThemeChangedCore when the theme actually changes
#else
		TryGetResources()?.UpdateThemeBindings(updateReason);
		(this as IDependencyObjectStoreProvider).Store.UpdateResourceBindings(updateReason);
		if (updateReason == ResourceUpdateReason.ThemeResource)
		{
			if (ActualThemeChanged != null && RequestedTheme == ElementTheme.Default)
			{
				try
				{
					ActualThemeChanged?.Invoke(this, null);
				}
				catch (Exception e)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error("ActualThemeChanged handler threw an exception", e);
					}
				}
			}
		}
#endif
	}

	#endregion
}
