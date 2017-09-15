using System;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.CallStack;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Native;

namespace PmipMyCallStack
{
	//class PmipRunner
	//{
	//	private readonly DkmStackContext _stackContext;
	//	private readonly DkmStackWalkFrame _frame;
	//	private readonly DkmInspectionContext _inspectionContext;

	//	public PmipRunner(DkmStackContext stackContext, DkmStackWalkFrame frame)
	//	{
	//		_stackContext = stackContext;
	//		_frame = frame;
	//		_inspectionContext = PmipUtils.CreateInspectionContext(stackContext, frame);
	//	}

	//	public DkmStackWalkFrame PmipStackFrame()
	//	{
	//		PmipFunctionDataItem pmipFunction;
	//		if (!TryGetPmipFunction(out pmipFunction))
	//			return _frame;

	//		var ip = $"0x{_frame.InstructionAddress.CPUInstructionPart.InstructionPointer:X}";
	//		var call = $"((char*(*)(void*)){pmipFunction.PmipFunction})((void*){ip})";

	//		var result = "<ERROR>";
	//		var isNull = true;

	//		var eval = PmipUtils.EvaluateExpression(_inspectionContext, _frame ,call, r =>
	//		{
	//			isNull = r.Address.InstructionAddress.CPUInstructionPart.InstructionPointer == 0;
	//		    result = r.Value;
	//		});

	//		if (!eval || isNull)
	//			return _frame; 
			
	//		return DkmStackWalkFrame.Create(
	//			_stackContext.Thread,
	//			_frame.InstructionAddress,
	//			_frame.FrameBase,
	//			_frame.FrameSize,
	//			_frame.Flags,
	//			result,
	//			_frame.Registers,
	//			_frame.Annotations);
	//	}


	//}
}
