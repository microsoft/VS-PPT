using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.TelemetryForPPT
{
    /// <summary>
    /// Default, dummy <see cref="ITelemetrySession"/> implementation.
    /// It's intended to be used when extensions are used standalone, not as part of
    /// official ProPower Tools VSIX (which exports real implementation).
    /// </summary>
    internal class DefaultTelemetrySession : ITelemetrySession
    {
        private readonly static DefaultTelemetryActivity s_dummyActivity = new DefaultTelemetryActivity();

        public Task<ITelemetryActivity> CreateActivityAsync(Assembly callingAssembly, string name)
        {
            return Task.FromResult<ITelemetryActivity>(s_dummyActivity);
        }

        public void PostEvent(string key, params object[] namesAndProperties)
        {
            // Do nothing by default
        }
    }
}
