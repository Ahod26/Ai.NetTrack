using System.Diagnostics;

namespace backend.Background;

public class SeqDockerService(
  ILogger<SeqDockerService> logger,
  IHostEnvironment environment) : IHostedService
{
  private const string ContainerName = "seq";
  private const string SeqPort = "5341";

  public async Task StartAsync(CancellationToken cancellationToken)
  {
    if (!environment.IsDevelopment())
      return;

    try
    {
      var containerExists = await RunDockerCommand($"ps -a --filter name={ContainerName} --format \"{{{{.Names}}}}\"");

      if (string.IsNullOrWhiteSpace(containerExists))
      {
        logger.LogInformation("Creating Seq container...");
        await RunDockerCommand($"run -d --name {ContainerName} -e ACCEPT_EULA=Y -p {SeqPort}:80 datalust/seq");
      }
      else
      {
        var isRunning = await RunDockerCommand($"ps --filter name={ContainerName} --format \"{{{{.Names}}}}\"");
        if (string.IsNullOrWhiteSpace(isRunning))
        {
          logger.LogInformation("Starting Seq container...");
          await RunDockerCommand($"start {ContainerName}");
        }
      }

      logger.LogInformation("Seq is available at http://localhost:{Port}", SeqPort);
    }
    catch (Exception ex)
    {
      logger.LogWarning(ex, "Failed to start Seq container - Docker may not be running. Logs will still output to console");
    }
  }

  public async Task StopAsync(CancellationToken cancellationToken)
  {
    if (!environment.IsDevelopment())
      return;

    try
    {
      logger.LogInformation("Stopping Seq container...");
      await RunDockerCommand($"stop {ContainerName}");
    }
    catch (Exception ex)
    {
      logger.LogWarning(ex, "Failed to stop Seq container");
    }
  }

  private static async Task<string> RunDockerCommand(string arguments)
  {
    using var process = new Process
    {
      StartInfo = new ProcessStartInfo
      {
        FileName = "docker",
        Arguments = arguments,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true
      }
    };

    process.Start();
    var output = await process.StandardOutput.ReadToEndAsync();
    await process.WaitForExitAsync();

    return output.Trim();
  }
}
