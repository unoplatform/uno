#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.Apps.Test.Automation;
using ElementNotAvailableException = Microsoft.Windows.Apps.Test.Automation.ElementNotAvailableException;
using Private.Infrastructure;
using DrawingPoint = System.Drawing.Point;
using DrawingRectangle = System.Drawing.Rectangle;
using Rect = Windows.Foundation.Rect;
using WinPoint = Windows.Foundation.Point;

namespace Microsoft.Windows.Apps.Test.Foundation;

/// <summary>
/// In-process stand-in for MITA's UIA-backed UIObject: wraps the automation peer of an element
/// in the test window. Queries read the peer synchronously; input goes through the InputInjector.
/// </summary>
public class UIObject : IEquatable<UIObject>
{
	private const int DefaultGestureDistance = 100;

	private static IFactory<UIObject>? _factory;

	public UIObject(UIObject uiObject)
	{
		if (uiObject is null)
		{
			throw new ArgumentNullException(nameof(uiObject));
		}

		Peer = uiObject.Peer;
	}

	internal UIObject(AutomationPeer peer) => Peer = peer ?? throw new ArgumentNullException(nameof(peer));

	internal AutomationPeer Peer { get; }

	internal UIElement? Owner => (Peer as FrameworkElementAutomationPeer)?.Owner;

	public static UIObject Root
	{
		get
		{
			var content = TestServices.WindowHelper.WindowContent as FrameworkElement
				?? throw new ElementNotAvailableException("The test window has no content.");
			return new UIObject(new WindowRootAutomationPeer(content));
		}
	}

	public static UIObject? Focused
		=> TestServices.WindowHelper.XamlRoot is { } xamlRoot && FocusManager.GetFocusedElement(xamlRoot) is UIElement focused
			? FromElement(focused)
			: null;

	public static IFactory<UIObject> Factory => _factory ??= new UIObjectFactory();

	internal static UIObject? FromElement(UIElement element)
		=> FrameworkElementAutomationPeer.CreatePeerForElement(element) is { } peer ? new UIObject(peer) : null;

	internal static UIObject? FromProvider(object? provider) => provider is AutomationPeer peer ? new UIObject(peer) : null;

	public string AcceleratorKey => Peer.GetAcceleratorKey() ?? string.Empty;

	public string AccessKey => Peer.GetAccessKey() ?? string.Empty;

	public string AutomationId => Peer.GetAutomationId() ?? string.Empty;

	public DrawingRectangle BoundingRectangle
	{
		get
		{
			var rect = GetBounds();
			return new DrawingRectangle((int)Math.Round(rect.X), (int)Math.Round(rect.Y), (int)Math.Round(rect.Width), (int)Math.Round(rect.Height));
		}
	}

	public string ClassName => Peer.GetClassName() ?? string.Empty;

	public ControlType ControlType => ControlType.From(Peer.GetAutomationControlType());

	public string FrameworkId => "XAML";

	public bool HasKeyboardFocus => Peer.HasKeyboardFocus();

	public string HelpText => Peer.GetHelpText() ?? string.Empty;

	public bool IsContentElement => Peer.IsContentElement();

	public bool IsControlElement => Peer.IsControlElement();

	public bool IsEnabled => Peer.IsEnabled();

	public bool IsKeyboardFocusable => Peer.IsKeyboardFocusable();

	public bool IsOffscreen => Peer.IsOffscreen();

	public bool IsPassword => Peer.IsPassword();

	public bool IsRequiredForForm => Peer.IsRequiredForForm();

	public string ItemStatus => Peer.GetItemStatus() ?? string.Empty;

	public string ItemType => Peer.GetItemType() ?? string.Empty;

	public UIObject? LabeledBy => Peer.GetLabeledBy() is { } labeledBy ? new UIObject(labeledBy) : null;

	public string LocalizedControlType => Peer.GetLocalizedControlType() ?? string.Empty;

	public string Name => Peer.GetName() ?? string.Empty;

	public OrientationType Orientation => Peer.GetOrientation() switch
	{
		AutomationOrientation.Horizontal => OrientationType.Horizontal,
		AutomationOrientation.Vertical => OrientationType.Vertical,
		_ => OrientationType.None,
	};

	public UICollection<UIObject> Children => new(GetChildren(Peer).Select(peer => new UIObject(peer)));

	public UICollection<UIObject> Descendants => new(GetDescendants(Peer).Select(peer => new UIObject(peer)));

	public UICollection<UIObject> Ancestors => new(GetAncestors().Select(peer => new UIObject(peer)));

	public UICollection<UIObject> Siblings
		=> Parent is { } parent
			? new(parent.Children.Where(child => !child.Equals(this)))
			: new(Array.Empty<UIObject>());

	public UIObject? FirstChild => GetChildren(Peer).FirstOrDefault() is { } peer ? new UIObject(peer) : null;

	public UIObject? LastChild => GetChildren(Peer).LastOrDefault() is { } peer ? new UIObject(peer) : null;

	public UIObject? NextSibling => GetSibling(1);

	public UIObject? PreviousSibling => GetSibling(-1);

	public UIObject? Parent => GetParentPeer(Peer) is { } parent ? new UIObject(parent) : null;

	public void Click() => Click(PointerButtons.Primary);

	public void Click(PointerButtons button) => InteractionInput.Click(GetClickablePointCore(), button);

	public void Click(PointerButtons button, double offsetX, double offsetY) => InteractionInput.Click(GetOffsetPoint(offsetX, offsetY), button);

	public void DoubleClick() => DoubleClick(PointerButtons.Primary);

	public void DoubleClick(PointerButtons button) => InteractionInput.DoubleClick(GetClickablePointCore(), button);

	public void DoubleClick(PointerButtons button, double offsetX, double offsetY) => InteractionInput.DoubleClick(GetOffsetPoint(offsetX, offsetY), button);

	public void ClickDrag(UIObject uiObject) => ClickDrag(PointerButtons.Primary, uiObject);

	public void ClickDrag(PointerButtons button, UIObject uiObject) => InteractionInput.MouseDrag(GetClickablePointCore(), uiObject.GetClickablePointCore(), button);

	public void MovePointer() => InteractionInput.MoveMouse(GetClickablePointCore());

	public void MovePointer(double offsetX, double offsetY) => InteractionInput.MoveMouse(GetOffsetPoint(offsetX, offsetY));

	public void Tap() => InteractionInput.Tap(GetClickablePointCore());

	public void Tap(double offsetX, double offsetY) => InteractionInput.Tap(GetOffsetPoint(offsetX, offsetY));

	public void DoubleTap() => DoubleTap(BoundingRectangle.Width / 2.0, BoundingRectangle.Height / 2.0);

	public void DoubleTap(double offsetX, double offsetY)
	{
		var point = GetOffsetPoint(offsetX, offsetY);
		InteractionInput.Tap(point);
		InteractionInput.Tap(point);
	}

	public void TapAndHold() => TapAndHold(InteractionInput.DefaultHoldDuration);

	public void TapAndHold(uint duration) => InteractionInput.TapAndHold(GetClickablePointCore(), duration);

	public void TapAndHold(double offsetX, double offsetY) => TapAndHold(offsetX, offsetY, InteractionInput.DefaultHoldDuration);

	public void TapAndHold(double offsetX, double offsetY, uint duration) => InteractionInput.TapAndHold(GetOffsetPoint(offsetX, offsetY), duration);

	public void Pan(float direction) => Pan(DefaultGestureDistance, direction);

	public void Pan(int distance, float direction) => Pan(GetClickablePoint(), distance, direction);

	public void Pan(DrawingPoint startPoint, int distance, float direction) => Pan(startPoint, Offset(startPoint, distance, direction));

	public void Pan(DrawingPoint startPoint, DrawingPoint endPoint) => InteractionInput.Pan(ToWindowPoint(startPoint), ToWindowPoint(endPoint), holdDuration: 0, InteractionInput.DefaultDragDuration);

	public void Flick(float direction) => Flick(DefaultGestureDistance, direction);

	public void Flick(DrawingPoint startPoint, float direction) => Flick(startPoint, DefaultGestureDistance, direction);

	public void Flick(int distance, float direction) => Flick(GetClickablePoint(), distance, direction);

	public void Flick(DrawingPoint startPoint, int distance, float direction) => Flick(startPoint, Offset(startPoint, distance, direction));

	public void Flick(DrawingPoint startPoint, DrawingPoint endPoint) => InteractionInput.Pan(ToWindowPoint(startPoint), ToWindowPoint(endPoint), holdDuration: 0, dragDuration: 50);

	public DrawingPoint GetClickablePoint()
	{
		var point = GetClickablePointCore();
		return new DrawingPoint((int)Math.Round(point.X), (int)Math.Round(point.Y));
	}

	public bool Matches(UICondition condition) => condition.Matches(this);

	public static bool Matches(UIObject uiObject, UICondition condition) => condition.Matches(uiObject);

	public object? GetProperty(UIProperty property) => property.GetValue(this);

	public void SendKeys(string text)
	{
		SetFocus();
		TextInput.SendText(text);
	}

	public void SetFocus()
	{
		if (Owner is Control control)
		{
			control.Focus(FocusState.Keyboard);
		}
		else
		{
			Peer.SetFocus();
		}
	}

	internal object? GetPattern(PatternInterface patternInterface) => Peer.GetPattern(patternInterface);

	public bool Equals(UIObject? other) => other is not null && ReferenceEquals(other.Peer, Peer);

	public override bool Equals(object? obj) => obj is UIObject other && Equals(other);

	public override int GetHashCode() => Peer.GetHashCode();

	public override string ToString() => $"{ControlType.ProgrammaticName} '{Name}' (AutomationId: '{AutomationId}', ClassName: '{ClassName}')";

	internal Rect GetBounds()
	{
		if (Owner is FrameworkElement owner && owner.IsLoaded)
		{
			return owner.TransformToVisual(null).TransformBounds(new Rect(0, 0, owner.ActualWidth, owner.ActualHeight));
		}

		return Peer.GetBoundingRectangle();
	}

	private WinPoint GetClickablePointCore()
	{
		var bounds = GetBounds();
		return new WinPoint(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);
	}

	private WinPoint GetOffsetPoint(double offsetX, double offsetY)
	{
		var bounds = GetBounds();
		return new WinPoint(bounds.X + offsetX, bounds.Y + offsetY);
	}

	private static WinPoint ToWindowPoint(DrawingPoint point) => new(point.X, point.Y);

	// MITA angles: 0 is +x, counter-clockwise, with screen y pointing down.
	private static DrawingPoint Offset(DrawingPoint start, int distance, float direction)
	{
		var radians = direction * Math.PI / 180d;
		return new DrawingPoint(start.X + (int)Math.Round(distance * Math.Cos(radians)), start.Y - (int)Math.Round(distance * Math.Sin(radians)));
	}

	private UIObject? GetSibling(int offset)
	{
		if (GetParentPeer(Peer) is not { } parent)
		{
			return null;
		}

		var siblings = GetChildren(parent);
		var index = siblings.IndexOf(Peer);
		var siblingIndex = index + offset;
		return index >= 0 && siblingIndex >= 0 && siblingIndex < siblings.Count ? new UIObject(siblings[siblingIndex]) : null;
	}

	private IEnumerable<AutomationPeer> GetAncestors()
	{
		for (var parent = GetParentPeer(Peer); parent is not null; parent = GetParentPeer(parent))
		{
			yield return parent;
		}
	}

	internal static List<AutomationPeer> GetChildren(AutomationPeer peer) => peer.GetChildren()?.Where(child => child is not null).ToList() ?? new List<AutomationPeer>();

	internal static IEnumerable<AutomationPeer> GetDescendants(AutomationPeer peer)
	{
		foreach (var child in GetChildren(peer))
		{
			yield return child;

			foreach (var descendant in GetDescendants(child))
			{
				yield return descendant;
			}
		}
	}

	private static AutomationPeer? GetParentPeer(AutomationPeer peer)
	{
		if (peer.Navigate(AutomationNavigationDirection.Parent) is AutomationPeer parent)
		{
			return parent;
		}

		// Element peers that UIA would parent through the visual tree.
		if (peer is FrameworkElementAutomationPeer { Owner: { } owner })
		{
			for (var current = VisualTreeHelper.GetParent(owner); current is not null; current = VisualTreeHelper.GetParent(current))
			{
				if (current is UIElement element && FrameworkElementAutomationPeer.CreatePeerForElement(element) is { } ancestor)
				{
					return ancestor;
				}
			}
		}

		return null;
	}

	private sealed class UIObjectFactory : IFactory<UIObject>
	{
		public UIObject Create(UIObject element) => new(element);
	}

	/// <summary>
	/// Stands in for the UIA window element: its children are the test content and the open popups.
	/// </summary>
	private sealed partial class WindowRootAutomationPeer : FrameworkElementAutomationPeer
	{
		public WindowRootAutomationPeer(FrameworkElement content) : base(content)
		{
		}

		protected override IList<AutomationPeer> GetChildrenCore()
		{
			List<AutomationPeer> children = new();
			AddTopLevel(Owner, children);

			if (Owner.XamlRoot is { } xamlRoot)
			{
				foreach (var popup in VisualTreeHelper.GetOpenPopupsForXamlRoot(xamlRoot))
				{
					if (popup.Child is { } child)
					{
						AddTopLevel(child, children);
					}
				}
			}

			return children;
		}

		protected override string GetClassNameCore() => "Window";

		protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Window;

		private static void AddTopLevel(UIElement element, List<AutomationPeer> children)
		{
			if (FrameworkElementAutomationPeer.CreatePeerForElement(element) is { } peer)
			{
				children.Add(peer);
			}
			else if (element is FrameworkElement frameworkElement
				&& new FrameworkElementAutomationPeer(frameworkElement).GetChildren() is { } descendants)
			{
				children.AddRange(descendants);
			}
		}
	}
}
