/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
namespace Windows.Storage
{
	export class ApplicationDataContainer_GetCountReturn
	{
		/* Pack=4 */
		public Count : number;
		public marshal(pData:number)
		{
			Module.setValue(pData + 0, this.Count, "i32");
		}
	}
}
