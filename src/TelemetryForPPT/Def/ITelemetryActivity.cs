using System;

namespace Microsoft.VisualStudio.TelemetryForPPT
{
    public interface ITelemetryActivity : IDisposable
    {
        void SetBoolProperty(string key, bool value);

        void SetStringProperty(string key, string value);

        void SetStringPiiProperty(string key, string value);

        void SetIntProperty(string key, int value);

        void SetProperty(string key, object value);

        void End();
    }
}
