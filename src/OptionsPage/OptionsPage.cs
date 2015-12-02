using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;

namespace Microsoft.VisualStudio.PowerTools.OptionsPage
{
    [Guid(OptionsPageGuidString)]
    internal class OptionsPage : UIElementDialogPage
    {
        /// <summary>
        /// GUID of the options page
        /// </summary>
        internal const string OptionsPageGuidString = "F4940076-DBDA-46C0-B0D3-81AFD3EBF6E0";

        /// <summary>
        /// A list of options that will be listed in the Tools>Options dialog.
        /// </summary>
        public static readonly List<LabeledOptionDefinitionGroup> Options = new List<LabeledOptionDefinitionGroup>()
        {
            new LabeledOptionDefinitionGroup(Strings.MatchMarginOptions,
                    new LabeledOptionDefinition(Strings.MarginMatch, "MatchMarginEnabled"),
                    new LabeledOptionDefinition(Strings.AdornmentMatch, "MatchAdornmentEnabled")
            ),
            new LabeledOptionDefinitionGroup(Strings.TimeStampMarginOptions,
                    new LabeledOptionDefinition(Strings.TimeStampMarginEnabled, "TimeStampMarginEnabled"),
                    new LabeledOptionDefinition(Strings.TimeStampMarginShowHours, "TimeStampMarginShowHours"),
                    new LabeledOptionDefinition(Strings.TimeStampMarginShowMilliseconds, "TimeStampMarginShowMilliseconds")
            ),
            new LabeledOptionDefinitionGroup(Strings.StructureVisualizerOptions,
                    new LabeledOptionDefinition(Strings.MethodSeparatorEnabled, "MethodSeparatorEnabled"),
                    new LabeledOptionDefinition(Strings.StructureAdornmentEnabled, "StructureAdornmentEnabled"),
                    new LabeledOptionDefinition(Strings.StructureMarginEnabled, "StructureMarginEnabled"),
                    new LabeledOptionDefinition(Strings.StructurePreviewEnabled, "StructurePreviewEnabled")
            ),
            new LabeledOptionDefinitionGroup(Strings.ControlClickOptions,
                    new LabeledOptionDefinition(Strings.ControlClickOpensPeek, "ControlClickOpensPeek")
            ),
            new LabeledOptionDefinitionGroup(Strings.SyntacticFisheyeOptions,
                    new LabeledOptionDefinition(Strings.SyntacticFisheyeCompressBlankLines, "SyntacticFisheyeCompressBlankLines"),
                    new LabeledOptionDefinition(Strings.SyntacticFisheyeCompressSimpleLines, "SyntacticFisheyeCompressSimpleLines")
            )
        };

        private OptionsControl _control;

        public OptionsPage()
        {
            var factory = Common.GetMefService<IEditorOptionsFactoryService>();
            _control = new OptionsControl(factory.GlobalOptions, OptionsPage.Options);
        }

        protected override UIElement Child
        {
            get
            {
                return _control;
            }
        }

        protected override void OnActivate(System.ComponentModel.CancelEventArgs e)
        {
            base.OnActivate(e);

            _control.Reset();
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
            base.OnApply(e);

            _control.Apply();
        }

        protected override void OnClosed(System.EventArgs e)
        {
            base.OnClosed(e);

            _control.Clear();
        }
    }

    public class LabeledOptionDefinitionGroup
    {
        public string GroupHeading { get; private set; }
        public IEnumerable<LabeledOptionDefinition> LabeledOptionDefinitions { get; private set; }

        public LabeledOptionDefinitionGroup(string groupHeading, params LabeledOptionDefinition[] definitions)
        {
            this.GroupHeading = groupHeading;
            this.LabeledOptionDefinitions = definitions;
        }
    }

    public class LabeledOptionDefinition
    {
        public string Label { get; private set; }
        public string Name { get; private set; }

        public LabeledOptionDefinition(string label, string name)
        {
            this.Label = label;
            this.Name = name;
        }
    }
}