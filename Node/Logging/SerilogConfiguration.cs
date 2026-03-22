using Serilog;
using Serilog.Core;

namespace Swarm.Node.Logging;

public static class SerilogConfiguration
{
    public static Logger CreateLogger(IConfiguration configuration)
    {
        return new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .Enrich.WithProperty("Application", "Swarm.Node")
            .CreateLogger();
    }
}
