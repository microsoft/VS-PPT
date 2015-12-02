using System.Reflection;

namespace Microsoft.VisualStudio.TelemetryForPPT
{
    public interface ITelemetrySessionFactory
    {
        ITelemetrySession CreateSession(Assembly callingAssembly);
    }
}
