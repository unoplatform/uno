/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerMeasureViewParams
{
	/* Pack=8 */
	public HtmlId : string;
	public AvailableWidth : number;
	public AvailableHeight : number;
	public static unmarshal(pData:number) : WindowManagerMeasureViewParams
	{
		const ret = new WindowManagerMeasureViewParams();
		
		{
			const ptr = Module.getValue(pData + 0, "*");
			if(ptr !== 0)
			{
				ret.HtmlId = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.HtmlId = null;
			}
		}
		
		{
			ret.AvailableWidth = Number(Module.getValue(pData + 8, "double"));
		}
		
		{
			ret.AvailableHeight = Number(Module.getValue(pData + 16, "double"));
		}
		return ret;
	}
}
