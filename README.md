# TinyJitHook
JIT Hook for .NET 2.0-4.6+ with x86 and x64 support.


# Usage
For .NET 4.0+ support you must compile the project **with** the conditional compilation symbol "NET4" as well as change the target framework.
For .NET 2.0-3.5 support you must compile the project **without** the symbol as well as change the target framework.


# SJITHook
A lot of the structures have been changed from the original SJITHook presented quite some time ago.
These changes were made to improve the original (and be more of a reflection of the present JIT implementation). 
If you are going to use the structures presented in this project; please note that they will change, and may not be entirely correct.
I have changed them to work for test programs, not for everything out there in this world.


# Credits
* yck - ConfuserEx - Anti-Tamper JIT and Confuser 1.9 Anti-Tamper JIT x64 .NET 2.0
* RebelDotNet - Good papers on JIT
* .NET Core CLR