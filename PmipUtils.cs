using System;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.CallStack;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Native;

namespace PmipMyCallStack
{
    public class PmipUtils
    {

        public static DkmNativeInstructionAddress FindInstructionAddress(string functionName, DkmStackWalkFrame frame)
        {
            foreach (var module in frame.RuntimeInstance.GetModuleInstances())
            {
                var address = (module as DkmNativeModuleInstance)?.FindExportName(functionName,
                    IgnoreDataExports: true);
                if (address != null)
                    return address;
            }
            return null;
        }

        public static readonly DkmLanguage CppLanguage = DkmLanguage.Create("C++", new DkmCompilerId(DkmVendorId.Microsoft, DkmLanguageId.Cpp));
        private static DkmLanguageExpression CppExpression(string expression)
        {
            return DkmLanguageExpression.Create(CppLanguage, DkmEvaluationFlags.None, expression, null);
        }
        public static bool EvaluateExpression(DkmInspectionContext inspectionContext, DkmStackWalkFrame frame, string expression, Action<DkmSuccessEvaluationResult> onSuccess)
        {
            var workList = DkmWorkList.Create(null);
            var success = false;

            inspectionContext.EvaluateExpression(workList, CppExpression(expression), frame, res =>
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

        public static DkmInspectionContext CreateInspectionContext(DkmStackContext stackContext, DkmStackWalkFrame frame)
        {
            return DkmInspectionContext.Create(
                stackContext.InspectionSession,
                frame.RuntimeInstance,
                frame.Thread,
                1000,
                DkmEvaluationFlags.None,
                DkmFuncEvalFlags.None,
                10,
                PmipUtils.CppLanguage,
                null);
        }
    }
}