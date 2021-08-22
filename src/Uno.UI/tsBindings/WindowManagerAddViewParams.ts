/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerAddViewParams
{
	/* Pack=4 */
	public HtmlId : string;
	public ChildView : string;
	public Index : number;
	public static unmarshal(pData:number) : WindowManagerAddViewParams
	{
		const ret = new WindowManagerAddViewParams();
		
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
		
		{
			ret.Index = Number(Module.getValue(pData + 8, "i32"));
		}
		return ret;
	}
}
