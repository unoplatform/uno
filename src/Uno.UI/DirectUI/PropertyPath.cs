using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace DirectUI;

internal interface IPropertyPathListener { }

/// <summary>
/// This interface is used by property path listener hosts
/// </summary>
internal interface IPropertyPathListenerHost // from: src\dxaml\xcp\dxaml\lib\JoltClasses.h
{
	void SourceChanged();
	string GetTraceString();
}

partial class PropertyPathListener // src\dxaml\xcp\dxaml\lib\PropertyPath.h
{
	// public:
	//public void Initialize(
	//	IPropertyPathListenerHost pOwner,
	//	PropertyPathParser pPropertyPathParser,
	//	bool fListenToChanges,
	//	bool fUseWeakReferenceForSource)
	//{
	//}

	//public void SetSource(object pSource) => throw new NotImplementedException();

	//public void SetValue(object pValue) => throw new NotImplementedException();
	//public object GetValue() => throw new NotImplementedException();

	//public bool FullPathExists();

	//public Type GetLeafType();

	// Tracing support
	//public string GetTraceString();

	//public PropertyPathStep DebugGetFirstStep();

	//public void ClearOwner();

	// private:
	//private void ConnectPathStep(PropertyPathStep pStep, object pSource) => throw new NotImplementedException();
	//private void PropertyPathStepChanged(PropertyPathStep pStep) => throw new NotImplementedException();
	//private void AppendStep(PropertyPathStep pStep) => throw new NotImplementedException();

	// private
	private IPropertyPathListenerHost m_pOwner;
	private PropertyPathStep m_tpFirst;
	private PropertyPathStep m_tpLast;
}
partial class PropertyPathListener // src\dxaml\xcp\dxaml\lib\PropertyPath.cpp
{
	public PropertyPathListener()
	{
		m_pOwner = null;
	}

	private void AppendStep(PropertyPathStep pStep)
	{
		if (m_tpFirst is null)
		{
			m_tpFirst = pStep;
			m_tpLast = pStep;
		}
		else
		{
			m_tpLast.SetNext(pStep);
			m_tpLast = pStep;
		}
	}

	public void Initialize(
		IPropertyPathListenerHost pOwner,
		PropertyPathParser pPropertyPathParser,
		bool fListenToChanges,
		bool fUseWeakReferenceForSource)
	{
		m_pOwner = pOwner;

		foreach (var itrDescriptor in pPropertyPathParser.Descriptors)
		{
			var spStep = itrDescriptor.CreateStep(this, fListenToChanges);
			AppendStep(spStep);
		}
	}

	public void SetSource(object pSource)
	{
		ConnectPathStep(m_tpFirst, pSource);
	}

	private void ConnectPathStep(PropertyPathStep pStep, object pSource)
	{
		object spCurrentObject;
		PropertyPathStep spCurrentStep = pStep;
		object spValue;

		spCurrentObject = pSource;

		while (spCurrentStep is { })
		{
			// Connext the current step to the source, and start going down from there
			spCurrentStep.ReConnect(spCurrentObject);

			// Only get the value if there's a next step 
			// that is going to be connected to it
			if (spCurrentStep.GetNextStep() is { })
			{
				// Now we get the next value in the chain
				spValue = spCurrentStep.GetValue();

				// Get the value for the next step, we do not care what it is
				// the path will deal with it
				spCurrentObject = spValue;
			}

			// Move to the next path
			spCurrentStep = spCurrentStep.GetNextStep();
		}
	}

	public object GetValue()
	{
		return m_tpLast?.GetValue();
	}

	public void SetValue(object pValue)
	{
		if (m_tpLast is { })
		{
			m_tpLast.SetValue(pValue);
		}
	}

	public Type GetLeafType()
	{
		return m_tpLast?.GetType();
	}

	// Tracing support
	public string GetTraceString()
	{
		if (m_pOwner is { })
		{
			return m_pOwner.GetTraceString();
		}
		else
		{
			return string.Empty;
		}
	}

	// private, but friend to PropertyPathStep
	internal void PropertyPathStepChanged(PropertyPathStep pStep)
	{
		object spValue;

		// We only need to reconnect if the step that changed is not
		// the last step. If it is the last step then we will just
		// get its value later on.
		if (m_tpLast != pStep)
		{
			// Get the current value of the step that changed
			spValue = pStep.GetValue();

			// Connect the step starting with the next step to the one that changed
			ConnectPathStep(pStep.GetNextStep(), spValue);
		}

		// Notify the expression that the source has changed
		m_pOwner.SourceChanged();
	}

	// This may be called when the owner is about to be destroyed, giving a chance to
	// clear out m_pOwner to make sure there aren't any further attempts to use it.
	public void ClearOwner()
	{
		m_pOwner = null;
	}

	public bool FullPathExists()
	{
		return m_tpLast is { } && m_tpLast.IsConnected();
	}

	public PropertyPathStep DebugGetFirstStep()
	{
		return m_tpFirst;
	}
}
partial class PropertyPathListener : IDisposable
{
	public void Dispose()
	{
		var previousStep = default(PropertyPathStep);
		for (var step = m_tpFirst; step is { }; step = step.GetNextStep())
		{
			step.Dispose();

			previousStep?.SetNext(null);
			previousStep = step;
		}

		ClearOwner();
	}
}
