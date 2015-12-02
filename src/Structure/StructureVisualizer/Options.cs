using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.PowerTools.StructureVisualizer
{
    [Export(typeof(EditorOptionDefinition))]
    [Name(StructureMarginEnabledOption.OptionName)]
    public sealed class StructureMarginEnabledOption : WpfViewOptionDefinition<bool>
    {
        public const string OptionName = "StructureMarginEnabled";
        public readonly static EditorOptionKey<bool> OptionKey = new EditorOptionKey<bool>(StructureMarginEnabledOption.OptionName);

        public override bool Default { get { return true; } }

        public override EditorOptionKey<bool> Key { get { return StructureMarginEnabledOption.OptionKey; } }
    }

    [Export(typeof(EditorOptionDefinition))]
    [Name(StructureAdornmentEnabledOption.OptionName)]
    public sealed class StructureAdornmentEnabledOption : WpfViewOptionDefinition<bool>
    {
        public const string OptionName = "StructureAdornmentEnabled";
        public readonly static EditorOptionKey<bool> OptionKey = new EditorOptionKey<bool>(StructureAdornmentEnabledOption.OptionName);

        public override bool Default { get { return true; } }

        public override EditorOptionKey<bool> Key { get { return StructureAdornmentEnabledOption.OptionKey; } }
    }

    [Export(typeof(EditorOptionDefinition))]
    [Name(StructurePreviewEnabledOption.OptionName)]
    public sealed class StructurePreviewEnabledOption : WpfViewOptionDefinition<bool>
    {
        public const string OptionName = "StructurePreviewEnabled";
        public readonly static EditorOptionKey<bool> OptionKey = new EditorOptionKey<bool>(StructurePreviewEnabledOption.OptionName);

        public override bool Default { get { return true; } }

        public override EditorOptionKey<bool> Key { get { return StructurePreviewEnabledOption.OptionKey; } }
    }

    [Export(typeof(EditorOptionDefinition))]
    [Name(MethodSeparatorEnabledOption.OptionName)]
    public sealed class MethodSeparatorEnabledOption : WpfViewOptionDefinition<bool>
    {
        public const string OptionName = "MethodSeparatorEnabled";
        public readonly static EditorOptionKey<bool> OptionKey = new EditorOptionKey<bool>(MethodSeparatorEnabledOption.OptionName);

        public override bool Default { get { return false; } }

        public override EditorOptionKey<bool> Key { get { return MethodSeparatorEnabledOption.OptionKey; } }
    }
}
