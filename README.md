Pmip My Call Stack
=====

PmipMyCallStack is a Visual Studio 2015 extension to help debug native applications embedding Mono.

Mono doesn't generate debug symbols that Visual Studio understands for jitted functions.

As a result, Visual Studio can not show anything meaningful for managed stack frames.

PmipMyCallStack is calling the function `mono_pmip` on every frame that doesn't belong to a module to show a meaningful representation of the stack frame and displays it natively in the call stack window.

![preview](https://raw.githubusercontent.com/jbevain/PmipMyCallStack/master/Images/cs.png)

