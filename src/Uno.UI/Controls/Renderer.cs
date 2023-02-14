using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml.Media;
using System.Linq;
using Uno.UI;
using Microsoft.UI.Xaml;
using Uno.Disposables;
using Windows.UI.Core;
using Uno.Collections;

namespace Uno.UI.Controls
{
	internal abstract class Renderer<TElement, TNative> : IDisposable
		where TElement : DependencyObject
		where TNative : class
	{
		private CompositeDisposable _subscriptions = new CompositeDisposable();
		private readonly WeakReference _element;
		private TNative _native;
		private bool _isRendering;

		public Renderer(TElement element)
		{
			if (element == null)
			{
				throw new ArgumentNullException(nameof(element));
			}

			_element = new WeakReference(element);
		}

		public TElement Element => (TElement)_element.Target;

		public TNative Native
		{
			get
			{
				if (_native == null)
				{
					// No Native instance was given.
					// We assume that the renderer knows how to create a new instance.
					_native = CreateNativeInstance();
					OnNativeChanged();
				}

				return _native;
			}
			set
			{
				if (!ReferenceEquals(_native, value))
				{
					_native = value;
					OnNativeChanged();
				}
			}
		}

		private void OnNativeChanged()
		{
			// We remove subscriptions to the previous pair of element and native 
			_subscriptions.Dispose();

			if (_native != null)
			{
				_subscriptions = new CompositeDisposable(Initialize());
			}

			Invalidate();
		}

		protected abstract TNative CreateNativeInstance();

		public void Invalidate()
		{
			// We don't render anything if there's no rendering target
			if (_native != null
				// Prevent Render() being called reentrantly - this can happen when the Element's parent changes within the Render() method
				&& !_isRendering)
			{
				try
				{
					_isRendering = true;
					Render();
				}
				finally
				{
					_isRendering = false;
				}
			}
		}

		protected abstract IEnumerable<IDisposable> Initialize();

		protected abstract void Render();

		public void Dispose()
		{
			_subscriptions.Dispose();
		}
	}

	internal static class RendererHelper
	{
		private static readonly WeakAttachedDictionary<DependencyObject, Type> _renderers = new WeakAttachedDictionary<DependencyObject, Type>();

		public static TRenderer GetRenderer<TElement, TRenderer>(this TElement element, Func<TRenderer> rendererFactory)
			where TElement : DependencyObject
		{
			return _renderers.GetValue(element, typeof(TRenderer), rendererFactory);
		}
		public static TRenderer ResetRenderer<TElement, TRenderer>(this TElement element, Func<TRenderer> rendererFactory)
			where TElement : DependencyObject
		{
			return _renderers.GetValue(element, typeof(TRenderer), rendererFactory);
		}
	}
}
