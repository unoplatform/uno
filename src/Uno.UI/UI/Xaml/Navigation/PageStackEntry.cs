using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.UI.DataBinding;
using Uno.UI.Helpers;

namespace Microsoft.UI.Xaml.Navigation;

/// <summary>
/// Represents an entry in the BackStack or ForwardStack of a Frame.
/// </summary>
public sealed partial class PageStackEntry : DependencyObject
{
	// Descriptor -- this is the type of the page that corresponds to this entry
	private string m_descriptor;

	// Frame which owns Navigation History and its PageStackEntries
	private ManagedWeakReference m_wrFrame;

	/// <summary>
	/// Initializes a new instance of the PageStackEntry class.
	/// </summary>
	/// <param name="sourcePageType">The type of page associated with the navigation entry, as a type reference.</param>
	/// <param name="parameter">The navigation parameter associated with the navigation entry.</param>
	/// <param name="navigationTransitionInfo">Info about the animated transition associated with the navigation entry.</param>
	public PageStackEntry(
		[DynamicallyAccessedMembers(TypeMappings.TypeRequirements)]
		Type sourcePageType,
		object parameter,
		NavigationTransitionInfo navigationTransitionInfo)
	{
		InitializeBinder();

		SourcePageType = sourcePageType;
		SetDescriptor(sourcePageType.AssemblyQualifiedName);

		Parameter = parameter;
		NavigationTransitionInfo = navigationTransitionInfo;
	}

	private PageStackEntry()
	{
		InitializeBinder();
	}

	[UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "The provided type has been marked before getting at that location")]
	internal static PageStackEntry Create(
		 Frame frame,
		 string descriptor,
		 object parameter,
		 NavigationTransitionInfo transitionInfo)
	{
		PageStackEntry spPageStackEntry = new();

		spPageStackEntry.SetFrame(frame);
		spPageStackEntry.Parameter = parameter;
		spPageStackEntry.NavigationTransitionInfo = transitionInfo;

		spPageStackEntry.SetDescriptor(descriptor);
		var sourcePageType = Type.GetType(descriptor);
		spPageStackEntry.SourcePageType = sourcePageType;

		return spPageStackEntry;
	}

	//------------------------------------------------------------------------
	//
	//  Method: PrepareContent
	//
	//  Synopsis: 
	//     Set the frame on the page that is being navigated to.
	//
	//------------------------------------------------------------------------


	internal void PrepareContent(object contentObject)
	{
		var page = (Page)contentObject;

		var frame = GetFrame();
		// PrepareContent is called while navigating to a page and frame should never be null at this point.
		// So ensure, it is not null.
		if (frame is null)
		{
			throw new InvalidOperationException("Frame should not be null while navigating to a page.");
		}

		// Set page's frame (Page.Frame). 
		// Frame holds a strong on the page using Frame.Content, and page holds a strong on the 
		// frame using Page.Frame. So there is a cycle. The cycle is broken when frame's content is 
		// changed/cleared, when a new page is set as content or during visual tree tear down.  
		// Then the frame releases the on the old page, which should be the last on the page, 
		// unless the page is cached. When the page is released, it releases the on the frame. 
		page.Frame = frame;
		page.SetDescriptor(m_descriptor);
	}

	internal string GetDescriptor() => m_descriptor;

	private void SetDescriptor(string descriptor) => m_descriptor = descriptor;

	internal void SetFrame(Frame frame)
	{
		m_wrFrame = null;
		if (frame is not null)
		{
			m_wrFrame = WeakReferencePool.RentWeakReference(this, frame);
		}
	}

	private Frame GetFrame() => m_wrFrame?.IsAlive == true ? m_wrFrame.Target as Frame : null;

	//------------------------------------------------------------------------
	//
	//  Method: CanBeAddedToFrame
	//
	//  Synopsis: 
	//     Validate whether this PageStackEntry can be added to the Frame.
	//
	//------------------------------------------------------------------------

	internal bool CanBeAddedToFrame(Frame parameterFrame)
	{
		var canAdd = false;

		var frame = GetFrame();

		// The PageStackEntry can be added to the frame only if its frame is null or 
		// is equal to the frame we are trying to add this to.
		if (frame == null || parameterFrame == frame)
		{
			canAdd = true;
		}

		return canAdd;
	}
}
