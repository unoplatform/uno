/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class WindowManagerSetUnsetClassesParams
{
	/* Pack=4 */
	public HtmlId : number;
	public CssClassesToSet_Length : number;
	public CssClassesToSet : Array<string>;
	public CssClassesToUnset_Length : number;
	public CssClassesToUnset : Array<string>;
	public static unmarshal(pData:number) : WindowManagerSetUnsetClassesParams
	{
		const ret = new WindowManagerSetUnsetClassesParams();
		
		{
			ret.HtmlId = Number(Module.getValue(pData + 0, "*"));
		}
		
		{
			ret.CssClassesToSet_Length = Number(Module.getValue(pData + 4, "i32"));
		}
		
		{
			const pArray = Module.getValue(pData + 8, "*");
			if(pArray !== 0)
			{
				ret.CssClassesToSet = new Array<string>();
				for(var i=0; i<ret.CssClassesToSet_Length; i++)
				{
					const value = Module.getValue(pArray + i * 4, "*");
					if(value !== 0)
					{
						ret.CssClassesToSet.push(String(MonoRuntime.conv_string(value)));
					}
					else
					
					{
						ret.CssClassesToSet.push(null);
					}
				}
			}
			else
			
			{
				ret.CssClassesToSet = null;
			}
		}
		
		{
			ret.CssClassesToUnset_Length = Number(Module.getValue(pData + 12, "i32"));
		}
		
		{
			const pArray = Module.getValue(pData + 16, "*");
			if(pArray !== 0)
			{
				ret.CssClassesToUnset = new Array<string>();
				for(var i=0; i<ret.CssClassesToUnset_Length; i++)
				{
					const value = Module.getValue(pArray + i * 4, "*");
					if(value !== 0)
					{
						ret.CssClassesToUnset.push(String(MonoRuntime.conv_string(value)));
					}
					else
					
					{
						ret.CssClassesToUnset.push(null);
					}
				}
			}
			else
			
			{
				ret.CssClassesToUnset = null;
			}
		}
		return ret;
	}
}
