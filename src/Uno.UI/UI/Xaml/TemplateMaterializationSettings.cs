#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml;

[EditorBrowsable(EditorBrowsableState.Never)]
public record TemplateMaterializationSettings(DependencyObject? TemplatedParent, Action<DependencyObject>? TemplateMemberCreatedCallback)
{
	public bool IsIgnored { get; set; } // fixme@xy: can we recover somehow?
}


