#nullable enable

using System;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Automation.Peers;

internal abstract class RichEditBoxTextObjectAutomationPeer : AutomationPeer, ITextChildProvider
{
	private readonly WeakReference<RichEditBox> _owner;
	private RichEditTextObjectInfo _info;
	private ITextRangeProvider? _textRange;
	private bool _isValid = true;
	private string _lastReportedName;
	private int _lastReportedStart;
	private int _lastReportedEnd;
	private Rect _lastBoundingRectangle;
	private bool _hasLastBoundingRectangle;

	protected RichEditBoxTextObjectAutomationPeer(RichEditBox owner, RichEditTextObjectInfo info)
	{
		_owner = new WeakReference<RichEditBox>(owner);
		_info = info;
		_lastReportedName = GetNameCore();
		_lastReportedStart = info.Start;
		_lastReportedEnd = info.End;
	}

	internal RichEditTextObjectInfo Info => _info;

	internal bool MatchesIdentity(RichEditTextObjectInfo info)
		=> _info.Kind == info.Kind
			&& ReferenceEquals(_info.Identity, info.Identity);

	internal void Update(RichEditTextObjectInfo info)
	{
		_info = info;
		_isValid = true;
	}

	internal void RaisePendingPropertyChanges()
	{
		var name = GetNameCore();
		if (!string.Equals(_lastReportedName, name, StringComparison.Ordinal)
			&& ListenerExistsHelper(AutomationEvents.PropertyChanged))
		{
			RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, _lastReportedName, name);
		}

		var newBounds = GetCurrentBoundingRectangle();
		if ((_lastReportedStart != _info.Start
				|| _lastReportedEnd != _info.End
				|| (_hasLastBoundingRectangle && _lastBoundingRectangle != newBounds))
			&& ListenerExistsHelper(AutomationEvents.PropertyChanged))
		{
			RaisePropertyChangedEvent(
				AutomationElementIdentifiers.BoundingRectangleProperty,
				_hasLastBoundingRectangle ? _lastBoundingRectangle : default,
				newBounds);
		}

		_lastReportedName = name;
		_lastReportedStart = _info.Start;
		_lastReportedEnd = _info.End;
		_lastBoundingRectangle = newBounds;
		_hasLastBoundingRectangle = true;
	}

	internal void Invalidate() => _isValid = false;

	internal ITextRangeProvider? CreateTextRangeProvider()
		=> GetOrCreateTextRangeProvider()?.Clone();

	private ITextRangeProvider? GetOrCreateTextRangeProvider()
	{
		if (_textRange is not null)
		{
			return _textRange;
		}

		if (!TryGetOwnerAndRange(out var owner, out var start, out var end)
			|| GetParent() is not RichEditBoxAutomationPeer parent)
		{
			return null;
		}

		return _textRange = new DirectUI.TextRangeAdapter(
			parent,
			owner,
			start,
			end,
			useObjectText: this is RichEditBoxImageAutomationPeer);
	}

	internal bool TryGetRange(out int start, out int end)
	{
		if (_isValid)
		{
			start = _info.Start;
			end = _info.End;
			return true;
		}

		start = 0;
		end = 0;
		return false;
	}

	protected bool TryGetOwnerAndRange(out RichEditBox owner, out int start, out int end)
	{
		if (_owner.TryGetTarget(out var target)
			&& GetParent() is RichEditBoxAutomationPeer parent
			&& parent.TryGetTextObjectRange(this, out start, out end))
		{
			owner = target;
			return true;
		}

		owner = null!;
		start = 0;
		end = 0;
		return false;
	}

	protected override object? GetPatternCore(PatternInterface patternInterface)
		=> patternInterface == PatternInterface.TextChild ? this : base.GetPatternCore(patternInterface);

	protected override string GetNameCore()
	{
		if (!string.IsNullOrEmpty(_info.Name))
		{
			return _info.Name;
		}

		return _info.Link is { } link && RichEditBox.TryGetLinkUri(link, out var uri)
			? uri.ToString()
			: string.Empty;
	}

	protected override Rect GetBoundingRectangleCore()
	{
		var bounds = GetCurrentBoundingRectangle();
		_lastBoundingRectangle = bounds;
		_hasLastBoundingRectangle = true;
		return bounds;
	}

	private Rect GetCurrentBoundingRectangle()
		=> _isValid
			&& _owner.TryGetTarget(out var owner)
			&& owner.TryGetAccessibilityRangeBounds(_info.Start, _info.End, out var bounds)
				? bounds
				: default;

	protected override Point GetClickablePointCore()
	{
		var bounds = GetBoundingRectangleCore();
		return bounds.Width > 0 && bounds.Height > 0
			? new Point(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2)
			: default;
	}

	protected override bool IsContentElementCore() => true;

	protected override bool IsControlElementCore() => true;

	protected override bool IsEnabledCore()
		=> TryGetOwnerAndRange(out var owner, out _, out _) && owner.IsEnabled;

	protected override bool IsOffscreenCore()
		=> !TryGetOwnerAndRange(out var owner, out var start, out var end)
			|| !owner.TryGetAccessibilityRangeBounds(start, end, out _);

	public IRawElementProviderSimple TextContainer
	{
		get
		{
			if (GetParent() is RichEditBoxAutomationPeer parent)
			{
				return new IRawElementProviderSimple(parent);
			}

			return _owner.TryGetTarget(out var owner)
				&& FrameworkElementAutomationPeer.CreatePeerForElement(owner) is { } ownerPeer
					? new IRawElementProviderSimple(ownerPeer)
					: null!;
		}
	}

	public ITextRangeProvider TextRange => CreateTextRangeProvider()!;
}

internal sealed class RichEditBoxLinkAutomationPeer : RichEditBoxTextObjectAutomationPeer, IInvokeProvider
{
	internal RichEditBoxLinkAutomationPeer(RichEditBox owner, RichEditTextObjectInfo info)
		: base(owner, info)
	{
	}

	protected override object? GetPatternCore(PatternInterface patternInterface)
		=> patternInterface == PatternInterface.Invoke ? this : base.GetPatternCore(patternInterface);

	protected override string GetClassNameCore() => "Hyperlink";

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Hyperlink;

	protected override bool IsKeyboardFocusableCore() => IsEnabledCore();

	protected override bool HasKeyboardFocusCore()
		=> TryGetOwnerAndRange(out var owner, out var start, out var end)
			&& owner.IsAccessibilityRangeFocused(start, end);

	protected override void SetFocusCore()
	{
		if (TryGetOwnerAndRange(out var owner, out var start, out var end))
		{
			owner.FocusAccessibilityRange(start, end);
		}
	}

	public void Invoke()
	{
		if (TryGetOwnerAndRange(out var owner, out var start, out _))
		{
			owner.TryNavigateLinkAt(start);
		}
	}
}

internal sealed class RichEditBoxImageAutomationPeer : RichEditBoxTextObjectAutomationPeer
{
	internal RichEditBoxImageAutomationPeer(RichEditBox owner, RichEditTextObjectInfo info)
		: base(owner, info)
	{
	}

	protected override string GetClassNameCore() => "Image";

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Image;
}
