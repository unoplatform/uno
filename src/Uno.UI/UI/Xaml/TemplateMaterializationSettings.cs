#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.Xaml;

[EditorBrowsable(EditorBrowsableState.Never)]
public class TemplateMaterializationSettings
{
	private readonly WeakReference? _templatedParentWR;

	public DependencyObject? TemplatedParent => _templatedParentWR?.Target as DependencyObject;
	public Action<DependencyObject>? TemplateMemberCreatedCallback { get; }

	public TemplateMaterializationSettings(DependencyObject? TemplatedParent, Action<DependencyObject>? TemplateMemberCreatedCallback)
	{
		if (TemplatedParent != null)
		{
			_templatedParentWR = new(TemplatedParent);
		}
		this.TemplateMemberCreatedCallback = TemplateMemberCreatedCallback;
	}
}
