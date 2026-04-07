using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.DataBinding;

namespace Microsoft.UI.Xaml;

// TextFormatting partial — mirrors WinUI's TextFormatting storage group system.
// Provides EnsureTextFormatting (lazy pull), PullInheritedTextFormatting (virtual),
// GetParentTextFormatting (tree walk), and MarkInheritedPropertyDirty (push notification).
public partial class UIElement : ITextFormattingOwner
{
	// MUX ref: CUIElement::m_pTextFormatting — uielement.h
	internal TextFormatting _textFormatting;

	TextFormatting ITextFormattingOwner.TextFormatting => _textFormatting;

	// -----------------------------------------------------------------------
	//  EnsureTextFormatting — MUX ref: depends.cpp:3184-3246
	// -----------------------------------------------------------------------

	/// <summary>
	/// Ensures the TextFormatting storage exists and is up-to-date.
	/// If <paramref name="forGetValue"/> is <see langword="true"/> and
	/// the cached values are stale (generation counter mismatch) and the
	/// <paramref name="property"/> is at default (not set locally/by style),
	/// triggers <see cref="PullInheritedTextFormatting"/> to refresh from parent.
	/// </summary>
	void ITextFormattingOwner.EnsureTextFormatting(DependencyProperty property, bool forGetValue)
		=> EnsureTextFormatting(property, forGetValue);

	internal void EnsureTextFormatting(DependencyProperty property, bool forGetValue)
	{
		if (_textFormatting is null)
		{
			_textFormatting = TextFormatting.CreateDefault();
		}

		// MUX ref: UpdateTextFormatting (depends.cpp:3230-3243)
		// Pull only when: reading a value AND cache is stale AND property is at default
		if (forGetValue
			&& _textFormatting.IsOld
			&& (property is null || IsPropertyDefault(property)))
		{
			PullInheritedTextFormatting();
			_textFormatting.SetIsUpToDate();
		}
	}

	// -----------------------------------------------------------------------
	//  GetParentTextFormatting — MUX ref: depends.cpp:3265-3309
	// -----------------------------------------------------------------------

	/// <summary>
	/// Walks up the parent chain to find the nearest ancestor with a
	/// non-null TextFormatting. Ensures the ancestor's TextFormatting
	/// is up-to-date before returning it. If no ancestor is found,
	/// returns a default TextFormatting.
	/// </summary>
	internal TextFormatting GetParentTextFormatting()
	{
		var parent = this.GetParent();

		while (parent is not null)
		{
			TextFormatting tf = parent switch
			{
				UIElement uie => uie._textFormatting,
				Documents.TextElement te => te._textFormatting,
				_ => null
			};

			if (tf is not null)
			{
				// Make sure the parent's TextFormatting is up-to-date
				// MUX ref: EnsureTextFormatting(pInheritanceParent, NULL, TRUE)
				if (parent is UIElement puie)
				{
					puie.EnsureTextFormatting(null, forGetValue: true);
				}
				else if (parent is Documents.TextElement pte)
				{
					pte.EnsureTextFormatting(null, forGetValue: true);
				}

				return tf;
			}

			// Continue to grandparent
			parent = parent.GetParent();
		}

		// No parent found — return defaults
		// MUX ref: pTextCore->GetDefaultTextFormatting(ppTextFormatting)
		return TextFormatting.CreateDefault();
	}

	// -----------------------------------------------------------------------
	//  PullInheritedTextFormatting — MUX ref: uielement.cpp:1103-1137
	// -----------------------------------------------------------------------

	/// <summary>
	/// Pulls all inherited text formatting properties from the parent.
	/// The base UIElement implementation copies ALL properties from parent
	/// unconditionally (UIElement has no locally settable text properties).
	/// Overridden by FrameworkElement, TextBlock, Control, ContentPresenter,
	/// and RichTextBlock to check <see cref="IsPropertyDefault"/> per property.
	/// </summary>
	internal virtual void PullInheritedTextFormatting()
	{
		var parent = GetParentTextFormatting();

		_textFormatting.FontFamily = parent.FontFamily;

		if (!_textFormatting.FreezeForeground)
		{
			_textFormatting.Foreground = parent.Foreground;
		}

		_textFormatting.Language = parent.Language;
		_textFormatting.FontSize = parent.FontSize;
		_textFormatting.FontWeight = parent.FontWeight;
		_textFormatting.FontStyle = parent.FontStyle;
		_textFormatting.FontStretch = parent.FontStretch;
		_textFormatting.CharacterSpacing = parent.CharacterSpacing;
		_textFormatting.TextDecorations = parent.TextDecorations;
		_textFormatting.FlowDirection = parent.FlowDirection;
		_textFormatting.IsTextScaleFactorEnabled = parent.IsTextScaleFactorEnabled;
	}

	// -----------------------------------------------------------------------
	//  IsPropertyDefault — MUX ref: depends.cpp:2374 (IsPropertyDefaultByIndex)
	// -----------------------------------------------------------------------

	/// <summary>
	/// Returns <see langword="true"/> if the given DependencyProperty has no
	/// value set at any precedence level (Local, Style, Animation, etc.).
	/// Only properties at <see cref="DependencyPropertyValuePrecedences.DefaultValue"/>
	/// are considered "default" — inherited values at Inheritance precedence
	/// are NOT considered default.
	/// </summary>
	internal bool IsPropertyDefault(DependencyProperty property)
	{
		if (property is null)
		{
			return true;
		}

		return DependencyObjectExtensions.GetCurrentHighestValuePrecedence(this, property)
			== DependencyPropertyValuePrecedences.DefaultValue;
	}

	// -----------------------------------------------------------------------
	//  MarkInheritedPropertyDirty — MUX ref: uielement.cpp:14408-14460
	// -----------------------------------------------------------------------

	/// <summary>
	/// Walks visual children and raises property change notifications for
	/// inherited text properties. For each child that doesn't have a local
	/// value for the property, updates the child's TextFormatting and
	/// raises a synthetic PropertyChanged notification, then recurses.
	/// </summary>
	/// <remarks>
	/// This is the "push" side of the TextFormatting system. It immediately
	/// notifies children so that bindings, layout, and rendering respond
	/// to the change. The "pull" side (EnsureTextFormatting + generation
	/// counter) handles lazy value resolution on property reads.
	/// </remarks>
	internal virtual void MarkInheritedPropertyDirty(string propertyName, object newValue)
	{
		// MUX ref: FlowDirection gets special transform dirty handling
		// (uielement.cpp:14414). Not ported yet — handled separately.

		var childCount = VisualTreeHelper.GetChildrenCount(this);
		for (var i = 0; i < childCount; i++)
		{
			var child = VisualTreeHelper.GetChild(this, i);
			if (child is not UIElement childUIE)
			{
				continue;
			}

			// MUX ref: InheritedProperties::GetCorrespondingInheritedProperty
			var correspondingDP = TextFormattingHelper.GetCorrespondingTextProperty(childUIE, propertyName);

			if (correspondingDP is null || childUIE.IsPropertyDefault(correspondingDP))
			{
				// The property is not set locally on the child.
				// i.e. the child does not block inheritance of this property.

				// MUX ref: FreezeForeground check — when a child has FreezeForeground set
				// (it's a theme boundary), don't push Foreground into it or its subtree.
				if (propertyName == "Foreground"
					&& childUIE._textFormatting is { FreezeForeground: true })
				{
					continue;
				}

				if (correspondingDP is not null)
				{
					// MUX ref: childUIE->NotifyPropertyChanged(...)
					// Only update children that already have TextFormatting initialized.
					// Children without TextFormatting will pull inherited values lazily
					// via EnsureTextFormatting when their properties are read.
					var childTF = childUIE._textFormatting;
					if (childTF is not null)
					{
						var oldValue = childTF.GetFieldValue(propertyName);

						// Write the parent's value, then immediately let the child's
						// PullInheritedTextFormatting correct it. This is critical for
						// elements like FontIcon that override PullInheritedTextFormatting
						// with special defaults (e.g., FontSize=20 instead of parent's).
						// MUX ref: In WinUI, NotifyPropertyChanged doesn't directly write
						// to TextFormatting — the pull mechanism handles value correction.
						childTF.SetFieldValue(propertyName, newValue);
						childUIE.PullInheritedTextFormatting();
						childTF.SetIsUpToDate();

						var effectiveValue = childTF.GetFieldValue(propertyName);
						if (!Equals(oldValue, effectiveValue))
						{
							var store = ((IDependencyObjectStoreProvider)childUIE).Store;
							store.RaiseTextFormattingPropertyChanged(correspondingDP, oldValue, effectiveValue);
						}
					}
				}

				// Recursively propagate to child's children
				// MUX ref: childUIE->MarkInheritedPropertyDirty(pdp, pValue)
				childUIE.MarkInheritedPropertyDirty(propertyName, newValue);
			}
		}

		// Also propagate to inline children (TextBlock → Inlines)
		if (this is TextBlock textBlock)
		{
			textBlock.MarkInheritedPropertyDirtyForInlines(propertyName, newValue);
		}
	}
}
