using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Android.OS;
using AndroidX.Core.View.Accessibility;
using AndroidX.CustomView.Widget;
using Java.Lang;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Media;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;

namespace Uno.UI.Runtime.Skia.Android;

internal sealed class UnoExploreByTouchHelper : ExploreByTouchHelper
{
	private readonly UnoSKCanvasView _host;
	private UIElement? _rootElement;
	private ConditionalWeakTable<DependencyObject, object> _cwtElementToId = new();
	private Dictionary<int, DependencyObject?> _idToElement = new(); // TODO: This will leak.
	private int _currentId;
	private readonly HashSet<DependencyObject> _rememberAllVisited = [];

	internal UIElement? RootElement => _rootElement ??= Microsoft.UI.Xaml.Window.CurrentSafe!.RootElement;

	public UnoExploreByTouchHelper(UnoSKCanvasView host) : base(host)
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
		var (element, _) = VisualTreeHelper.HitTest(new Windows.Foundation.Point(x, y).PhysicalToLogicalPixels(), RootElement.XamlRoot);
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

			return GetOrCreateVirtualId(element);
		}
		finally
		{
			FocusProperties.UnoForceGetTextBlockForAccessibility = false;
		}
	}

	private int GetOrCreateVirtualId(DependencyObject element)
	{
		if (_cwtElementToId.TryGetValue(element, out var existingId))
		{
			return (int)existingId;
		}

		var id = Interlocked.Increment(ref _currentId);
		_cwtElementToId.Add(element, id);
		_idToElement.Add(id, element);
		return id;
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

	protected override bool OnPerformActionForVirtualView(int virtualViewId, int action, Bundle? arguments)
	{
		// TODO: What about non-UIElements? e.g, Hyperlinks?
		// In WinUI, `TextElement`s can have automation peers. We need to support that in Uno.
		if (_idToElement.TryGetValue(virtualViewId, out var element) &&
			element is UIElement uiElement &&
			uiElement.GetOrCreateAutomationPeer() is { } peer)
		{
			if (peer.InvokeAutomationPeer())
			{
				return true;
			}
		}

		return false;
	}

	protected override void OnPopulateNodeForVirtualView(int virtualViewId, AccessibilityNodeInfoCompat node)
	{
		// TODO: What about non-UIElements? e.g, Hyperlinks?
		// In WinUI, `TextElement`s can have automation peers. We need to support that in Uno.
		if (_idToElement.TryGetValue(virtualViewId, out var element) &&
			element is UIElement uiElement)
		{
			var transform = UIElement.GetTransform(from: uiElement, to: null);
			var logicalRect = transform.Transform(new Windows.Foundation.Rect(default, new Windows.Foundation.Size(uiElement.Visual.Size.X, uiElement.Visual.Size.Y)));
			var physicalRect = logicalRect.LogicalToPhysicalPixels();
#pragma warning disable CS0618 // Type or member is obsolete
			node.SetBoundsInParent(new global::Android.Graphics.Rect((int)physicalRect.Left, (int)physicalRect.Top, (int)physicalRect.Right, (int)physicalRect.Bottom));
#pragma warning restore CS0618 // Type or member is obsolete

			var peer = uiElement.GetOrCreateAutomationPeer();

			if (peer is null)
			{
				// No automation peer available for this element
				// anymore due to visibility changes.
				// The next frame will rebuild the accessiblity tree,
				// so we can temporarily mark this as unspecified.
				node.ContentDescription = "N/A";
				node.Enabled = false;
				node.Editable = false;
				node.ClassName = "android.view.View";
			}
			else
			{

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
				node.Checked = peer is IToggleProvider toggleProvider && toggleProvider.ToggleState == ToggleState.On;
				node.Checkable = peer is IToggleProvider;
				node.Clickable = isClickable;
				node.Editable = automationControlType == AutomationControlType.Edit;

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
	}
}
