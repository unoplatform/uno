using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DirectUI;
using Microsoft.UI.Xaml;

namespace DirectUI;

internal abstract class PropertyPathStepDescriptor
{
	public abstract PropertyPathStep CreateStep(
		PropertyPathListener pListener,
		bool fListenToChanges);
}

// src\dxaml\xcp\dxaml\lib\PropertyPathStepDescriptor.cpp
internal class SourceAccessPathStepDescriptor : PropertyPathStepDescriptor
{
	public override PropertyPathStep CreateStep(PropertyPathListener pListener, bool fListenToChanges)
	{
		SourceAccessPathStep spStep;
		spStep = new SourceAccessPathStep();
		spStep.Initialize(pListener);

		return spStep;
	}
}
internal class PropertyAccessPathStepDescriptor(string szName) : PropertyPathStepDescriptor
{
	private string m_szName = szName;

	public override PropertyPathStep CreateStep(PropertyPathListener pListener, bool fListenToChanges)
	{
		PropertyAccessPathStep spStep;
		spStep = new PropertyAccessPathStep();
		spStep.Initialize(pListener, m_szName, fListenToChanges);

		return spStep;
	}
}
internal class IntIndexerPathStepDescriptor(int nIndex) : PropertyPathStepDescriptor
{
	private int m_nIndex = nIndex;

	public override PropertyPathStep CreateStep(PropertyPathListener pListener, bool fListenToChanges)
	{
		IntIndexerPathStep spStep;
		spStep = new IntIndexerPathStep();
		spStep.Initialize(pListener, m_nIndex, fListenToChanges);

		return spStep;
	}
}
internal class StringIndexerPathStepDescriptor(string szIndex) : PropertyPathStepDescriptor
{
	private string m_szIndex = szIndex;

	public override PropertyPathStep CreateStep(PropertyPathListener pListener, bool fListenToChanges)
	{
		StringIndexerPathStep spStep;
		spStep = new StringIndexerPathStep();
		spStep.Initialize(pListener, m_szIndex, fListenToChanges);

		return spStep;
	}
}
internal class DependencyPropertyPathStepDescriptor(DependencyProperty pDP) : PropertyPathStepDescriptor
{
	private readonly DependencyProperty m_pDP = pDP;

	public override PropertyPathStep CreateStep(PropertyPathListener pListener, bool fListenToChanges)
	{
		PropertyAccessPathStep spStep;
		spStep = new PropertyAccessPathStep();
		spStep.Initialize(pListener, m_pDP, fListenToChanges);

		return spStep;
	}
}
