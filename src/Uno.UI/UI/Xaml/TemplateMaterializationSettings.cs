#nullable enable

using System.Collections.Generic;
using System.ComponentModel;
using Uno.UI.DataBinding;

namespace Microsoft.UI.Xaml;

[EditorBrowsable(EditorBrowsableState.Never)]
public class TemplateMaterializationSettings
{
	private readonly ManagedWeakReference? _templatedParentWeakRef;
	private readonly List<DependencyObject>? _members;

	public DependencyObject? TemplatedParent => _templatedParentWeakRef?.Target as DependencyObject;

	internal TemplateMaterializationSettings(DependencyObject? templatedParent, List<DependencyObject>? members)
	{
		// Borrows the object's own weak reference instead of allocating a GC handle per materialization,
		// and reads the templated parent through the same path as DependencyObject.GetTemplatedParent.
		_templatedParentWeakRef = (templatedParent as IWeakReferenceProvider)?.WeakReference;

		_members = members;
	}

	/// <summary>
	/// Applies the materialization settings to a member created from the template.
	/// </summary>
	/// <remarks>
	/// An instance method rather than a callback on purpose: members materialized after the builder returned
	/// (a lazy <see cref="VisualState"/>, an unloaded x:Load element) capture these settings, so a delegate
	/// closing over the templated parent would keep it alive for as long as the template content -- which
	/// outlives it once the content is returned to the <see cref="FrameworkTemplatePool"/>. Reading the weak
	/// reference on each call keeps that impossible by construction.
	/// </remarks>
	[EditorBrowsable(EditorBrowsableState.Never)]
	public void OnMemberCreated(DependencyObject member)
	{
		member.SetTemplatedParent(TemplatedParent);
		_members?.Add(member);
	}
}
