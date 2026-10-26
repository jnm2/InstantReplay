#if NET35
using System;
using NUnitLite;

if (Environment.Version.Major != 2)
{
    Console.Error.WriteLine("The net35 tests must run on CLR 2.0 (.NET Framework 3.5), not " + Environment.Version + ".");
    return 1;
}

return new AutoRun(typeof(Program).Assembly).Execute(args);
#endif
