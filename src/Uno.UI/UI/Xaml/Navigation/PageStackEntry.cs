using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Loader;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.Extensions.Specialized;
using Uno.UI.DataBinding;
using Uno.UI.Helpers;
using System.Linq;

namespace Microsoft.UI.Xaml.Navigation;

/// <summary>
/// Represents an entry in the BackStack or ForwardStack of a Frame.
/// </summary>
public sealed partial class PageStackEntry : DependencyObject
{
	// Delimiter used to separate the assembly-qualified name from the ALC name in descriptors
	private const string AlcDescriptorDelimiter = "##";

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
		SetDescriptor(BuildDescriptor(sourcePageType));

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
		var sourcePageType = ResolveDescriptor(descriptor);
		spPageStackEntry.SourcePageType = sourcePageType;

		return spPageStackEntry;
	}

	internal static string BuildDescriptor(Type pageType)
	{
		var assemblyQualifiedName = pageType.AssemblyQualifiedName
			?? throw new ArgumentException($"Type {pageType.FullName} does not have an assembly-qualified name.", nameof(pageType));

		if (AssemblyLoadContext.GetLoadContext(pageType.Assembly) != AssemblyLoadContext.Default)
		{
			var alc = AssemblyLoadContext.GetLoadContext(pageType.Assembly);
			return $"{assemblyQualifiedName}{AlcDescriptorDelimiter}{alc.Name}";
		}

		return assemblyQualifiedName;
	}

	/// <summary>
	/// Resolves a type descriptor string to a Type instance.
	/// </summary>
	/// <param name="descriptor">The descriptor string, either an assembly-qualified name or a name with ALC suffix (##ALCName).</param>
	/// <returns>The resolved Type, or null if the type cannot be found or the ALC is not loaded.</returns>
	internal static Type ResolveDescriptor(string descriptor)
	{
		if (!descriptor.Contains(AlcDescriptorDelimiter))
		{
			// Type.GetType returns null if the type is not found
			return GetType(descriptor);
		}

		var alcParts = descriptor.Split(AlcDescriptorDelimiter, StringSplitOptions.None);

		if (AssemblyLoadContext.All.FirstOrDefault(alc => alc.Name == alcParts[1]) is { } alc)
		{
			using (alc.EnterContextualReflection())
			{
				// Type.GetType returns null if the type is not found in the ALC
				return GetType(alcParts[0]);
			}
		}

		throw new InvalidOperationException(
			$"Failed to resolve type descriptor '{descriptor}': AssemblyLoadContext with name '{alcParts[1]}' was not found.");
	}

	[UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "`Uno.UI.SourceGenerators/BindableTypeProviders` / `BindableMetadata.g.cs` ensures the type exists.")]
	static Type GetType(string typeName)
		=> Type.GetType(typeName);

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
