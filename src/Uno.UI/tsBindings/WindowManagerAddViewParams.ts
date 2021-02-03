/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerAddViewParams
{
	/* Pack=4 */
	public HtmlId : number;
	public ChildView : number;
	public Index : number;
	public static unmarshal(pData:number) : WindowManagerAddViewParams
	{
		const ret = new WindowManagerAddViewParams();
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
		}
		
		{
			ret.ChildView = Number(Module.getValue(pData + 4, "*"));
		}
		
		{
			ret.Index = Number(Module.getValue(pData + 8, "i32"));
		}
		return ret;
	}
}
