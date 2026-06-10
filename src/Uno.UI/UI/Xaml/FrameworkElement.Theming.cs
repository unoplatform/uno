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

	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Get theme to set for this element during Theme Walk
	//------------------------------------------------------------------------
	// MUX Reference: CFrameworkElement::GetRequestedThemeOverride — framework.cpp:3284-3304
	internal Theme GetRequestedThemeOverride(Theme theme)
	{
		var highContrastTheme = theme & Theme.HighContrastMask;

		// If RequestedTheme is set, it takes precedence.
		var requestedTheme = RequestedTheme;
		if (requestedTheme != ElementTheme.Default)
		{
			if (requestedTheme == ElementTheme.Dark)
			{
				theme = Theme.Dark | highContrastTheme;
			}
			else if (requestedTheme == ElementTheme.Light)
			{
				theme = Theme.Light | highContrastTheme;
			}
		}

		return theme;
	}

#if UNO_HAS_ENHANCED_LIFECYCLE
	//------------------------------------------------------------------------
	//
	//  Synopsis:
	//      Notify element that theme has changed
	//
	//------------------------------------------------------------------------
	// MUX Reference: CFrameworkElement::NotifyThemeChangedCore — framework.cpp:3313-3333
	internal override void NotifyThemeChangedCore(Theme theme, bool forceRefresh)
	{
		// If RequestedTheme is set, freeze of theme related inherited
		// properties at the root of the subtree, so they don't inherit from this
		// element's parent, which may have a different theme
		if (RequestedTheme != ElementTheme.Default)
		{
			NotifyThemeChangedForInheritedProperties(theme, freeze: true);
		}
		else if (IsEffectiveThemeChanging(theme, forceRefresh))
		{
			// Uno-specific: Foreground is an inherited DP in Uno (WinUI propagates it through the
			// TextFormatting pull system, see EnsureThemeForeground), so the inherited theme
			// foreground is re-pulled from the parent or cleared here when the effective theme
			// changes.
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

		// Notify base class that theme has changed
		base.NotifyThemeChangedCore(theme, forceRefresh);

		// Raise event if the effective theme is changing.
		RaiseActiveThemeChangedEventIfChanging(theme, forceRefresh);
	}

	// Transitional shape of CFrameworkElement::RaiseActiveThemeChangedEventIfChanging
	// (framework.cpp:3346-3386): the base-theme comparison ports 1:1 using the stashed pre-walk
	// theme (see DependencyObjectStore.PreviousThemeForWalk); the HighContrastChanged event and the
	// HC suppression rules land with the Phase 4 framework.cpp port.
	private void RaiseActiveThemeChangedEventIfChanging(Theme theme, bool forceRefresh)
	{
		if (IsEffectiveThemeChanging(theme, forceRefresh))
		{
			RaiseActualThemeChanged();
		}
	}

	// MUX: framework.cpp:3378-3382 — "Raise ActualThemeChanged event if effective base
	// (non-HighContrast) theme value is changing": oldTheme == None ? GetBaseTheme() :
	// GetBaseValue(oldTheme), compared to the incoming base. The pre-walk theme comes from
	// DependencyObjectStore.PreviousThemeForWalk (Uno persists before the core walk — sync event
	// delivery); Application.IsBaseThemeChanging mirrors WinUI's IsBaseThemeChanging() so a
	// never-walked element still raises on an app/system switch.
	// Uno-specific: forceRefresh also counts as changing (pre-existing behavior, re-validated in
	// Phase 4 of the theming alignment).
	private bool IsEffectiveThemeChanging(Theme theme, bool forceRefresh)
	{
		var oldTheme = ((IDependencyObjectStoreProvider)this).Store.PreviousThemeForWalk;
		var appBaseTheme = Theming.FromElementTheme(Application.Current?.ActualElementTheme ?? ElementTheme.Light);
		var oldBaseForEvent = oldTheme == Theme.None ? appBaseTheme : Theming.GetBaseValue(oldTheme);
		return oldBaseForEvent != Theming.GetBaseValue(theme)
			|| (oldTheme == Theme.None && Application.IsBaseThemeChanging)
			|| forceRefresh;
	}
#endif

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

			// Resolve the theme's default text foreground brush against the element's own theme. MUX:
			// CFrameworkElement::NotifyThemeChangedForInheritedProperties resolves the default text
			// foreground from the element's theme (framework.cpp:3401-3492).
			var themeKey = ResourceDictionary.GetThemeKey(theme);

#if UNO_HAS_ENHANCED_LIFECYCLE
			// MUX Reference: framework.cpp:3441-3487 — the inherited-property foreground lookup runs
			// with the element's theme scoped onto the core requested-theme-for-subtree slot (the same
			// save/set/restore pattern as CCoreServices::LookupThemeResource, xcpcore.cpp:2371-2394).
			// Transitional: the theme key is still ALSO passed explicitly below; retired in Phase 5.
			var core = Uno.UI.Xaml.Core.CoreServices.Instance;
			var prevSlotTheme = core.GetRequestedThemeForSubTree();
			var popSlotTheme = false;
			if (prevSlotTheme != Theming.GetBaseValue(theme))
			{
				core.SetRequestedThemeForSubTree(theme);
				popSlotTheme = true;
			}
			try
			{
#endif
			var brush = (Brush)ResourceResolver.ResolveTopLevelResource(
				"DefaultTextForegroundThemeBrush", themeKey, null);
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
#if UNO_HAS_ENHANCED_LIFECYCLE
			}
			finally
			{
				// Scope-restore the slot (framework.cpp:3487).
				if (popSlotTheme)
				{
					core.SetRequestedThemeForSubTree(prevSlotTheme);
				}
			}
#endif
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
	/// Clears inherited theme-foreground state when unloading from the visual tree.
	/// Called from <see cref="OnUnloadedPartial"/> to prevent a stale frozen foreground
	/// brush from leaking to elements loaded later under the same parent.
	/// </summary>
	/// <remarks>
	/// MUX Reference: CDependencyObject keeps m_theme across Leave; it is re-established (or overridden
	/// by a different ancestor theme) when the element re-enters the tree via the EnterImpl theme block
	/// (depends.cpp:1044-1069), ported to DependencyObjectStore.EnterImpl (DependencyObjectStore.mux.cs). So
	/// this method clears only the inherited foreground brush and intentionally does <b>not</b> reset _theme.
	/// </remarks>
	private void ClearThemeStateOnUnloaded()
	{
		// Clear the inherited theme foreground to prevent stale values from leaking to elements loaded
		// later under the same parent (e.g., between runtime tests). Elements with explicit RequestedTheme
		// (_isForegroundFrozen) keep their brush; on re-Enter the correct foreground is re-pulled.
		if (!_isForegroundFrozen)
		{
			_themeForeground = null;
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
