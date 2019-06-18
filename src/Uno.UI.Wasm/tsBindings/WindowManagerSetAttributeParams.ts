/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetAttributeParams
{
	/* Pack=4 */
	HtmlId : number;
	Name : string;
	Value : string;
	public static unmarshal(pData:number) : WindowManagerSetAttributeParams
	{
		let ret = new WindowManagerSetAttributeParams();
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
		}
		
		{
			var ptr = Module.getValue(pData + 4, "*");
			if(ptr !== 0)
			{
				ret.Name = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.Name = null;
			}
		}
		
		{
			var ptr = Module.getValue(pData + 8, "*");
			if(ptr !== 0)
			{
				ret.Value = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.Value = null;
			}
		}
		return ret;
	}
}
