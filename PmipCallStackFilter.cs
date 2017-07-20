using Microsoft.VisualStudio.Debugger.CallStack;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;

namespace PmipMyCallStack
{
    public class PmipCallStackFilter : IDkmCallStackFilter
    {

		public DkmStackWalkFrame[] FilterNextFrame(DkmStackContext stackContext, DkmStackWalkFrame input)
		{
			if (input == null) // after last frame
				return null;

			if (input.InstructionAddress == null) // error case
				return new[] { input };

			if (input.InstructionAddress.ModuleInstance != null && input.InstructionAddress.ModuleInstance.Module != null) // code in existing module
				return new[] { input };

			var runner = new PmipRunner(stackContext, input);
			return new[] { runner.PmipStackFrame() };
		}
	}
}