using System.ComponentModel.Composition;
using System.Reflection;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.TelemetryForPPT
{
    /// <summary>
    /// Default, dummy <see cref="ITelemetrySessionFactory"/> implementation.
    /// It's intended to be used when extensions are used standalone, not as part of
    /// official ProPower Tools VSIX (which exports real <see cref="ITelemetrySessionFactory"/>
    /// implementation.
    /// </summary>
    [Export(typeof(ITelemetrySessionFactory))]
    [Name("Default Telemetry Session Factory")]
    [Order]
    internal class DefaultTelemetrySessionFactory : ITelemetrySessionFactory
    {
        private readonly static DefaultTelemetrySession s_dummySession = new DefaultTelemetrySession();

        public ITelemetrySession CreateSession(Assembly callingAssembly)
        {
            return s_dummySession;
        }
    }
}
