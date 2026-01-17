using System.Diagnostics;

namespace CoverageChecker.Services;

internal class SystemProcess : ISystemProcess
{
    private readonly Process _process = new();

    public ProcessStartInfo StartInfo => _process.StartInfo;
    public StreamReader StandardOutput => _process.StandardOutput;
    public StreamReader StandardError => _process.StandardError;
    public int ExitCode => _process.ExitCode;

    public bool Start() => _process.Start();
    public bool WaitForExit(int milliseconds) => _process.WaitForExit(milliseconds);
    public void Kill() => _process.Kill();

    public void Dispose() => _process.Dispose();
}
