/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerRemoveViewParams
{
	/* Pack=4 */
	public HtmlId : string;
	public ChildView : string;
	public static unmarshal(pData:number) : WindowManagerRemoveViewParams
	{
		const ret = new WindowManagerRemoveViewParams();
		
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
			const ptr = Module.getValue(pData + 4, "*");
			if(ptr !== 0)
			{
				ret.ChildView = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.ChildView = null;
			}
		}
		return ret;
	}
}
