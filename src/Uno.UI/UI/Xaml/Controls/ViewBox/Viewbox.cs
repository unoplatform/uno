// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using Windows.Foundation;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Uno.UI;

using static Uno.Helpers.MathHelpers;

namespace Windows.UI.Xaml.Controls;

/// <summary>
/// Defines a content decorator that can stretch and scale a single child to fill the available space.
/// </summary>
[ContentProperty(Name = nameof(Child))]
public partial class Viewbox : global::Windows.UI.Xaml.FrameworkElement, ILayoutOptOut
{
	public UIElement Child
	{
		get => m_pContainerVisual.Child;
		set => m_pContainerVisual.Child = value;
	}

	bool ILayoutOptOut.ShouldUseMinSize => false;

	private void AddChildNative(UIElement child) => AddChild(child);
}

public partial class Viewbox // Viewbox.h
{
	#region private:
	//
	// CViewbox(_In_ CCoreServices *pCore)
	//     : CFrameworkElement(pCore)
	// {}

	//~CViewbox();

	//_Check_return_ XSIZEF ComputeScaleFactor(XSIZEF availableSize, XSIZEF contentSize);

	private ScaleTransform m_pScaleTransform;
	#endregion

	#region public:
	// Creation method
	//DECLARE_CREATE(CViewbox);

	// CDependencyObject overrides
	//KnownTypeIndex GetTypeIndex() const override
	//{
	//	return DependencyObjectTraits<CViewbox>::Index;
	//}

	//override bool GetIsLayoutElement() const override sealed { return true; }

	internal override bool CanHaveChildren() => true;

	//__override virtual _Check_return_ HRESULT AddChild(_In_ CUIElement* pChild);

	//virtual _Check_return_ HRESULT InitInstance();

	// Method to set the Child property
	// using PROP_METHOD_CALL
	//static _Check_return_ HRESULT Child(
	//	_In_ CDependencyObject *pObject,
	//	_In_ XUINT32 cArgs,
	//	_Inout_updates_(cArgs) CValue *ppArgs,
	//	_In_opt_ IInspectable* pValueOuter,
	//	_Out_ CValue *pResult);

	//virtual _Check_return_ HRESULT SetChild( _In_ CUIElement* pContent);
	//virtual _Check_return_ HRESULT GetChild( _Outptr_ CUIElement** ppContent);

	//DirectUI::StretchDirection m_stretchDirection = DirectUI::StretchDirection::Both;
	//DirectUI::Stretch m_stretch = DirectUI::Stretch::Uniform;
	private Border m_pContainerVisual;
	#endregion

	#region protected:
	//virtual _Check_return_ HRESULT MeasureOverride(
	//	XSIZEF availableSize,
	//	XSIZEF& desiredSize);

	//virtual _Check_return_ HRESULT ArrangeOverride(
	//	XSIZEF finalSize,
	//	XSIZEF& newFinalSize);
	#endregion
}

public partial class Viewbox // Viewbox.cpp
{
	/// <summary>
	/// Initialize internal members
	/// </summary>
	public Viewbox()
	{
		//HRESULT hr = S_OK;

		//CREATEPARAMETERS cp(GetContext());

#if HAS_UNO
		HorizontalAlignment = HorizontalAlignment.Center;
		VerticalAlignment = VerticalAlignment.Center;
#endif

		m_pContainerVisual = new Border();
		m_pScaleTransform = new ScaleTransform();

		m_pContainerVisual.RenderTransform = m_pScaleTransform;
		AddChildNative(m_pContainerVisual);

		//Cleanup:
		//RRETURN(hr);
	}

	~Viewbox()
	{
		//CUIElement* pExistingLogicalChild = NULL;

		//if (GetChild() is { } pExistingLogicalChild)
		//{
		//	RemoveLogicalChild(pExistingLogicalChild);
		//}

		//ReleaseInterface(pExistingLogicalChild);
		m_pScaleTransform = null;
		m_pContainerVisual = null;
	}

#if false
	/// <summary>
	/// This is overridden so that we can return an error if the
	/// parser adds more than one child to the Viewbox.
	/// </summary>
	private void AddChild(UIElement pChild)
	{
		//HRESULT hr = S_OK;
		//CUIElement* pExistingChild = NULL;

		//IFCEXPECT(pChild != NULL);

		// Can only have one child!
		//pExistingChild = GetFirstChildNoAddRef();
		//IFCEXPECT(pExistingChild == NULL);

		//IFC(CFrameworkElement::AddChild(pChild));

		//Cleanup:
		//RRETURN(hr);
	}
#endif

	/// <summary>
	/// Compute the scale factor of the Child content.
	/// </summary>
	Size ComputeScaleFactor(Size availableSize, Size contentSize)
	{
		Size desiredSize = default;
		var scaleX = 1.0;
		var scaleY = 1.0;

		var isConstrainedWidth = !double.IsInfinity(availableSize.Width);
		var isConstrainedHeight = !double.IsInfinity(availableSize.Height);

		// Don't scale if we shouldn't stretch or the scaleX and scaleY are both infinity.
		if ((Stretch != Stretch.None) && (isConstrainedWidth || isConstrainedHeight))
		{
			// Compute the individual scaleX and scaleY scale factors
			scaleX = IsCloseReal(contentSize.Width, 0.0) ? 0.0 : (availableSize.Width / contentSize.Width);
			scaleY = IsCloseReal(contentSize.Height, 0.0) ? 0.0 : (availableSize.Height / contentSize.Height);

			// Make the scale factors uniform by setting them both equal to
			// the larger or smaller (depending on infinite lengths and the
			// Stretch value)
			if (!isConstrainedWidth)
			{
				scaleX = scaleY;
			}
			else if (!isConstrainedHeight)
			{
				scaleY = scaleX;
			}
			else
			{
				switch (Stretch)
				{
					case Stretch.Uniform:
						// Use the smaller factor for both
						scaleX = scaleY = Math.Min(scaleX, scaleY);
						break;
					case Stretch.UniformToFill:
						// Use the larger factor for both
						scaleX = scaleY = Math.Max(scaleX, scaleY);
						break;
					case Stretch.Fill:
					default:
						break;
				}
			}

			// Prevent scaling in an undesired direction
			switch (StretchDirection)
			{
				case StretchDirection.UpOnly:
					scaleX = Math.Max(1.0, scaleX);
					scaleY = Math.Max(1.0, scaleY);
					break;
				case StretchDirection.DownOnly:
					scaleX = Math.Min(1.0, scaleX);
					scaleY = Math.Min(1.0, scaleY);
					break;
				case StretchDirection.Both:
				default:
					break;
			}
		}

		desiredSize.Width = scaleX;
		desiredSize.Height = scaleY;

		return desiredSize;
	}

#if false
	//-------------------------------------------------------------------------
	//
	//  Function:   CViewbox::Child()
	//
	//  Synopsis:   This is the child property getter and setter method. Note
	//              that the storage for this property is actually this children
	//              collection.
	//
	//-------------------------------------------------------------------------
	_Check_return_
	HRESULT
	CViewbox::Child(
		_In_ CDependencyObject * pObject,
		_In_ XUINT32 cArgs,
		_Inout_updates_(cArgs) CValue* ppArgs,
		_In_opt_ IInspectable* pValueOuter,
		_Out_ CValue* pResult)
	{
		HRESULT hr = S_OK;
	CViewbox* pViewbox = NULL;

	IFC(DoPointerCast(pViewbox, pObject));

		if (cArgs == 0)
		{
			// Getting the child
			CUIElement* pChild = NULL;
	hr = pViewbox->GetChild(&pChild);
			if (SUCCEEDED(hr))
			{
				pResult->SetObjectNoRef(pChild);
}
			else
			{
				pResult->SetNull();
IFC(hr);
			}
		}
		else if (cArgs == 1 && ppArgs->GetType() == valueObject)
{
	// Setting the child
	CUIElement* pChild;
	IFC(DoPointerCast(pChild, ppArgs->AsObject()));
	IFC(pViewbox->SetChild(pChild));
}
else if (cArgs == 1 && ppArgs->GetType() == valueNull)
{
	IFC(pViewbox->SetChild(NULL));
}
else
{
	IFC(E_INVALIDARG);
}
Cleanup:
RRETURN(hr);
	}

	/// <summary>
	/// Remove any existing child and set the new child tree.
	/// </summary>
	void SetChild(UIElement pChild)
{
	//HRESULT hr = S_OK;
	//CUIElement* pExistingLogicalChild = NULL;

	//var pExistingLogicalChild = GetChild();
	//if (null != pExistingLogicalChild)
	//{
	//	RemoveLogicalChild(pExistingLogicalChild);
	//}

	m_pContainerVisual.Child = pChild;
	//if (NULL != pChild)
	//{
	//	IFC(AddLogicalChild(pChild));
	//}

	//Cleanup:
	//ReleaseInterface(pExistingLogicalChild);
	//RRETURN(hr);
}

/// <summary>
/// This will return the first (and only) child, or NULL if the there is no Child yet.
/// </summary>
UIElement GetChild()
{
	//HRESULT hr = S_OK;

	//IFCPTR(ppChild);
	return m_pContainerVisual.Child;

	//Cleanup:
	//	RRETURN(hr);
}
#endif

	/// <summary>
	/// Returns the desired size for layout purposes.
	/// </summary>
	protected override Size MeasureOverride(Size availableSize)
	{
		Size childDesiredSize = default, scale = default, infiniteSize = default;

		//IFCEXPECT_RETURN(m_pContainerVisual);

		infiniteSize.Width = double.PositiveInfinity;
		infiniteSize.Height = double.PositiveInfinity;

		m_pContainerVisual.Measure(infiniteSize);
		//m_pContainerVisual.EnsureLayoutStorage();

		// Desired size would be my child's desired size plus the border
		childDesiredSize.Width = m_pContainerVisual.DesiredSize.Width;
		childDesiredSize.Height = m_pContainerVisual.DesiredSize.Height;

		scale = ComputeScaleFactor(availableSize, childDesiredSize);
		//IFCEXPECT_ASSERT_RETURN(!IsInfiniteF(scale.Width));
		//IFCEXPECT_ASSERT_RETURN(!IsInfiniteF(scale.Height));

		Size desiredSize = default;
		desiredSize.Width = scale.Width * childDesiredSize.Width;
		desiredSize.Height = scale.Height * childDesiredSize.Height;

		return desiredSize;
	}

	/// <summary>
	/// Returns the final render size for layout purposes.
	/// </summary>
	protected override Size ArrangeOverride(Size finalSize)
	{
		//IFCEXPECT(m_pContainerVisual);

		// Determine the scale factor given the final size
		//m_pContainerVisual.EnsureLayoutStorage();
		var desiredSize = m_pContainerVisual.DesiredSize;
		var scale = ComputeScaleFactor(finalSize, desiredSize);

		// Scale the ChildElement by the necessary factor
		m_pScaleTransform.ScaleX = scale.Width;
		m_pScaleTransform.ScaleY = scale.Height;

		// Position the Child to fill the Viewbox
		m_pContainerVisual.Arrange(new Rect(default, desiredSize));

		finalSize.Width = scale.Width * desiredSize.Width;
		finalSize.Height = scale.Height * desiredSize.Height;

		return finalSize;
	}
}
