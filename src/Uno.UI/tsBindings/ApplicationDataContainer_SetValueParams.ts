/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class ApplicationDataContainer_SetValueParams
{
	/* Pack=4 */
	public Key : string;
	public Value : string;
	public Locality : string;
	public static unmarshal(pData:number) : ApplicationDataContainer_SetValueParams
	{
		const ret = new ApplicationDataContainer_SetValueParams();
		
		{
			const ptr = Module.getValue(pData + 0, "*");
			if(ptr !== 0)
			{
				ret.Key = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.Key = null;
			}
		}
		
		{
			const ptr = Module.getValue(pData + 4, "*");
			if(ptr !== 0)
			{
				ret.Value = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.Value = null;
			}
		}
		
		{
			const ptr = Module.getValue(pData + 8, "*");
			if(ptr !== 0)
			{
				ret.Locality = String(Module.UTF8ToString(ptr));
			}
			else
			
			{
				ret.Locality = null;
			}
		}
		return ret;
	}
}
