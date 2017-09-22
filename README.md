Pmip My Call Stack
=====

PmipMyCallStack is a Visual Studio 2015 extension to help debug native applications embedding Mono, like Unity.

Mono doesn't generate debug symbols that Visual Studio understands for jitted functions.

As a result, Visual Studio can not show anything meaningful for managed stack frames.

This fork of PmipMyCallstack has been developed for developers at unity-technologies and support has been added to our clone of mono, which can be found here https://github.com/Unity-Technologies/mono

This version requires you to set the MONO_PMIP environment variable before launching Unity or using our mono, which will tell the mono runtime to write out each jit'd function to a file.

The original PmipMyCallstack would call the function `mono_pmip` on every frame that doesn't belong to a module to show a meaningful representation of the stack frame and display it natively in the call stack window.  This version of PmipMyCallstack does the same thing, but instead of calling over inter process communication, mono writes a file with the jit information and we lookup the IP from that. This allows us to open much larger callstacks, and many frames without visual studio hanging.

In this version we also display the module the managed code belongs to, and the file and linenumber information if mono has loaded debug symbols. This gives you a nice callstack to look at in visual studio.

## Before

![Before Pmip My Call Stack](https://raw.githubusercontent.com/mderoy/PmipMyCallStack/master/Images/csb.png)

## After

![After Pmip My Call Stack](https://raw.githubusercontent.com/mderoy/PmipMyCallStack/master/Images/cs.png)

