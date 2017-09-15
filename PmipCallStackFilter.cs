using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.CallStack;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;
using Microsoft.VisualStudio.Debugger.Evaluation;

namespace PmipMyCallStack
{
    public class PmipCallStackFilter : IDkmCallStackFilter
    {
        Guid ContextBeingWalked = new Guid();
		public DkmStackWalkFrame[] FilterNextFrame(DkmStackContext stackContext, DkmStackWalkFrame input)
		{
            if (input == null) // after last frame
                return null;

			if (input.InstructionAddress == null) // error case
				return new[] { input };

			if (input.InstructionAddress.ModuleInstance != null && input.InstructionAddress.ModuleInstance.Module != null) // code in existing module
				return new[] { input };

            PmipFunctionDataItem pmipPrettyDump;
		    if (!TryGetPmipPrettyDumpFunction(stackContext, input, out pmipPrettyDump))
		        return new[] { input };

            if (stackContext.UniqueId != ContextBeingWalked)
            {
                ContextBeingWalked = stackContext.UniqueId;
                pmipPrettyDump.RefreshStackData(input);
            }

		    return new[] { pmipPrettyDump.PmipStackFrame(input) };
		}

        private bool TryGetPmipPrettyDumpFunction(DkmStackContext stackContext, DkmStackWalkFrame frame, out PmipFunctionDataItem pmipFunction)
        {
            pmipFunction = stackContext.GetDataItem<PmipFunctionDataItem>();
            if (pmipFunction != null)
                return true;

            var pmipPrettyDump = PmipUtils.FindInstructionAddress("mono_dump_pmip_pretty", frame);

            if (pmipPrettyDump == null)
                return false;

            pmipFunction = new PmipFunctionDataItem(stackContext, frame, "0x" + pmipPrettyDump.CPUInstructionPart.InstructionPointer.ToString("X"));
            stackContext.SetDataItem(DkmDataCreationDisposition.CreateAlways, pmipFunction);

            return true;
        }
    }
}