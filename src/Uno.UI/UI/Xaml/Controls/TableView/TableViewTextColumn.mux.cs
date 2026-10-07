// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference controls\dev\TableView\TableViewTextColumn.cpp, tag winui3/release/2.5.4-experimental, commit 7b127093475

#nullable enable

using System;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Data;

namespace Microsoft.UI.Xaml.Controls.Tabular;

partial class TableViewTextColumn
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TableViewTextColumn"/> class.
	/// </summary>
	public TableViewTextColumn()
	{
	}

	// Setter lets XAML pass the Binding object through without evaluating it.
	/// <summary>
	/// Gets or sets the binding that produces the cell text. A CLR property so Binding-typed XAML
	/// values route through the setter.
	/// </summary>
	public Data.Binding? Binding
	{
		get => m_binding;
		set
		{
			m_binding = value;
			// Rebuild realized cells so the new Binding is applied immediately;
			// virtualized rows pick it up when realized.
			if (GetOwningTableView() is { } owner)
			{
				owner.OnColumnCellTemplateChanged(this);
			}
		}
	}

	/// <inheritdoc />
	protected override FrameworkElement? GenerateElementCore(object? dataItem)
	{
		// Let XAML binding pick up the row data item from the cell DataContext.
		TextBlock textBlock = new();
		var owner = GetOwningTableView();
		// Cell content is left-aligned and vertically centered within the row (standard grid look).
		textBlock.VerticalAlignment = VerticalAlignment.Center;
		// Fluent body text: theme font size / Normal.
		textBlock.FontSize = owner is not null ? owner.GetCellFontSize() : 14.0;
		textBlock.FontWeight = FontWeights.Normal;
		// Density-aware built-in cell padding (Standard = 8,4,8,4 = unchanged default).
		if (owner is not null)
		{
			textBlock.Padding = owner.GetDensityCellPadding();
		}
		else
		{
			textBlock.Padding = ThicknessHelper.FromLengths(8, 4, 8, 4);
		}
		textBlock.TextTrimming = TextTrimming.CharacterEllipsis;

		if (m_binding is { } binding)
		{
			BindingOperations.SetBinding(textBlock, TextBlock.TextProperty, binding);
		}

		return textBlock;
	}

	internal override FrameworkElement? GenerateEditingElementCore(object? dataItem)
	{
		// An explicit CellEditingTemplate wins: a text column is a convenience over the template path,
		// not a separate mechanism, so an app can replace the editor without subclassing.
		if (CellEditingTemplate is not null)
		{
			return base.GenerateEditingElementCore(dataItem);
		}

		// A column with no Binding has nothing to write back to, so it declines the edit rather than
		// opening a TextBox whose contents could never be committed.
		var binding = m_binding;
		if (binding is null)
		{
			return null;
		}

		TextBox textBox = new();
		var owner = GetOwningTableView();

		// Match the display cell's metrics so swapping the TextBlock for the TextBox does not shift the
		// text or resize the row as the edit opens.
		textBox.VerticalAlignment = VerticalAlignment.Center;
		textBox.FontSize = owner is not null ? owner.GetCellFontSize() : 14.0;
		textBox.FontWeight = FontWeights.Normal;
		textBox.Padding = owner is not null
			? owner.GetDensityCellPadding()
			: ThicknessHelper.FromLengths(8, 4, 8, 4);

		// The shipping TextBox style carries its own MinHeight (32px), which is taller than a Compact
		// TableView row (30px). Left alone, opening an editor grows the row and shifts the rows below
		// it. Clamp to the row's own minimum so the swap is visually neutral.
		if (owner is not null)
		{
			textBox.MinHeight = owner.GetDensityRowMinHeight();
		}

		// The consumer's Binding is reused rather than copied field-by-field so that Converter,
		// ConverterParameter, StringFormat and TargetNullValue all keep working in the editor.
		// Only the parts that editing requires are forced.
		Data.Binding editingBinding = new();
		editingBinding.Path = binding.Path;
		editingBinding.Converter = binding.Converter;
		editingBinding.ConverterParameter = binding.ConverterParameter;
		editingBinding.ConverterLanguage = binding.ConverterLanguage;
		editingBinding.TargetNullValue = binding.TargetNullValue;
		editingBinding.FallbackValue = binding.FallbackValue;

		// Carry the source selector across. Without this a display binding that targets an explicit
		// object or named element silently becomes a binding against the row's DataContext, and the
		// edit would be written to a different object than the one being displayed.
		if (binding.Source is { } source)
		{
			editingBinding.Source = source;
		}
		if (binding.ElementName is { } elementName && !string.IsNullOrEmpty(elementName))
		{
			editingBinding.ElementName = elementName;
		}
		if (binding.RelativeSource is { } relativeSource)
		{
			editingBinding.RelativeSource = relativeSource;
		}

		// TwoWay is required: a OneWay display binding would never write the edited value back.
		editingBinding.Mode = BindingMode.TwoWay;

		// Explicit rather than the default PropertyChanged: the control decides when the value lands on
		// the item, so a cancel can restore the pre-edit value and a validation failure can hold the
		// edit open. With PropertyChanged every keystroke would already have mutated the item.
		editingBinding.UpdateSourceTrigger = UpdateSourceTrigger.Explicit;

		BindingOperations.SetBinding(textBox, TextBox.TextProperty, editingBinding);

		return textBox;
	}

	internal override object? PrepareCellForEditCore(FrameworkElement? editingElement, RoutedEventArgs? editingEventArgs)
	{
		base.PrepareCellForEditCore(editingElement, editingEventArgs);

		var textBox = editingElement as TextBox;
		if (textBox is null)
		{
			// A CellEditingTemplate replaced the built-in TextBox; the base already handled focus.
			return null;
		}

		// Select-all matches the platform grid convention: the first keystroke replaces the value.
		textBox.SelectAll();

		// The pre-edit text, handed back to CancelCellEditCore. Not strictly needed while cancel is a
		// re-pull from the source, but it makes the column's revert independent of the binding.
		return textBox.Text;
	}

	// Exact, rather than the base's subtree walk: this column knows its editor is a TextBox bound on
	// Text, so it can commit and revert without inspecting anything.
	internal override bool CommitCellEditCore(FrameworkElement? editingElement)
	{
		var textBox = editingElement as TextBox;
		if (textBox is null)
		{
			return base.CommitCellEditCore(editingElement);
		}

		var expression = textBox.GetBindingExpression(TextBox.TextProperty);
		if (expression is null)
		{
			return false;
		}

		try
		{
			expression.UpdateSource();
		}
		catch (Exception)
		{
			// The app rejected the value in its setter.
			return false;
		}

		return true;
	}

	internal override void CancelCellEditCore(FrameworkElement? editingElement, object? uneditedValue)
	{
		// Restoring the text is not strictly required - the editor is discarded and the display element
		// re-reads the source, which an Explicit binding never wrote - but it keeps the editor coherent
		// for anything that observes it during teardown.
		if (editingElement is TextBox textBox)
		{
			if (uneditedValue is string original)
			{
				textBox.Text = original;
				return;
			}
		}

		base.CancelCellEditCore(editingElement, uneditedValue);
	}

	/// <inheritdoc />
	protected internal override string GetSortMemberPathCore()
	{
		// An explicit SortMemberPath always wins: it is how a consumer sorts on a field the cell does
		// not display (e.g. show a formatted name, sort on a sequence number).
		if (SortMemberPath is { } explicitPath && !string.IsNullOrEmpty(explicitPath))
		{
			return explicitPath;
		}

		// Otherwise sort on whatever the cell shows. The same source-qualification rule as editing
		// applies - a binding against an explicit source names a path on THAT object, and sorting the
		// rows by it would be meaningless.
		return GetEditingPropertyPath();
	}

	internal string GetEditingPropertyPath()
	{
		// Only reported for bindings against the row data item. When the binding names an explicit
		// source the path is relative to THAT object, and the control's snapshot/validation - which
		// resolve against the row item - would touch the wrong one. The editor's own binding expression
		// remains the commit mechanism there, and the empty answer keeps the control from guessing.
		if (m_binding is { } binding)
		{
			if (binding.Source is not null || binding.RelativeSource is not null || !string.IsNullOrEmpty(binding.ElementName))
			{
				return "";
			}

			if (binding.Path is { } path)
			{
				return path.Path;
			}
		}

		return "";
	}
}
