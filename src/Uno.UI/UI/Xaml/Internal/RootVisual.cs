// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// RootVisual.h, RootVisual.cpp

#nullable enable

using System;
using System.Linq;
using Uno.UI.Extensions;
using Windows.Foundation;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Input;

using PointerIdentifierPool = Windows.Devices.Input.PointerIdentifierPool; // internal type (should be in Uno namespace)
#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace Uno.UI.Xaml.Core;

/// <summary>
/// This class is the type of the invisible root of the tree.
/// This differs from Canvas in the way it arranges its children - it
/// ensures that all the children are arranged with the plugin size.
/// </summary>
internal partial class RootVisual : Panel, IRootElement
{
	private readonly CoreServices _coreServices;
	private readonly UnoRootElementLogic _rootElementLogic;

	public RootVisual(CoreServices coreServices)
	{
		_coreServices = coreServices ?? throw new System.ArgumentNullException(nameof(coreServices));
		_rootElementLogic = new(this);
	}

	void IRootElement.NotifyFocusChanged() => _rootElementLogic.NotifyFocusChanged();

	void IRootElement.ProcessPointerUp(PointerRoutedEventArgs args, bool isAfterHandledUp) =>
		_rootElementLogic.ProcessPointerUp(args, isAfterHandledUp);

	/// <summary>
	/// Gets or sets the Visual Tree.
	/// </summary>
	internal VisualTree? AssociatedVisualTree { get; set; }

	/// <summary>
	/// Updates the color of the background brush.
	/// </summary>
	/// <param name="backgroundColor">Background color.</param>
	internal void SetBackgroundColor(Color backgroundColor) =>
		SetValue(Panel.BackgroundProperty, new SolidColorBrush(backgroundColor));

	/// <summary>
	/// Overriding virtual to add specific logic to measure pass.
	/// This behavior is the same as that of the Canvas.
	/// </summary>
	/// <param name="availableSize">Available size.</param>
	/// <returns>Desired size.</returns>
	protected override Size MeasureOverride(Size availableSize)
	{
		foreach (var child in Children)
		{
			if (child != null)
			{
				// Measure child to the plugin size
				child.Measure(availableSize);
			}
		}

		return new Size();
	}

	/// <summary>
	/// Overriding CFrameworkElement virtual to add specific logic to arrange pass.
	/// The root visual always arranges the children with the finalSize. This ensures that
	/// children of the root visual are always arranged at the plugin size.
	/// </summary>
	/// <param name="finalSize">Final arranged size.</param>
	/// <returns>Final size.</returns>
	protected override Size ArrangeOverride(Size finalSize)
	{
		foreach (var child in Children)
		{
			if (child == null)
			{
				continue;
			}

			var x = child.GetOffsetX();
			var y = child.GetOffsetY();

			if (true)//child.GetIsArrangeDirty() || child.GetIsOnArrangeDirtyPath())
			{
				//child.EnsureLayoutStorage();

				var childRect = new Rect(x, y, finalSize.Width, finalSize.Height);

				child.Arrange(childRect);
			}
		}

		return finalSize;
	}
}
