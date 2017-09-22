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
        private static Range[] _rangesSortedByIp;
        private static long _previousFileLength;
        private static FuzzyRangeComparer _comparer = new FuzzyRangeComparer();

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

        public static DkmStackWalkFrame PmipStackFrame(DkmStackContext stackContext, DkmStackWalkFrame frame)
        {
            var fileName = Path.Combine(Path.GetTempPath(), "pmip." + frame.Process.LivePart.Id);
            RefreshStackData(fileName);
            string name = null;
            if (TryGetDescriptionForIp(frame.InstructionAddress.CPUInstructionPart.InstructionPointer, out name))
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

        public static void RefreshStackData(string fileName)
        {
            try
            {
                if (!File.Exists(fileName))
                    return;

                var fileInfo = new FileInfo(fileName);
                if (fileInfo.Length == _previousFileLength)
                    return;

                var list = new List<Range>(10000);
                using (var inStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var file = new StreamReader(inStream))
                    {
                        string line;
                        while ((line = file.ReadLine()) != null)
                        {
                            const char delemiter = ';';
                            var tokens = line.Split(delemiter);

                            //should never happen, but lets be safe and not get array out of bounds if it does
                            if (tokens.Length != 3)
                                continue;

                            var startip = tokens[0];
                            var endip = tokens[1];
                            var description = tokens[2];

                            var startiplong = ulong.Parse(startip, NumberStyles.HexNumber);
                            var endipint = ulong.Parse(endip, NumberStyles.HexNumber);

                            list.Add(new Range() { Name = description, Start = startiplong, End = endipint });
                        }
                    }
                }

                list.Sort((r1, r2) => r1.Start.CompareTo(r2.Start));
                _rangesSortedByIp = list.ToArray();
                _previousFileLength = fileInfo.Length;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to read dumped pmip file: " + ex.Message);
            }

        }

        public static bool TryGetDescriptionForIp(ulong ip, out string name)
        {
            name = string.Empty;

            if (_rangesSortedByIp == null)
                return false;

            var rangeToFindIp = new Range() { Start = ip };
            var index = Array.BinarySearch(_rangesSortedByIp, rangeToFindIp, _comparer);

            if (index < 0)
                return false;

            name = _rangesSortedByIp[index].Name;

            return true;
        }
    }
}