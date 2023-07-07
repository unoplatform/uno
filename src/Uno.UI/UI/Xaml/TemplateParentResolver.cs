#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.UI.DataBinding;
using Uno.Disposables;
using Windows.UI.Xaml.Data;

namespace Uno.UI;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class TemplateParentResolver
{
	private static readonly Stack<TemplatedParentScope> _stack = new();

	internal static TemplatedParentScope? CurrentScope => _stack.TryPeek(out var scope) ? scope : default;

	private static TemplatedParentScope Push(ControlTemplate template, Control templatedParent)
	{
		var scope = new TemplatedParentScope(template, templatedParent);
		_stack.Push(scope);

		return scope;
	}

	internal static TemplatedParentScope Pop() => _stack.Pop();

	internal static IDisposable RentScope(ControlTemplate template, Control templatedParent, out TemplatedParentScope scope)
	{
		var capturedScope = scope = Push(template, templatedParent);
		var depth = _stack.Count;

		return Disposable.Create(() =>
		{
			if (_stack.Count == 0 ||
				_stack.Count != depth ||
				Pop() != capturedScope)
			{
				throw new InvalidOperationException("The scope has desynchronized");
			}
		});
	}

	internal class TemplatedParentScope
	{
		private ManagedWeakReference _controlTemplate;
		private ManagedWeakReference _templatedParent;
		//private List<WeakReference> _nestedTemplateBindings = new();
		public ManagedWeakReference TemplatedParentRef => _templatedParent;

		public ControlTemplate? ControlTemplate => _controlTemplate.Target as ControlTemplate; // fxime@xy: drop?
		public Control? TemplatedParent => _templatedParent.Target as Control;
		//public bool HasTemplatedParrent => _templatedParent != null;

		public TemplatedParentScope(ControlTemplate template, Control templatedParent)
		{
			_controlTemplate = (template as IWeakReferenceProvider).WeakReference;
			_templatedParent = (templatedParent as IWeakReferenceProvider).WeakReference;
		}

		//public void SetTemplatedParent(Control templatedParent)
		//{
		//	_templatedParent = (templatedParent as IWeakReferenceProvider).WeakReference;
		//	//if (_templatedParent is { })
		//	//{
		//	//	foreach (var wr in _nestedTemplateBindings)
		//	//	{
		//	//		if (wr.Target is BindingExpression binding)
		//	//		{
		//	//			binding.
		//	//		}
		//	//	}
		//	//}
		//}
	}
}
