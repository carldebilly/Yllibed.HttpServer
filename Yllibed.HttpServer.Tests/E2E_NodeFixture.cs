using System.Diagnostics;

namespace Yllibed.HttpServer.Tests;

[TestClass]
public sealed class E2E_NodeFixture : FixtureBase
{
	[TestMethod]
	public void E2E_NodeScript_ShouldSucceed()
	{
		// Verify that Node.js is available (otherwise mark the test as Inconclusive)
		if (!IsCommandAvailable("node"))
		{
			Assert.Inconclusive("Node.js (node) is not available on PATH. Install Node.js to run this E2E test.");
		}

		var solutionRoot = GetSolutionRoot();
		// Script now located under the test project folder
		var scriptPath = Path.Combine(solutionRoot, "Yllibed.HttpServer.Tests", "e2e", "run-e2e.js");
		if (!File.Exists(scriptPath))
		{
			// Fallback: when tests run from within the test project root already
			scriptPath = Path.Combine(solutionRoot, "e2e", "run-e2e.js");
		}
		File.Exists(scriptPath).Should().BeTrue("E2E script must exist at {0}", scriptPath);

		var psi = new ProcessStartInfo
		{
			FileName = "node",
			Arguments = scriptPath,
			WorkingDirectory = Path.GetDirectoryName(scriptPath)!,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true,
		};

		using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
		var stdout = new System.Text.StringBuilder();
		var stderr = new System.Text.StringBuilder();
		proc.OutputDataReceived += (_, e) => { if (e.Data is not null) stdout.AppendLine(e.Data); };
		proc.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderr.AppendLine(e.Data); };

		proc.Start();
		proc.BeginOutputReadLine();
		proc.BeginErrorReadLine();

		// Give a generous timeout (same order as the script, which times out after ~10s)
		var exited = proc.WaitForExit(30000);
		if (!exited)
		{
			try { proc.Kill(entireProcessTree: true); } catch (Exception) { /* ignore */ }
			Assert.Fail("E2E Node script did not exit within timeout.\nSTDOUT:\n{0}\nSTDERR:\n{1}", stdout.ToString(), stderr.ToString());
		}

		// Ensure we read all remaining output
		proc.WaitForExit();

		if (proc.ExitCode != 0)
		{
			Assert.Fail("E2E Node script failed with exit code {0}.\nSTDOUT:\n{1}\nSTDERR:\n{2}", proc.ExitCode, stdout.ToString(), stderr.ToString());
		}
	}

	private static bool IsCommandAvailable(string command)
	{
		try
		{
			var psi = new ProcessStartInfo
			{
				FileName = command,
				Arguments = "--version",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
			};
			using var p = Process.Start(psi);
			if (p is null) return false;
			p.WaitForExit(5000);
			return p.ExitCode == 0;
		}
		catch
		{
			return false;
		}
	}

	private static string GetSolutionRoot()
	{
		// Heuristic: tests usually run with a WorkingDirectory under the test project's bin folder.
		// Walk up until we find the solution file.
		var dir = new DirectoryInfo(Environment.CurrentDirectory);
		for (int i = 0; i < 10 && dir is not null; i++, dir = dir.Parent!)
		{
			if (File.Exists(Path.Combine(dir.FullName, "Yllibed.HttpServer.slnx")))
			{
				return dir.FullName;
			}
		}
		// Fallback: base directory if available
		return AppContext.BaseDirectory; // best effort
	}
}
