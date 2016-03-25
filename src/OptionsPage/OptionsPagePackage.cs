using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TelemetryForPPT;
using Microsoft.VisualStudio.Text.Editor;
using System.Runtime.InteropServices;

namespace Microsoft.VisualStudio.PowerTools.OptionsPage
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [Guid(PackageGuidString)]
    [ProvideOptionPage(typeof(OptionsPage), "Productivity Power Tools", "Other Extensions", 2000, 2010, true, keywordListResourceName: "ToolsOptionsKeywords")]
    // Automatically load the package after 5 seconds if it hasn't be loaded already
    [ProvideUIContextRule(ContextGuidString,
                          name: "AutoLoad Settings Sync",
                          expression: "ShellInitialized",
                          termNames: new[] { "ShellInitialized" },
                          termValues: new[] { VSConstants.UICONTEXT.ShellInitialized_string },
                          delay: 5000)]
    [ProvideAutoLoad(ContextGuidString)]
    public sealed class OptionsPagePackage : Package
    {
        // Guid for OptionsPagePackage
        internal const string PackageGuidString = "d177414d-2d6a-49f2-8497-c51a2629846b";

        // Guid for UI Context
        internal const string ContextGuidString = "{0FA62381-8038-4DFA-9575-E87661C99253}";

        // Object to report telemetry through
        internal static ITelemetrySession TelemetrySession { get; private set; }

        #region Package Members
        protected override void Initialize()
        {
            base.Initialize();

            // Log telemetry on the initial settings values
            OptionsPagePackage.TelemetrySession = TelemetrySessionForPPT.Create(this.GetType().Assembly);

            IEditorOptions globalOptions = (Common.GetMefService<IEditorOptionsFactoryService>()).GlobalOptions;
            foreach (object option in OptionsPage.Options)
            {
                var definition = option as LabeledOptionDefinition;
                if (definition != null)
                {
                    if (globalOptions.IsOptionDefined(definition.Name, localScopeOnly: true))
                    {
                        var value = globalOptions.GetOptionValue<bool>(definition.Name);
                        OptionsPagePackage.TelemetrySession.PostEvent("VS/PPT-Options/OptionInitialValue", definition.Name, value);
                    }
                }
            }
        }
        #endregion
    }
}
