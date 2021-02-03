/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerRemoveAttributeParams
{
	/* Pack=4 */
	public HtmlId : number;
	public Name : string;
	public static unmarshal(pData:number) : WindowManagerRemoveAttributeParams
	{
		const ret = new WindowManagerRemoveAttributeParams();
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
		}
		
		{
			const ptr = Module.getValue(pData + 4, "*");
			if(ptr !== 0)
			{
				ret.Name = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.Name = null;
			}
		}
		return ret;
	}
}
