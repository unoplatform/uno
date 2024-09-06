#nullable enable

using System.ComponentModel;
using Uno.Extensions;
using Uno.Foundation.Logging;

namespace Microsoft.UI.Xaml
{
	public abstract partial class SetterBase
	{
		internal SetterBase()
		{
			IsAutoPropertyInheritanceEnabled = false;
		}

#if ENABLE_LEGACY_TEMPLATED_PARENT_SUPPORT
		internal virtual void PropagateTemplatedParent(DependencyObject tp)
		{
			TemplatedParentScope.UpdateTemplatedParent(this, tp, reapplyTemplateBindings: true);
		}
#endif

		internal abstract void ApplyTo(DependencyObject o);

		/// <summary>
		/// This method is present for binary backward compatibility with <see cref="Setter{T}"/>.
		/// Use <see cref="Setter.Property"/> or <see cref="Setter{T}.Property"/> instead.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public void set_Property(string name) => OnStringPropertyChanged(name);

		internal virtual void OnStringPropertyChanged(string name) { }

		partial void OnDataContextChangedPartial(DependencyPropertyChangedEventArgs e)
		{
			this.Log().Debug("SetterBase.DataContextChanged");
		}

		public bool IsSealed
		{
			get; private set;
		}

		internal void Seal()
			=> IsSealed = true;
	}
}

