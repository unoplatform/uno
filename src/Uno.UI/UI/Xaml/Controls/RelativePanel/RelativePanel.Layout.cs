using Uno.Extensions;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using System.Linq;
using System.Text;
using Windows.UI.Xaml.Data;
using Uno.UI;

#if XAMARIN_ANDROID
using Android.Views;
#elif XAMARIN_IOS
using View = UIKit.UIView;
using UIKit;
#endif

namespace Windows.UI.Xaml.Controls
{
	partial class RelativePanel
	{
		protected override Size MeasureOverride(Size availableSize)
		{
			// Get the graph of all the children and enumerate them in the order they should be treated
			var graph = GetChildGraph(availableSize);

			return graph.MeasureSiblings(availableSize, Padding, MeasureElement);
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			// Get the graph of all the children and enumerate them in the order they should be treated
			var graph = GetChildGraph(finalSize);

			graph.ArrangeSiblings(finalSize, Padding, GetElementDesiredSize, ArrangeElement);

			return finalSize;
		}

		/// <summary>
		/// Gets the graph for all the children in the Panel
		/// </summary>
		/// <param name="availableSize">The Panel's available size</param>
		private SiblingGraph GetChildGraph(Size availableSize)
		{
			var children = Children
				.Select(c => c as IFrameworkElement)
				.Trim()
				.ToArray();

			var orderedChildren = new List<IFrameworkElement>();
			var childrenDependencies = new Dictionary<IFrameworkElement, Dependency[]>();

			// We need to order the children based on dependencies first before generating the graph in order to make sure there
			// are no circular references.  Once this is done, we can go ahead and build the full graph.
			foreach (var child in children)
			{
				var dependencies = GetDependencies(child, children);
				var dependencyElements = dependencies.SelectToArray(d => d.Element);

				ValidateDependencies(children, dependencyElements);

				childrenDependencies[child] = dependencies;

				OrderChildBasedOnDependencies(child, dependencyElements, orderedChildren);
			}

			ValidateCircularReferences(orderedChildren, childrenDependencies);

			return BuildGraph(orderedChildren.ToArray(), childrenDependencies);
		}

		/// <summary>
		/// Gets all the direct dependencies of a FrameworkElement (based on the set AttachedProperties)
		/// </summary>
		private Dependency[] GetDependencies(IFrameworkElement child, IFrameworkElement[] allChildren)
		{
			var dependencies = new List<Dependency>(Dependency.DependencyTypeCount);

			IFrameworkElement element;

			element = GetChild(RelativePanel.GetAbove(child), allChildren);
			if (element != null)
			{
				dependencies.Add(new Dependency(element, DependencyType.Above));
			}

			element = GetChild(RelativePanel.GetBelow(child), allChildren);
			if (element != null)
			{
				dependencies.Add(new Dependency(element, DependencyType.Below));
			}

			element = GetChild(RelativePanel.GetLeftOf(child), allChildren);
			if (element != null)
			{
				dependencies.Add(new Dependency(element, DependencyType.LeftOf));
			}

			element = GetChild(RelativePanel.GetRightOf(child), allChildren);
			if (element != null)
			{
				dependencies.Add(new Dependency(element, DependencyType.RightOf));
			}

			element = GetChild(RelativePanel.GetAlignBottomWith(child), allChildren);
			if (element != null)
			{
				dependencies.Add(new Dependency(element, DependencyType.AlignBottomWith));
			}

			element = GetChild(RelativePanel.GetAlignHorizontalCenterWith(child), allChildren);
			if (element != null)
			{
				dependencies.Add(new Dependency(element, DependencyType.AlignHorizontalCenterWith));
			}

			element = GetChild(RelativePanel.GetAlignLeftWith(child), allChildren);
			if (element != null)
			{
				dependencies.Add(new Dependency(element, DependencyType.AlignLeftWith));
			}

			element = GetChild(RelativePanel.GetAlignRightWith(child), allChildren);
			if (element != null)
			{
				dependencies.Add(new Dependency(element, DependencyType.AlignRightWith));
			}

			element = GetChild(RelativePanel.GetAlignTopWith(child), allChildren);
			if (element != null)
			{
				dependencies.Add(new Dependency(element, DependencyType.AlignTopWith));
			}

			element = GetChild(RelativePanel.GetAlignVerticalCenterWith(child), allChildren);
			if (element != null)
			{
				dependencies.Add(new Dependency(element, DependencyType.AlignVerticalCenterWith));
			}

			return dependencies.ToArray();
		}

		/// <summary>
		/// Make sure all dependencies are valid
		/// </summary>
		private void ValidateDependencies(IFrameworkElement[] children, IFrameworkElement[] dependencies)
		{
			foreach (var dependency in dependencies)
			{
				if (!children.Contains(dependency))
				{
					throw new InvalidOperationException("Cannot position an item in relation to a sibling that is not directly located in the RelativePanel.");
				}
			}
		}

		/// <summary>
		/// Order the child in a list so that the sibling it depends on are laid out before itself
		/// </summary>
		private void OrderChildBasedOnDependencies(IFrameworkElement child, IFrameworkElement[] dependencies, List<IFrameworkElement> orderedChildren)
		{
			if (orderedChildren.Count == 0 || dependencies.Length == 0)
			{
				orderedChildren.Insert(0, child);
				return;
			}

			int index = 0;
			var remainingDependencies = new List<IFrameworkElement>();
			remainingDependencies.AddRange(dependencies);

			for (int i = 0; i < orderedChildren.Count; i++)
			{
				var current = orderedChildren[i];
				remainingDependencies.Remove(current);
				index = i + 1;

				if (remainingDependencies.Count == 0)
				{
					break;
				}
			}

			orderedChildren.Insert(index, child);
		}

		/// <summary>
		/// Make sure there are no relationships between Siblings that make a circular dependency (E.G. A depends on B, B depends on C, C depends on A)
		/// </summary>
		private void ValidateCircularReferences(List<IFrameworkElement> orderedChildren, Dictionary<IFrameworkElement, Dependency[]> childrenDependencies)
		{
			var elements = new List<IFrameworkElement>();

			foreach (var child in orderedChildren)
			{
				if (childrenDependencies[child].Select(d => d.Element).Except(elements).Any())
				{
					throw new ArgumentException("RelativePanel error : Circular dependency detected.  Layout cannot complete.");
				}

				elements.Add(child);
			}
		}

		/// <summary>
		/// Build the sibling graph for the given chilren
		/// </summary>
		/// <param name="orderedChildren">The list of all children, properly ordered and without circular dependencies</param>
		/// <param name="childrenDependencies">The list of all given sibling dependencies</param>
		private SiblingGraph BuildGraph(
			IFrameworkElement[] orderedChildren,
			Dictionary<IFrameworkElement, Dependency[]> childrenDependencies
		)
		{
			var graph = new SiblingGraph();

			foreach (var child in orderedChildren)
			{
				graph.AddNode(child, childrenDependencies[child]);
			}

			return graph;
		}

		/// <summary>
		/// Gets a child instance from the named siblings, whether it's the name of a sibling, an instance or 
		/// an ElementNameSubject
		/// </summary>
		private IFrameworkElement GetChild(object obj, IEnumerable<IFrameworkElement> allChildren)
		{
			if (obj == null)
			{
				return null;
			}

			var name = obj as string;
			if (name.HasValue())
			{
				return allChildren.FirstOrDefault(fe => string.Equals(fe.Name, name));
			}

			var subject = obj as ElementNameSubject;
			if (subject == null)
			{
				return obj as IFrameworkElement;
			}

			if (subject.ElementInstance == null)
			{
				ElementNameSubject.ElementInstanceChangedHandler elementChangedEventHandler = null;

				elementChangedEventHandler = new ElementNameSubject.ElementInstanceChangedHandler((sender, newInstance) =>
				{
					this.InvalidateMeasure();

					if (!(newInstance is ElementStub))
					{
						// If the new instance is not an ElementStub, then the value 
						// will never change again, then we can remove the subscription.
						(sender as ElementNameSubject).ElementInstanceChanged -= elementChangedEventHandler;
					}
				});

				subject.ElementInstanceChanged += elementChangedEventHandler;
			}

			return subject.ElementInstance as IFrameworkElement;
		}

		private class Dependency
		{
			/// <summary>
			/// The number of possible <see cref="DependencyType "/> values. Must 
			/// be adjusted manually if the count changes.
			/// </summary>
			public const int DependencyTypeCount = 10;

			public Dependency(IFrameworkElement element, DependencyType type)
			{
				Element = element;
				Type = type;
			}

			public IFrameworkElement Element { get; private set; }

			public DependencyType Type { get; private set; }
		}

		private enum DependencyType
		{
			Above,
			Below,
			LeftOf,
			RightOf,
			AlignBottomWith,
			AlignLeftWith,
			AlignRightWith,
			AlignTopWith,
			AlignHorizontalCenterWith,
			AlignVerticalCenterWith
		}
	}
}