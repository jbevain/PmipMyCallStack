using System;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.CallStack;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Native;

namespace PmipMyCallStack
{
	class PmipRunner
	{
		private static readonly DkmLanguage CppLanguage = DkmLanguage.Create("C++", new DkmCompilerId(DkmVendorId.Microsoft, DkmLanguageId.Cpp));

		private readonly DkmStackContext _stackContext;
		private readonly DkmStackWalkFrame _frame;
		private readonly DkmInspectionContext _inspectionContext;

		public PmipRunner(DkmStackContext stackContext, DkmStackWalkFrame frame)
		{
			_stackContext = stackContext;
			_frame = frame;
			_inspectionContext = CreateInspectionContext(stackContext, frame);
		}

		public DkmStackWalkFrame PmipStackFrame()
		{
			PmipFunctionDataItem pmipFunction;
			if (!TryGetPmipFunction(out pmipFunction))
				return _frame;

			var ip = $"0x{_frame.InstructionAddress.CPUInstructionPart.InstructionPointer:X}";
			var call = $"((char*(*)(void*)){pmipFunction.PmipFunction})((void*){ip})";

			var result = "<ERROR>";
			var isNull = true;

			var eval = EvaluateExpression(call, r =>
			{
				isNull = r.Address.InstructionAddress.CPUInstructionPart.InstructionPointer == 0;
				if (r.Value != null)
				{
					try
					{
						int afterBeginQuote = r.Value.IndexOf("\"", StringComparison.Ordinal) + 1;
						int beforeEndQuote = r.Value.Length - 1;
						result = r.Value.Substring(afterBeginQuote, beforeEndQuote - afterBeginQuote);
					}
					catch (Exception)
					{
					}

				}

			});

			if (!eval || isNull)
				return _frame; 
			
			return DkmStackWalkFrame.Create(
				_stackContext.Thread,
				_frame.InstructionAddress,
				_frame.FrameBase,
				_frame.FrameSize,
				_frame.Flags,
				result,
				_frame.Registers,
				_frame.Annotations);
		}

		private DkmNativeInstructionAddress FindInstructionAddress(string functionName)
		{
			foreach (var module in this._frame.RuntimeInstance.GetModuleInstances())
			{
				var address = (module as DkmNativeModuleInstance)?.FindExportName(functionName,
					IgnoreDataExports: true);
				if(address != null)
					return address;
			}
			return null;
		}

		private bool TryGetPmipFunction(out PmipFunctionDataItem pmipFunction)
		{
			pmipFunction = _stackContext.GetDataItem<PmipFunctionDataItem>();
			if (pmipFunction != null)
				return true;

			var pmipOrig = FindInstructionAddress("mono_pmip");
			var pmipPretty = FindInstructionAddress("mono_pmip_pretty");

			var pmipFnToUse = pmipPretty ?? pmipOrig;

			if (pmipFnToUse == null)
				return false;

			var item = new PmipFunctionDataItem { PmipFunction = "0x" + pmipFnToUse.CPUInstructionPart.InstructionPointer.ToString("X") };
			pmipFunction = item;
			_stackContext.SetDataItem(DkmDataCreationDisposition.CreateAlways, item);

			return true;
		}

		private static DkmLanguageExpression CppExpression(string expression)
		{
			return DkmLanguageExpression.Create(CppLanguage, DkmEvaluationFlags.None, expression, null);
		}

		private static DkmInspectionContext CreateInspectionContext(DkmStackContext stackContext, DkmStackWalkFrame frame)
		{
			return DkmInspectionContext.Create(
				stackContext.InspectionSession,
				frame.RuntimeInstance,
				frame.Thread,
				1000,
				DkmEvaluationFlags.None,
				DkmFuncEvalFlags.None,
				10,
				CppLanguage,
				null);
		}

		private bool EvaluateExpression(string expression, Action<DkmSuccessEvaluationResult> onSuccess)
		{
			var workList = DkmWorkList.Create(null);
			var success = false;

			_inspectionContext.EvaluateExpression(workList, CppExpression(expression), _frame, res =>
			{
				var resObj = res.ResultObject;
				var result = resObj as DkmSuccessEvaluationResult;
				if (result != null)
				{
					success = true;
					onSuccess(result);
				}

				resObj.Close();
			});

			workList.Execute();
			return success;
		}
	}
}
