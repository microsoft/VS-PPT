
namespace Microsoft.VisualStudio.TelemetryForPPT
{
    /// <summary>
    /// Default, dummy <see cref="ITelemetryActivity"/> implementation.
    /// It's intended to be used when extensions are used standalone, not as part of
    /// official ProPower Tools VSIX (which exports real implementation).
    /// </summary>
    internal class DefaultTelemetryActivity : ITelemetryActivity
    {
        public void Dispose()
        {
        }

        public void End()
        {
        }

        public void SetBoolProperty(string key, bool value)
        {
        }

        public void SetIntProperty(string key, int value)
        {
        }

        public void SetProperty(string key, object value)
        {
        }

        public void SetStringPiiProperty(string key, string value)
        {
        }

        public void SetStringProperty(string key, string value)
        {
        }
    }
}
