using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.Background
{
	public partial class SystemCondition : IBackgroundCondition
	{
		public SystemConditionType ConditionType { get; }
		public SystemCondition(SystemConditionType conditionType)
		{
			ConditionType = conditionType;
		}
	}
}
