using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Android.OS;
using Android.Views;
using AndroidX.Core.View.Accessibility;
using AndroidX.CustomView.Widget;
using Java.Lang;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;

namespace Uno.UI.Runtime.Skia.Android;

internal sealed class UnoExploreByTouchHelper : ExploreByTouchHelper
{
	private const string ActionArgumentSetText = "ACTION_ARGUMENT_SET_TEXT_CHARSEQUENCE";
	private const string ActionArgumentSelectionStart = "ACTION_ARGUMENT_SELECTION_START_INT";
	private const string ActionArgumentSelectionEnd = "ACTION_ARGUMENT_SELECTION_END_INT";

	private readonly View _host;
	private UIElement? _rootElement;
	private ConditionalWeakTable<DependencyObject, object> _cwtElementToId = new();
	private Dictionary<int, WeakReference<DependencyObject>> _idToElement = new();
	private int _currentId;
	private readonly HashSet<DependencyObject> _rememberAllVisited = [];

	internal UIElement? RootElement => _rootElement ??= Microsoft.UI.Xaml.Window.CurrentSafe!.RootElement;

	public UnoExploreByTouchHelper(View host) : base(host)
	{
		_host = host;
	}

	private static bool ShouldSkipElement(DependencyObject element)
	{
		var accessibilityView = AutomationProperties.GetAccessibilityView(element);
		if (accessibilityView == AccessibilityView.Raw)
		{
			return true;
		}

		// TODO: What about non-UIElements? e.g, Hyperlinks?
		// In WinUI, `TextElement`s can have automation peers. We need to support that in Uno.
		if ((element as UIElement)?.GetOrCreateAutomationPeer() is null)
		{
			return true;
		}

		return false;
	}

	protected override int GetVirtualViewAt(float x, float y)
	{
		if (RootElement is null)
		{
			return ExploreByTouchHelper.HostId;
		}
		var (element, _) = VisualTreeHelper.HitTest(new Windows.Foundation.Point(x, y).PhysicalToLogicalPixels(), RootElement.XamlRoot?.VisualTree.RootElement);
		element ??= RootElement;
		try
		{
			FocusProperties.UnoForceGetTextBlockForAccessibility = true;
			while (!FocusProperties.IsPotentialTabStop(element) || ShouldSkipElement(element))
			{
				// Walking the tree up is not correct in the case of render transforms.
				// We could press on some coordinates and end up walking the tree up and retrieving
				// a parent that doesn't contain the pressed point.
				// TODO: Find a good way to handle this case.
				element = element.GetUIElementAdjustedParentInternal();
				if (element is null)
				{
					return ExploreByTouchHelper.HostId;
				}
			}

			if (element is RichEditBox richEditBox
				&& TryGetTextObjectAt(richEditBox, x, y, out var textObjectPeer))
			{
				return GetOrCreateVirtualId(textObjectPeer);
			}

			return GetOrCreateVirtualId(element);
		}
		finally
		{
			FocusProperties.UnoForceGetTextBlockForAccessibility = false;
		}
	}

	private static bool TryGetTextObjectAt(
		RichEditBox richEditBox,
		float physicalX,
		float physicalY,
		[NotNullWhen(true)] out AutomationPeer? textObjectPeer)
	{
		if (richEditBox.GetOrCreateAutomationPeer() is { } peer)
		{
			foreach (var child in peer.GetChildren() ?? Array.Empty<AutomationPeer>())
			{
				if (TryGetVirtualTextObjectBounds(child, out var bounds)
					&& bounds.Contains(new Windows.Foundation.Point(physicalX, physicalY)))
				{
					textObjectPeer = child;
					return true;
				}
			}
		}

		textObjectPeer = null;
		return false;
	}

	private int GetOrCreateVirtualId(DependencyObject element)
	{
		if (_cwtElementToId.TryGetValue(element, out var existingId))
		{
			return (int)existingId;
		}

		var id = Interlocked.Increment(ref _currentId);
		_cwtElementToId.Add(element, id);
		_idToElement.Add(id, new WeakReference<DependencyObject>(element));
		return id;
	}

	private bool TryGetVirtualElement(int virtualViewId, [NotNullWhen(true)] out DependencyObject? element)
	{
		if (_idToElement.TryGetValue(virtualViewId, out var weakReference)
			&& weakReference.TryGetTarget(out element))
		{
			return true;
		}

		_idToElement.Remove(virtualViewId);
		element = null;
		return false;
	}

	protected override void GetVisibleVirtualViews(IList<Integer>? virtualViewIds)
	{
		if (Microsoft.UI.Xaml.Window.CurrentSafe is null)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogWarning("No current window could be found.");
			}

			return;
		}

		var focusManager = VisualTree.GetFocusManagerForElement(RootElement);
		if (focusManager == null)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning("A focus manager couldn't be found to get virtual views.");
			}

			return;
		}

		if (virtualViewIds is null)
		{
			return;
		}

		try
		{
			FocusProperties.UnoForceGetTextBlockForAccessibility = true;

			var current = focusManager.GetNextTabStop(RootElement);
			var firstFocusable = current;
			while (current is not null)
			{
				if (!ShouldSkipElement(current))
				{
					virtualViewIds.Add(Integer.ValueOf(GetOrCreateVirtualId(current)));
					AddTextObjectVirtualViews(current, virtualViewIds);
				}

				_rememberAllVisited.Add(current);

				current = focusManager.GetNextTabStop(current);
				if (current is not null && _rememberAllVisited.Contains(current))
				{
					break;
				}
			}
		}
		finally
		{
			FocusProperties.UnoForceGetTextBlockForAccessibility = false;
			_rememberAllVisited.Clear();
		}
	}

	private void AddTextObjectVirtualViews(DependencyObject element, IList<Integer> virtualViewIds)
	{
		if (element is not RichEditBox richEditBox
			|| richEditBox.GetOrCreateAutomationPeer() is not { } peer)
		{
			return;
		}

		foreach (var child in peer.GetChildren() ?? Array.Empty<AutomationPeer>())
		{
			if (TryGetVirtualTextObjectBounds(child, out _))
			{
				virtualViewIds.Add(Integer.ValueOf(GetOrCreateVirtualId(child)));
			}
		}
	}

	protected override bool OnPerformActionForVirtualView(int virtualViewId, int action, Bundle? arguments)
	{
		if (!TryGetVirtualElement(virtualViewId, out var element))
		{
			return false;
		}

		var peer = element switch
		{
			AutomationPeer automationPeer => automationPeer,
			UIElement ownerElement => ownerElement.GetOrCreateAutomationPeer(),
			_ => null,
		};

		if (peer is RichEditBoxAutomationPeer
			&& peer.TryGetProviderOwner(out var owner)
			&& owner is RichEditBox richEditBox
			&& TryPerformRichEditAction(richEditBox, action, arguments))
		{
			InvalidateVirtualView(virtualViewId);
			return true;
		}

		if (peer is not null
			&& peer.IsEnabled())
		{
			if (peer.InvokeAutomationPeer())
			{
				InvalidateVirtualView(virtualViewId);
				return true;
			}
		}

		return false;
	}

	private static bool TryPerformRichEditAction(
		RichEditBox richEditBox,
		int action,
		Bundle? arguments)
	{
		if (action == AccessibilityNodeInfoCompat.ActionSetText
			&& Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
		{
			if (arguments is null || !arguments.ContainsKey(ActionArgumentSetText))
			{
				return false;
			}

			var text = arguments.GetCharSequence(ActionArgumentSetText)?.ToString() ?? string.Empty;
			return richEditBox.ApplyAccessibilityTextInput(text, text.Length, text.Length);
		}

		if (action == AccessibilityNodeInfoCompat.ActionSetSelection
			&& Build.VERSION.SdkInt >= BuildVersionCodes.JellyBeanMr2
			&& arguments is not null
			&& arguments.ContainsKey(ActionArgumentSelectionStart)
			&& arguments.ContainsKey(ActionArgumentSelectionEnd))
		{
			return richEditBox.ApplyAccessibilitySelection(
				arguments.GetInt(ActionArgumentSelectionStart),
				arguments.GetInt(ActionArgumentSelectionEnd));
		}

		return false;
	}

	protected override void OnPopulateNodeForVirtualView(int virtualViewId, AccessibilityNodeInfoCompat node)
	{
		if (!TryGetVirtualElement(virtualViewId, out var element))
		{
			return;
		}

		var peer = element switch
		{
			AutomationPeer automationPeer => automationPeer,
			UIElement ownerElement => ownerElement.GetOrCreateAutomationPeer(),
			_ => null,
		};

		if (peer is null)
		{
			node.ContentDescription = "N/A";
			node.Enabled = false;
			node.Editable = false;
			node.ClassName = "android.view.View";
			return;
		}

		if (element is AutomationPeer)
		{
			PopulateVirtualTextObjectNode(peer, node);
			return;
		}

		if (element is UIElement uiElement)
		{
			var transform = UIElement.GetTransform(from: uiElement, to: null);
			var logicalRect = transform.Transform(new Windows.Foundation.Rect(default, new Windows.Foundation.Size(uiElement.Visual.Size.X, uiElement.Visual.Size.Y)));
			var physicalRect = logicalRect.LogicalToPhysicalPixels();
#pragma warning disable CS0618 // Type or member is obsolete
			node.SetBoundsInParent(new global::Android.Graphics.Rect((int)physicalRect.Left, (int)physicalRect.Top, (int)physicalRect.Right, (int)physicalRect.Bottom));
#pragma warning restore CS0618 // Type or member is obsolete

				// TODO: Scrolling?

				var isClickable = peer is IInvokeProvider or IToggleProvider or ISelectionItemProvider;

				if (isClickable)
				{
					node.AddAction(AccessibilityNodeInfoCompat.AccessibilityActionCompat.ActionClick);
				}

				var automationControlType = peer!.GetAutomationControlType();

				node.ContentDescription = peer.GetName() ?? "";
				node.Password = peer.IsPassword();
				node.Enabled = peer.IsEnabled();
				// Call setChecked via JNI to stay compatible across the AndroidX.Core
				// signature change in 1.17 (setChecked(boolean) -> setChecked(int)).
				// See unoplatform/uno#22999.
				AccessibilityNodeInfoCompatJni.SetChecked(
					node,
					peer is IToggleProvider toggleProvider && toggleProvider.ToggleState == ToggleState.On);
				node.Checkable = peer is IToggleProvider;
				node.Clickable = isClickable;
				node.Editable = automationControlType == AutomationControlType.Edit;
				if (peer is RichEditBoxAutomationPeer && uiElement is RichEditBox richEditBox)
				{
					PopulateRichEditBoxNode(richEditBox, peer, node);
				}

				if (peer.GetLabeledBy() is FrameworkElementAutomationPeer labeledByPeer &&
					_cwtElementToId.TryGetValue(labeledByPeer.Owner, out var labeledByVirtualId))
				{
					node.SetLabeledBy(_host, (int)labeledByVirtualId);
				}

				node.Heading = peer.GetHeadingLevel() != AutomationHeadingLevel.None;
				node.HintText = peer.GetHelpText();
				var controlType = peer.GetAutomationControlType();
				// TalkBack appears to rely on the native qualified name. So, we have to transform common class names.
				// TODO: Is it correct to rely on AutomationControlType? or should we rely on our GetClassName? or a mix of both?
				var androidClassName = controlType switch
				{
					AutomationControlType.AppBar => "android.view.View",
					AutomationControlType.Button => "android.widget.Button",
					AutomationControlType.CheckBox => "android.widget.CheckBox",
					AutomationControlType.Calendar => "android.view.View",
					AutomationControlType.ComboBox => "android.widget.Spinner",
					AutomationControlType.Edit => "android.widget.EditText",
					AutomationControlType.Hyperlink => "android.view.View",
					AutomationControlType.Image => "android.widget.ImageView",
					AutomationControlType.ListItem => "android.view.View",
					AutomationControlType.List => "android.view.View",
					AutomationControlType.Menu => "android.view.View",
					AutomationControlType.MenuBar => "android.view.View",
					AutomationControlType.MenuItem => "android.view.View",
					AutomationControlType.ProgressBar => "android.view.View",
					AutomationControlType.RadioButton => "android.widget.RadioButton",
					AutomationControlType.ScrollBar => "android.view.View",
					AutomationControlType.Slider => "android.widget.SeekBar",
					AutomationControlType.Spinner => "android.view.View",
					AutomationControlType.StatusBar => "android.view.View",
					AutomationControlType.Tab => "android.view.View",
					AutomationControlType.TabItem => "android.view.View",
					AutomationControlType.Text => "android.view.View",
					AutomationControlType.ToolBar => "android.view.View",
					AutomationControlType.ToolTip => "android.view.View",
					AutomationControlType.Tree => "android.view.View",
					AutomationControlType.TreeItem => "android.view.View",
					AutomationControlType.Custom => "android.view.View",
					AutomationControlType.Group => "android.view.View",
					AutomationControlType.Thumb => "android.view.View",
					AutomationControlType.DataGrid => "android.view.View",
					AutomationControlType.DataItem => "android.view.View",
					AutomationControlType.Document => "android.view.View",
					AutomationControlType.SplitButton => "android.view.View",
					AutomationControlType.Window => "android.view.View",
					AutomationControlType.Pane => "android.view.View",
					AutomationControlType.Header => "android.view.View",
					AutomationControlType.HeaderItem => "android.view.View",
					AutomationControlType.Table => "android.view.View",
					AutomationControlType.TitleBar => "android.view.View",
					AutomationControlType.Separator => "android.view.View",
					AutomationControlType.SemanticZoom => "android.view.View",
					_ => "android.view.View",
				};

				node.ClassName = androidClassName;
		}
	}

	private void PopulateRichEditBoxNode(
		RichEditBox richEditBox,
		AutomationPeer peer,
		AccessibilityNodeInfoCompat node)
	{
		var text = richEditBox.GetAccessibilityText();
		richEditBox.GetAccessibilitySelection(
			out var selectionStart,
			out var selectionEnd,
			out _);
		node.Text = text;
		node.SetTextSelection(selectionStart, selectionEnd);
		node.MultiLine = true;
		var canEdit = richEditBox.IsEnabled && !richEditBox.IsReadOnly;
		node.Editable = canEdit;
		if (richEditBox.IsEnabled
			&& Build.VERSION.SdkInt >= BuildVersionCodes.JellyBeanMr2)
		{
			node.AddAction(AccessibilityNodeInfoCompat.AccessibilityActionCompat.ActionSetSelection);
		}
		if (canEdit
			&& Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
		{
			node.AddAction(AccessibilityNodeInfoCompat.AccessibilityActionCompat.ActionSetText);
		}

		foreach (var child in peer.GetChildren() ?? Array.Empty<AutomationPeer>())
		{
			if (TryGetVirtualTextObjectBounds(child, out _))
			{
				node.AddChild(_host, GetOrCreateVirtualId(child));
			}
		}
	}

	private void PopulateVirtualTextObjectNode(
		AutomationPeer peer,
		AccessibilityNodeInfoCompat node)
	{
		if (!TryGetVirtualTextObjectBounds(peer, out var bounds))
		{
			node.ContentDescription = "N/A";
			node.Enabled = false;
			node.Editable = false;
			node.ClassName = "android.view.View";
			return;
		}

#pragma warning disable CS0618 // Type or member is obsolete
		node.SetBoundsInParent(new global::Android.Graphics.Rect(
			(int)bounds.Left,
			(int)bounds.Top,
			(int)bounds.Right,
			(int)bounds.Bottom));
#pragma warning restore CS0618 // Type or member is obsolete
		node.ContentDescription = peer.GetName() ?? string.Empty;
		node.Enabled = peer.IsEnabled();
		node.Editable = false;
		node.Clickable = peer is IInvokeProvider && peer.IsEnabled();
		node.Focusable = peer.IsKeyboardFocusable();
		node.ClassName = peer.GetAutomationControlType() == AutomationControlType.Image
			? "android.widget.ImageView"
			: "android.widget.TextView";

		if (peer.GetParent() is { } parent
			&& parent.TryGetProviderOwner(out var parentOwner))
		{
			node.SetParent(_host, GetOrCreateVirtualId(parentOwner));
		}

		if (node.Clickable)
		{
			node.AddAction(AccessibilityNodeInfoCompat.AccessibilityActionCompat.ActionClick);
		}
	}

	private static bool TryGetVirtualTextObjectBounds(
		AutomationPeer peer,
		out Windows.Foundation.Rect physicalBounds)
	{
		physicalBounds = default;
		if (peer.GetAutomationControlType() is not (AutomationControlType.Hyperlink or AutomationControlType.Image)
			|| peer.IsOffscreen()
			|| string.IsNullOrEmpty(peer.GetName()))
		{
			return false;
		}

		var logicalBounds = peer.GetBoundingRectangle();
		if (logicalBounds.Width <= 0
			|| logicalBounds.Height <= 0
			|| !double.IsFinite(logicalBounds.X)
			|| !double.IsFinite(logicalBounds.Y)
			|| !double.IsFinite(logicalBounds.Width)
			|| !double.IsFinite(logicalBounds.Height))
		{
			return false;
		}

		physicalBounds = logicalBounds.LogicalToPhysicalPixels();
		return physicalBounds.Width > 0 && physicalBounds.Height > 0;
	}
}
