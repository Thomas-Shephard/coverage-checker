using System.Diagnostics;

namespace CoverageChecker.Services;

internal interface ISystemProcess : IDisposable
{
    ProcessStartInfo StartInfo { get; }
    StreamReader StandardOutput { get; }
    StreamReader StandardError { get; }
    int ExitCode { get; }
    bool Start();
    bool WaitForExit(int milliseconds);
    void Kill();
}
