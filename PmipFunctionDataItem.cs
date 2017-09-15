using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.CallStack;
using Microsoft.VisualStudio.Debugger.Evaluation;

namespace PmipMyCallStack
{
	class PmipFunctionDataItem : DkmDataItem
	{
	    private string m_pmipFunctionString;
        private readonly DkmStackContext m_stackContext;
        private SortedList<int, string> MapIPsToDescriptions = new SortedList<int, string>(5000);

	    public PmipFunctionDataItem(DkmStackContext ctx, DkmStackWalkFrame mFrame, string mPmipFunctionString)
	    {
	        m_stackContext = ctx;
	        m_pmipFunctionString = mPmipFunctionString;
	    }

        public string GetDescriptionForIP(string ip)
        {
            int ipKey = int.Parse(ip, NumberStyles.HexNumber);
            IList<int> ipKeys = MapIPsToDescriptions.Keys;

            int left = 0;
            int right = ipKeys.Count;
            while (right > left)
            {
                int pivot = (left + right) / 2;
                if (ipKeys[pivot] <= ipKey)
                    right = pivot;
                else
                    left = pivot + 1;
            }
            int keyOfFirstIdxGreaterThanOrEqualToIp = ipKeys[right];
            return MapIPsToDescriptions[keyOfFirstIdxGreaterThanOrEqualToIp];
        }

        public void RefreshStackData(DkmStackWalkFrame frame)
	    {
            var call = $"((char*(*)()){m_pmipFunctionString})()";
	        var stackTraceFile = "";
            var eval = PmipUtils.EvaluateExpression(PmipUtils.CreateInspectionContext(m_stackContext, frame), frame, call, r =>
            {
                stackTraceFile = r.Value;
            });

	        try
	        {
	            using (StreamReader file = new StreamReader(stackTraceFile))
	            {
	                HashSet<string> visitedFunctions = new HashSet<string>();
	                string line;
	                while ((line = file.ReadLine()) != null)
	                {
	                    const char delemiter = ';';
	                    string[] tokens = line.Split(delemiter);

	                    //should never happen, but lets be safe and not get array out of bounds if it does
	                    if (tokens.Length != 3)
	                        continue;

	                    string startip = "0x" + tokens[0];
	                    string endip = "0x" + tokens[1];
	                    string description = tokens[2];

	                    //if we've already indexed this description, continue
	                    if (visitedFunctions.Contains(description))
	                        continue;
	                    visitedFunctions.Add(description);

	                    int startipint = int.Parse(startip, NumberStyles.HexNumber);
	                    int endipint = int.Parse(startip, NumberStyles.HexNumber);

	                    MapIPsToDescriptions.Add(startipint, description);
	                    MapIPsToDescriptions.Add(endipint, description);
	                }
	            }
	        }
	        catch (Exception ex)
	        {
	            Console.WriteLine("Unable to read dumped pmip file: " + ex.Message);
	        }

	    }

	    public DkmStackWalkFrame PmipStackFrame(DkmStackWalkFrame frame)
	    {
            var ip = $"0x{frame.InstructionAddress.CPUInstructionPart.InstructionPointer:X}";

            return DkmStackWalkFrame.Create(
                m_stackContext.Thread,
                frame.InstructionAddress,
                frame.FrameBase,
                frame.FrameSize,
                frame.Flags,
                GetDescriptionForIP(ip),
                frame.Registers,
                frame.Annotations);
	    }

    }
}