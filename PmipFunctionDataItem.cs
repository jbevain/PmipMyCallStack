using Microsoft.VisualStudio.Debugger;

namespace PmipMyCallStack
{
	class PmipFunctionDataItem : DkmDataItem
	{
		public string PmipFunction { get; set; }	
	}
}