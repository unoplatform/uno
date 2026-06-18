#nullable enable

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
	private Brush? _themeForeground;
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

	// MUX Reference: CFrameworkElement::OnRequestedThemeChanged — framework.cpp:3501-3559
	private void OnRequestedThemeChanged(ElementTheme oldValue, ElementTheme newValue)
	{
#if UNO_HAS_ENHANCED_LIFECYCLE
		var theme = Theme.None;
		bool setIsSwitchingTheme = false;
		var core = Uno.UI.Xaml.Core.CoreServices.Instance;

		switch (newValue)
		{
			case ElementTheme.Dark:
				theme = Theme.Dark;
				break;
			case ElementTheme.Light:
				theme = Theme.Light;
				break;
			case ElementTheme.Default:
				// Use parent's theme if cleared
				if (this.GetParent() is IDependencyObjectStoreProvider parentProvider)
				{
					theme = parentProvider.Store.GetTheme();
				}
				if (theme == Theme.None)
				{
					// MUX: framework.cpp:3520-3527 — fall back to the app/system base theme
					// (FrameworkTheming::GetBaseTheme).
					theme = core.Theming.GetBaseTheme();
				}

				// UnFreeze inherited properties
				NotifyThemeChangedForInheritedProperties(theme, freeze: false);
				break;
		}

		try
		{
			if (!core.IsSwitchingTheme)
			{
				core.IsSwitchingTheme = true;
				setIsSwitchingTheme = true;
			}

			// Apply theme + HC to subtree
			theme |= core.Theming.GetHighContrastTheme();
			NotifyThemeChanged(theme);
		}
		finally
		{
			if (setIsSwitchingTheme)
			{
				core.IsSwitchingTheme = false;
			}
		}
#else
		SyncRootRequestedTheme();
		if (_actualThemeChanged != null)
		{
			var actualThemeChanged =
				(oldValue == ElementTheme.Default && Application.Current?.ActualElementTheme != newValue) ||
				(oldValue != ElementTheme.Default && oldValue != ActualTheme);
			if (actualThemeChanged)
			{
				_actualThemeChanged?.Invoke(this, null!);
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
			// MUX: framework.cpp:3978-3989 — FrameworkTheming::GetBaseTheme() when m_theme is None.
			var baseTheme = Theming.GetBaseValue(GetTheme());
			if (baseTheme == Theme.None)
			{
				baseTheme = Uno.UI.Xaml.Core.CoreServices.Instance.Theming.GetBaseTheme();
			}
			return Theming.ToElementTheme(baseTheme);
#else
			return RequestedTheme == ElementTheme.Default
				? (Application.Current?.ActualElementTheme ?? ElementTheme.Light)
				: RequestedTheme;
#endif
		}
	}

	private TypedEventHandler<FrameworkElement, object>? _actualThemeChanged;

	/// <summary>
	/// Occurs when the ActualTheme property value has changed.
	/// </summary>
	public event TypedEventHandler<FrameworkElement, object>? ActualThemeChanged
	{
		add
		{
			if (value is null)
			{
				return;
			}

#if UNO_HAS_ENHANCED_LIFECYCLE
			// MUX Reference: CFrameworkElement::AddEventListener — framework.cpp:3996-4014:
			// "Register to receive theme change notifications so we can raise the event
			//  for default (system/app) theme changes when the element happens to be outside
			//  the live tree (i.e. not included in the theme walk from root)."
			Uno.UI.Xaml.Core.CoreServices.Instance.AddThemeChangedListener(this);
#endif
			_actualThemeChanged += value;
		}
		remove
		{
			if (value is null)
			{
				return;
			}

#if UNO_HAS_ENHANCED_LIFECYCLE
			// MUX Reference: CFrameworkElement::RemoveEventListener — framework.cpp:4016-4028
			Uno.UI.Xaml.Core.CoreServices.Instance.RemoveThemeChangedListener(this);
#endif
			_actualThemeChanged -= value;
		}
	}

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
		// Uno-specific gate (the foreground re-pull below emulates WinUI's TextFormatting pull
		// system): also re-pull on forced walks so hot reload refreshes the inherited brush.
		else if (IsEffectiveBaseThemeChanging(theme) || forceRefresh)
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
		RaiseActiveThemeChangedEventIfChanging(theme);
	}

	/// <summary>
	/// Occurs when the system high-contrast state affecting this element changes.
	/// </summary>
	/// <remarks>
	/// MUX Reference: KnownEventIndex::FrameworkElement_HighContrastChanged, raised by
	/// CFrameworkElement::RaiseActiveThemeChangedEventIfChanging (framework.cpp:3364-3376).
	/// Internal: the WinAppSDK does not expose this event on the public FrameworkElement surface
	/// (no Generated stub) — it is the engine-level companion of ActualThemeChanged.
	/// </remarks>
	internal event TypedEventHandler<FrameworkElement, object>? HighContrastChanged;

	// Called when there are default (system/app) theme changes. This enables us to notify
	// ActualThemeChanged listeners on this element when it's outside the live tree.
	// MUX Reference: CFrameworkElement::NotifyThemeChangedListeners — framework.cpp:3337-3344.
	// Invoked by the CCoreServices.NotifyThemeChange walk for elements registered through the
	// ActualThemeChanged accessors (CoreServices.AddThemeChangedListener).
	internal void NotifyThemeChangedListeners(Theme theme)
	{
		if (!IsActiveInVisualTree)
		{
			// Raise event if the effective theme is changing.
			RaiseActiveThemeChangedEventIfChanging(theme);
		}
	}

	// MUX Reference: CFrameworkElement::RaiseActiveThemeChangedEventIfChanging — framework.cpp:3346-3386
	private void RaiseActiveThemeChangedEventIfChanging(Theme theme)
	{
		// The actual/effective theme value on an element may change due to theme changes
		// in several places. The local theme value starts as None, which means default,
		// and doesn't change unless one of the following changes occurs. Though we may be
		// notified by these changes, the actual/effective theme value may remain the same.
		// For example, if the initial actual value is Dark by default from the system theme,
		// then setting RequestedTheme=Dark would trigger a theme walk here but wouldn't
		// change the actual value.
		// Possible causes of theme change notifications:
		//  - RequestedTheme change on this element or an ancestor.
		//  - RequestedTheme change on application.
		//  - System theme change.
		//  - System high contrast change.

		var theming = Uno.UI.Xaml.Core.CoreServices.Instance.Theming;

		// Raise HighContrastChanged event if it's high contrast that's changing.
		// (Both raises are posted — WinUI delivers them through the EventManager, framework.cpp:
		// 3367/3384; confirmed async by native ResourceDictionaryBasicTests.cpp:4209-4234, so
		// handlers observe the already-persisted new theme.)
		if (theming.IsHighContrastChanging())
		{
			Uno.UI.Dispatching.NativeDispatcher.Main.Enqueue(RaiseHighContrastChanged);

			// For backwards compatibility of ActualThemeChanged, we return only if
			// we are changing from HC to HC, or non-HC to HC. If we are changing
			// from HC to non-HC, we want to fire ActualThemeChanged as well.
			if (theming.HasHighContrastTheme())
			{
				return;
			}
		}

		// Raise ActualThemeChanged event if effective base (non-HighContrast) theme value is changing.
		if (IsEffectiveBaseThemeChanging(theme))
		{
			Uno.UI.Dispatching.NativeDispatcher.Main.Enqueue(RaiseActualThemeChanged);
		}
	}

	private void RaiseHighContrastChanged()
	{
		try
		{
			HighContrastChanged?.Invoke(this, null!);
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("HighContrastChanged handler threw an exception", e);
			}
		}
	}

	// MUX: framework.cpp:3378-3382 — "Raise ActualThemeChanged event if effective base
	// (non-HighContrast) theme value is changing": oldTheme == None ? theming->GetBaseTheme() :
	// GetBaseValue(oldTheme), compared to the incoming base, OR a never-walked element (None) while
	// a base (app/system) switch is in progress (FrameworkTheming::IsBaseThemeChanging). GetTheme()
	// still holds the pre-walk theme here (persisted after the core walk, Theming.cpp:155). No force
	// disjunct — forced walks (fForceRefresh) re-resolve values but do not raise on elements whose
	// effective theme is unchanged.
	private bool IsEffectiveBaseThemeChanging(Theme theme)
	{
		var theming = Uno.UI.Xaml.Core.CoreServices.Instance.Theming;
		var oldTheme = GetTheme();
		var oldBase = oldTheme == Theme.None ? theming.GetBaseTheme() : Theming.GetBaseValue(oldTheme);
		var newBase = Theming.GetBaseValue(theme);
		return oldBase != newBase
			|| (oldTheme == Theme.None && theming.IsBaseThemeChanging());
	}

	// MUX Reference framework.cpp, lines 3346-3386
	/// <summary>
	/// Raises the ActualThemeChanged event.
	/// </summary>
	private void RaiseActualThemeChanged()
	{
		try
		{
			_actualThemeChanged?.Invoke(this, null!);
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
		DependencyProperty? foregroundProperty = GetForegroundProperty();

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
#endif

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
			DependencyProperty? foregroundProperty = GetForegroundProperty();

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
#if UNO_HAS_ENHANCED_LIFECYCLE
			// MUX Reference: framework.cpp:3441-3487 — the element's theme is pushed onto the core
			// requested-theme-for-subtree slot around the CCoreServices::LookupThemeResource call and
			// the foreground freeze; the resolution leaf reads the slot (EnsureActiveThemeDictionary,
			// Resources.cpp:764-768).
			var core = Uno.UI.Xaml.Core.CoreServices.Instance;
			var prevSlotTheme = core.GetRequestedThemeForSubTree();
			var popSlotTheme = false;
			if (prevSlotTheme != Theming.GetBaseValue(theme))
			{
				core.SetRequestedThemeForSubTree(theme);
				popSlotTheme = true;
			}
#endif

			// On non-enhanced targets the try below compiles out, leaving a plain scope block so the
			// body keeps a single indentation shape across both compilations.
#if UNO_HAS_ENHANCED_LIFECYCLE
			try
#endif
			{
				// MUX: core->LookupThemeResource(L"DefaultTextForegroundThemeBrush") under the pushed slot.
				var brush = (Brush?)Uno.UI.Xaml.Core.CoreServices.Instance.LookupThemeResource(
					"DefaultTextForegroundThemeBrush");
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
#if UNO_HAS_ENHANCED_LIFECYCLE
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
			DependencyProperty? foregroundProperty = GetForegroundProperty();
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
	private DependencyProperty? GetForegroundProperty()
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
		var store = ((IDependencyObjectStoreProvider)this).Store;

		if (store.IsProcessingThemeWalk)
		{
			// MUX: Resources is a field-backed property in WinUI, so the theme walk notifies the
			// dictionary per-child — CResourceDictionary::NotifyThemeChangedCore →
			// CDOCollection::NotifyThemeChangedCore (DOCollection.cpp:1295-1325) calls
			// NotifyThemeChanged on every value, which re-resolves its theme references under the
			// walk's ambient slot theme. The engine path below would resolve them under this
			// element's per-object theme instead, which is stale during the walk — it is only
			// persisted once the walk completes (persist-after-Core, Theming.cpp:155).
			TryGetResources()?.NotifyThemeChanged(store.WalkTheme, store.WalkForceRefresh);
		}
		else
		{
			// Pass element's ActualTheme to resource binding updates
			TryGetResources()?.UpdateThemeBindings(updateReason);
		}

		store.UpdateResourceBindings(
			updateReason,
			resourceContextProvider: this);

		// Don't fire ActualThemeChanged here anymore - it's now fired
		// in NotifyThemeChangedCore when the theme actually changes
#else
		TryGetResources()?.UpdateThemeBindings(updateReason);
		((IDependencyObjectStoreProvider)this).Store.UpdateResourceBindings(updateReason);
		if (updateReason == ResourceUpdateReason.ThemeResource)
		{
			if (_actualThemeChanged != null && RequestedTheme == ElementTheme.Default)
			{
				try
				{
					_actualThemeChanged?.Invoke(this, null!);
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
