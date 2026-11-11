#nullable enable

using Microsoft.UI.Xaml.Controls;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Core;

namespace Microsoft.UI.Xaml;

public partial class FrameworkElement
{
	private CodeNameRegistration? _codeNameRegistration;

	/// <summary>
	/// Registers a name set in code into the namescope the element entered, so that ElementName bindings can find it.
	/// </summary>
	/// <remarks>
	/// The XAML generator registers declared names itself, so only names it doesn't know about land here.
	/// MUX Reference: CDependencyObject::EnterImpl / RegisterName in depends.cpp.
	/// </remarks>
	private void RegisterCodeName()
	{
		var name = Name;
		if (_codeNameRegistration is not null
			|| string.IsNullOrEmpty(name)
			// Template parts belong to their template's namescope only (IsTemplateNamescopeMember in WinUI).
			|| GetTemplatedParent() is not null)
		{
			return;
		}

		// A root declared in XAML (UserControl, DataTemplate) is listed in its own scope.
		var ownScope = NameScope.GetNameScope(this);
		if (ReferenceEquals(ownScope?.FindName(name), this))
		{
			return;
		}

		// The parent's scope, not the element's own: a UserControl's scope holds the names inside it, not its own.
		var scope = GetNameScopeAnchor() is { } anchor ? NameScope.GetNameScope(anchor) : null;
		if (scope is not null && !ReferenceEquals(scope, ownScope) && ReferenceEquals(scope.FindName(name), this))
		{
			return;
		}

		var target = scope as NameScope ?? VisualTree.GetForElement(this, VisualTree.LookupOptions.NoFallback)?.CodeNameScope;
		if (target is null)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"No namescope to register the name [{name}] of [{this}] in, so ElementName bindings won't find it.");
			}

			return;
		}

		target.RegisterCodeName(name, this);
		_codeNameRegistration = new CodeNameRegistration(target, name);
	}

	/// <summary>
	/// Content shown through a control's template comes from that control, so its name belongs to the control's
	/// namescope rather than the template's, as in WinUI where the control enters it as a logical child.
	/// </summary>
	private DependencyObject? GetNameScopeAnchor()
	{
		var anchor = this.GetParent() as DependencyObject;
		while (anchor is FrameworkElement host
			&& host.GetTemplatedParent() is ContentControl contentControl
			&& ReferenceEquals(contentControl.GetValue(ContentControl.ContentProperty), this))
		{
			anchor = contentControl;
		}

		return anchor;
	}

	private void UnregisterCodeName()
	{
		if (_codeNameRegistration is { } registration)
		{
			_codeNameRegistration = null;
			registration.Scope.UnregisterCodeName(registration.Name, this);
		}
	}

	private sealed class CodeNameRegistration(NameScope scope, string name)
	{
		public NameScope Scope => scope;

		public string Name => name;
	}
}
