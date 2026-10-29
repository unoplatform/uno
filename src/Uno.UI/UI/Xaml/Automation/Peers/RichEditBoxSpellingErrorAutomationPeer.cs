#nullable enable

using System;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Automation.Peers;

internal sealed class RichEditBoxSpellingErrorAutomationPeer : AutomationPeer, IAnnotationProvider
{
	private readonly WeakReference<RichEditBox> _owner;
	private RichEditSpellingAnnotationInfo _info;
	private bool _isValid = true;

	internal RichEditBoxSpellingErrorAutomationPeer(
		RichEditBox owner,
		RichEditSpellingAnnotationInfo info)
	{
		_owner = new WeakReference<RichEditBox>(owner);
		_info = info;
	}

	internal RichEditSpellingAnnotationInfo Info => _info;

	internal bool Matches(RichEditSpellingAnnotationInfo info)
		=> _info.Start == info.Start
			&& _info.End == info.End
			&& string.Equals(_info.Text, info.Text, StringComparison.Ordinal);

	internal void Update(RichEditSpellingAnnotationInfo info)
	{
		var oldName = GetNameCore();
		var oldHelpText = GetHelpTextCore();
		_info = info;
		_isValid = true;

		var newName = GetNameCore();
		if (!string.Equals(oldName, newName, StringComparison.Ordinal)
			&& ListenerExistsHelper(AutomationEvents.PropertyChanged))
		{
			RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, oldName, newName);
		}

		var newHelpText = GetHelpTextCore();
		if (!string.Equals(oldHelpText, newHelpText, StringComparison.Ordinal)
			&& ListenerExistsHelper(AutomationEvents.PropertyChanged))
		{
			RaisePropertyChangedEvent(AutomationElementIdentifiers.HelpTextProperty, oldHelpText, newHelpText);
		}
	}

	internal void Invalidate() => _isValid = false;

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

	internal ITextRangeProvider? CreateTextRangeProvider()
	{
		if (!TryGetOwnerAndRange(out var owner, out var start, out var end)
			|| GetParent() is not RichEditBoxAutomationPeer parent)
		{
			return null;
		}

		return new DirectUI.TextRangeAdapter(parent, owner, start, end);
	}

	private bool TryGetOwnerAndRange(out RichEditBox owner, out int start, out int end)
	{
		if (_owner.TryGetTarget(out var target)
			&& GetParent() is RichEditBoxAutomationPeer parent
			&& parent.TryGetSpellingAnnotationRange(this, out start, out end))
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
		=> patternInterface == PatternInterface.Annotation ? this : base.GetPatternCore(patternInterface);

	protected override string GetClassNameCore() => "SpellingError";

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Text;

	protected override string GetNameCore() => _info.Text;

	protected override string GetHelpTextCore()
		=> _info.Suggestions.Count == 0
			? string.Empty
			: string.Join(", ", _info.Suggestions);

	protected override Rect GetBoundingRectangleCore()
		=> TryGetOwnerAndRange(out var owner, out var start, out var end)
			&& owner.TryGetAccessibilityRangeBounds(start, end, out var bounds)
				? bounds
				: default;

	protected override bool IsContentElementCore() => true;

	protected override bool IsControlElementCore() => true;

	protected override bool IsEnabledCore()
		=> TryGetOwnerAndRange(out var owner, out _, out _) && owner.IsEnabled;

	protected override bool IsOffscreenCore()
		=> !TryGetOwnerAndRange(out var owner, out var start, out var end)
			|| !owner.TryGetAccessibilityRangeBounds(start, end, out _);

	public int AnnotationTypeId => (int)AnnotationType.SpellingError;

	public string AnnotationTypeName => "Spelling error";

	public string Author => string.Empty;

	public string DateTime => string.Empty;

	public IRawElementProviderSimple Target
		=> GetParent() is RichEditBoxAutomationPeer parent
			? new IRawElementProviderSimple(parent)
			: null!;
}
