using System.Reflection;
using System.Threading.Tasks;

namespace Microsoft.VisualStudio.TelemetryForPPT
{
    public interface ITelemetrySession
    {
        void PostEvent(string key, params object[] namesAndProperties);

        Task<ITelemetryActivity> CreateActivityAsync(Assembly callingAssembly, string name);
    }
}
