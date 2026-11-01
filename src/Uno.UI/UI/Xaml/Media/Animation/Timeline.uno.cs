#nullable enable

using System;
using Microsoft.UI.Xaml.Data;
using Uno.Foundation.Logging;
using Uno.UI.DataBinding;

namespace Microsoft.UI.Xaml.Media.Animation;

partial class Timeline : IThemeChangeAware
{
	private BindingPath? _propertyPath;

	/// <summary>
	/// The target set by <see cref="Storyboard.SetTarget(Timeline, DependencyObject)"/> (the manual target).
	/// </summary>
	internal DependencyObject? Target
	{
		get => GetTargetObjectNoRef();
		set => m_manualTargetObjectWeakRef = value is null ? null : new WeakReference<DependencyObject>(value);
	}

	/// <summary>
	/// Sets the manual target through an ElementNameSubject, for a target materialized lazily (x:Load, ElementStub).
	/// </summary>
	internal void SetElementNameTarget(ElementNameSubject subject)
	{
		ElementNameSubject.ElementInstanceChangedHandler? handler = null;

		handler = (s, e) =>
		{
			Target = e as DependencyObject;

			// An ElementStub is replaced by the real element later; anything else is final.
			if (Target is not null && e is not ElementStub)
			{
				subject.ElementInstanceChanged -= handler;
			}
		};

		Target = subject.ElementInstance as DependencyObject;

		subject.ElementInstanceChanged += handler;
	}

	/// <summary>The resolved target object (m_targetObjectWeakRef).</summary>
	private protected DependencyObject? GetResolvedTargetObject()
		=> m_targetObjectWeakRef is not null && m_targetObjectWeakRef.TryGetTarget(out var target) ? target : null;

	/// <summary>
	/// CCoreServices::ParsePropertyPath: walks a property path ("(UIElement.RenderTransform).(CompositeTransform.TranslateX)")
	/// down to the object and the property it ends on.
	/// </summary>
	private bool ParsePropertyPath(ref DependencyObject targetObject, out DependencyProperty? property, string path)
	{
		property = null;

		var propertyPath = GetPropertyPath(path, targetObject);

		// Animating a brush or transform shared with other elements must not affect them.
		propertyPath.CloneShareableObjectsInPath();

		IBindingItem? leaf = null;
		foreach (var item in propertyPath.GetPathItems())
		{
			leaf = item;
		}

		if (leaf?.DataContext is not DependencyObject leafObject || string.IsNullOrEmpty(leaf.PropertyName))
		{
			return false;
		}

		property = DependencyProperty.GetProperty(leafObject.GetType(), leaf.PropertyName);
		if (property is null)
		{
			return false;
		}

		targetObject = leafObject;
		return true;
	}

	private BindingPath GetPropertyPath(string path, DependencyObject target)
	{
		if (_propertyPath is null || _propertyPath.Path != path)
		{
			if (_propertyPath is not null)
			{
				_propertyPath.DataContext = null;
				_propertyPath.Dispose();
			}

			_propertyPath = new BindingPath(path: path, fallbackValue: null, forAnimations: true, allowPrivateMembers: false);
		}

		_propertyPath.DataContext = target;
		return _propertyPath;
	}

	/// <summary>
	/// The path of the targeted property bound to the current target. Lets the visual state manager find out which
	/// properties a storyboard animates.
	/// </summary>
	internal BindingPath? PropertyInfo
	{
		get
		{
			var targetProperty = Storyboard.GetTargetProperty(this);
			if (string.IsNullOrEmpty(targetProperty))
			{
				return null;
			}

			var target = Target ?? ResolveNameFromParents(Storyboard.GetTargetName(this), GetTimingParent());

			return target is null ? null : GetPropertyPath(targetProperty, target);
		}
	}

	private DependencyObject? ResolveNameFromParents(string? name, Timeline? parentTimeline)
	{
		if (string.IsNullOrEmpty(name))
		{
			return null;
		}

		var resolved = FindNameFrom(this, name!);
		if (resolved is null && parentTimeline is not null)
		{
			resolved = FindNameFrom(parentTimeline, name!);
		}

		if (resolved is null && this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Failed to find target {name} on {this.GetParent()?.GetType()}");
		}

		return resolved;

		static DependencyObject? FindNameFrom(DependencyObject start, string name)
		{
			object? current = start;
			while ((current = current.GetParent()) is not null)
			{
				if (current is FrameworkElement fe)
				{
					return fe.FindName(name) as DependencyObject;
				}
			}

			return null;
		}
	}

	// The framework timelines that are neither animations nor storyboards: the theme animations, which WinUI implements as
	// dynamic timelines.
	private bool IsUnportedThemeTimeline()
		=> !IsAnimation() && this is not Storyboard && GetType().Assembly == typeof(Timeline).Assembly && GetType().Name.EndsWith("ThemeAnimation", StringComparison.Ordinal);

	// RepeatBehaviorVOHelper::Create: the core value only keeps the field that matters for its type, and the count of a
	// Duration or Forever repeat behavior is the default count (1), where the public struct has 0.
	private static RepeatBehavior RepeatBehaviorVOHelperCreate(RepeatBehavior value)
		=> value.Type switch
		{
			RepeatBehaviorType.Duration => new RepeatBehavior(value.Duration) { Count = 1 },
			RepeatBehaviorType.Forever => new RepeatBehavior { Type = RepeatBehaviorType.Forever, Count = 1 },
			_ => new RepeatBehavior(value.Count),
		};

	// XcpCeiling, as a frame request delay in milliseconds.
	private protected static uint XcpCeiling(double milliseconds)
	{
		var ceiling = Math.Ceiling(milliseconds);
		return ceiling <= 0 ? 0 : ceiling >= uint.MaxValue ? uint.MaxValue : (uint)ceiling;
	}

	/// <summary>
	/// Gives this timeline, its key frames and its child timelines the theme WinUI establishes when they enter the tree
	/// (CDependencyObject::EnterImpl theme block), so their {ThemeResource} references resolve in that theme.
	/// </summary>
	/// <remarks>
	/// TODO Uno: Uno's theme walk doesn't recurse into children that aren't UIElements yet, and visual state storyboards
	/// are materialized after their element entered the tree.
	/// </remarks>
	internal void EstablishTheme(Theme theme)
	{
		// A no-op for objects that already have the theme.
		NotifyThemeChanged(theme);

		if (KeyFrames_Core is { } keyFrames)
		{
			foreach (var keyFrame in keyFrames)
			{
				(keyFrame as DependencyObject)?.NotifyThemeChanged(theme);
			}
		}

		if (this is Storyboard storyboard)
		{
			foreach (var child in storyboard.Children)
			{
				child.EstablishTheme(theme);
			}
		}
	}

	void IThemeChangeAware.OnThemeChanged() => NotifyThemeChangedCore();

	// CDependencyObject::NotifyThemeChangedCore
	private protected virtual void NotifyThemeChangedCore()
	{
		// TODO Uno specific: CAnimation is folded into Timeline.
		if (IsAnimation())
		{
			AnimationNotifyThemeChangedCore();
		}
	}
}
