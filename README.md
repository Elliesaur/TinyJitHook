# TinyJitHook
JIT Hook for:
* .NET 2.0-4.6+ with x86 and x64 support.
* .NET 2.0-3.5 does not support exception handlers.
* .NET Core x.x-2.1 (tested) with x86 and x64 support for Windows devices only (WinAPI used).

The main purpose of this JIT Hook is to demonstrate how someone can change method code at runtime.
Unlike some other libraries out there, this does not edit native code after the original compile method has run. 
It edits the IL code before it is sent to be compiled.

There are a few helper classes to create IL code, GetBytes/GetInstructions extension methods will allow you to read instructions, 
edit then write them to bytes. The overhead created by libraries such as dnlib is too great, the idea was to keep it tiny and simple.

An example: a program has a serial key that users must enter, but decompilation can reveal the serial key's value or algorithm that checks the serial.
Editing the check method through JIT can change the way the check happens without showing the real IL code to decompilers.

Of course, the more famous example would be anti-tamper where method bodies are restored as needed through a JIT hook.


# Usage
For .NET 4.0+ support you must compile the project **with** the conditional compilation symbol "NET4" as well as change the target framework.

For .NET 2.0-3.5 support you must compile the project **without** the symbol as well as change the target framework.

For .NET Core support you must compile the project **with** the conditional compilation symbol "NET4". Tested with .NET Core 2.0 and 2.1.


# SJITHook
A lot of the structures have been changed from the original SJITHook presented quite some time ago.
These changes were made to improve the original (and be more of a reflection of the present JIT implementation). 
If you are going to use the structures presented in this project; please note that they will change, and may not be entirely correct.
I have changed them to work for test programs, not for everything out there in this world.


# Credits
* yck - ConfuserEx - Anti-Tamper JIT and Confuser 1.9 Anti-Tamper JIT x64 .NET 2.0
* RebelDotNet - Good papers on JIT
* .NET Core CLR
* 0xd4d - Extra section reader
