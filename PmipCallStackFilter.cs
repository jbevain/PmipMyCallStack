using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.VisualStudio.Debugger.CallStack;
using Microsoft.VisualStudio.Debugger.ComponentInterfaces;

namespace PmipMyCallStack
{
    public class PmipCallStackFilter : IDkmCallStackFilter
    {
        private static Range[] IPs;
        private static long previousFileLength;
        static FuzzyRangeComparer comparer = new FuzzyRangeComparer();

        public DkmStackWalkFrame[] FilterNextFrame(DkmStackContext stackContext, DkmStackWalkFrame input)
        {
            if (input == null) // after last frame
                return null;

            if (input.InstructionAddress == null) // error case
                return new[] { input };

            if (input.InstructionAddress.ModuleInstance != null && input.InstructionAddress.ModuleInstance.Module != null) // code in existing module
                return new[] { input };

            if (!stackContext.Thread.IsMainThread) // error case
                return new[] { input };


            return new[] { PmipStackFrame(stackContext, input) };
        }
        struct Range
        {
            public ulong Start;
            public ulong End;
            public string Name;
        }

        class FuzzyRangeComparer : IComparer<Range>
        {
            public int Compare(Range x, Range y)
            {
                if (x.Name == null && y.Start <= x.Start && y.End >= x.Start)
                {
                    return 0;
                }

                if (y.Name == null && x.Start <= y.Start && x.End >= y.Start)
                {
                    return 0;
                }

                return x.Start.CompareTo(y.Start);
            }
        }

        public static bool TryGetDescriptionForIP(ulong ip, out string name)
        {
            name = string.Empty;
            if (IPs == null)
                return false;
            int index = Array.BinarySearch(IPs, new Range() {Start = ip}, comparer);
            int linearIndex = -1;
            for (var i =0; i < IPs.Length; i++)
            {
                var item = IPs[i];
                if (ip > item.Start && ip < item.End)
                {
                    linearIndex = i;
                    break;
                }
            }


            if (linearIndex == -1)
            {
                if (index >= 0)
                    GC.KeepAlive(name);
                return false;
            }

            if (linearIndex != index)
                GC.KeepAlive(name);

            name = IPs[linearIndex].Name;
            return true;
        }

        public static void RefreshStackData(DkmStackWalkFrame frame)
        {
            try
            {
                var fileName = "D:\\jon.txt";
                var fileInfo = new FileInfo(fileName);
                if (fileInfo.Length == previousFileLength)
                    return;

                var list = new List<Range>(IPs?.Length * 2 ?? 1000);
                using (var inStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var file = new StreamReader(inStream))
                {
                    string line;
                    while ((line = file.ReadLine()) != null)
                    {
                        const char delemiter = ';';
                        string[] tokens = line.Split(delemiter);

                        //should never happen, but lets be safe and not get array out of bounds if it does
                        if (tokens.Length != 3)
                            continue;

                        string startip = tokens[0];
                        string endip = tokens[1];
                        string description = tokens[2];

                        var startipint = ulong.Parse(startip, NumberStyles.HexNumber);
                        var endipint = ulong.Parse(endip, NumberStyles.HexNumber);

                        list.Add(new Range() { Name = description, Start = startipint, End = endipint });
                    }
                }

                list.Sort((r1, r2) => r1.Start.CompareTo(r2.Start));
                IPs = list.ToArray();
                previousFileLength = fileInfo.Length;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to read dumped pmip file: " + ex.Message);
            }

        }

        public static DkmStackWalkFrame PmipStackFrame(DkmStackContext stackContext, DkmStackWalkFrame frame)
        {
            RefreshStackData(frame);
            string name = null;
            if (TryGetDescriptionForIP(frame.InstructionAddress.CPUInstructionPart.InstructionPointer, out name))
                return DkmStackWalkFrame.Create(
                    stackContext.Thread,
                    frame.InstructionAddress,
                    frame.FrameBase,
                    frame.FrameSize,
                    frame.Flags,
                    name,
                    frame.Registers,
                    frame.Annotations);

            return frame;
        }
    }
}