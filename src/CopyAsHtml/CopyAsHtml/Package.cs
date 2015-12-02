using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TelemetryForPPT;

namespace CopyAsHtml
{
    public class GuidList
    {
        public const string Package = "61684dba-41b3-404f-be62-f4e6dd74c18b";
        public const string CommandSet = "9103a42b-9b65-4d63-b852-577056db412f";

        public static readonly Guid CommandSetGuid = new Guid(CommandSet);
    }

    [PackageRegistration(UseManagedResourcesOnly = true)]
    [Guid(GuidList.Package)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(ToolsOptionsPage), CopyAsHtmlPackage.CategoryName, CopyAsHtmlPackage.PageName, 1, 2, true)]
    public class CopyAsHtmlPackage : Package
    {
        public const string CategoryName = "Productivity Power Tools";
        public const string PageName = "HTML Copy";
    }

    [Guid("86E0CD70-9576-4569-B7BF-6B229C5B34B0")]
    [ComVisible(true)]
    public class ToolsOptionsPage
        : DialogPage
    {
        private const string _beforeCodeSnippetDefault = "<pre style=\"{font-family}{font-size}{font-weight}{font-style}{color}{background}\">";
        private const string _afterCodeSnippetDefault = "</pre>";
        private const string _spaceDefault = "&nbsp;";
        private const bool _replaceLineBreaksWithBRDefault = false;
        private const bool _replaceTabsWithSpacesDefault = false;
        private const bool _emitSpanStyleDefault = true;
        private const bool _emitSpanClassDefault = false;
        private const bool _unindentToRemoveExtraLeadingWhitespaceDefault = true;
        private const string _propertyCategoryName = "General";

        private bool _replaceLineBreaksWithBR;
        private bool _replaceTabsWithSpaces;
        private bool _emitSpanStyle;
        private bool _emitSpanClass;
        private bool _unindentToRemoveExtraLeadingWhitespace;

        private ITelemetrySession _telemetrySession;

        public ToolsOptionsPage()
        {
            _telemetrySession = TelemetrySessionForPPT.Create(this.GetType().Assembly);
            BeforeCodeSnippet = _beforeCodeSnippetDefault;
            AfterCodeSnippet = _afterCodeSnippetDefault;
            Space = _spaceDefault;
            ReplaceLineBreaksWithBR = _replaceLineBreaksWithBRDefault;
            ReplaceTabsWithSpaces = _replaceTabsWithSpacesDefault;
            UnindentToRemoveExtraLeadingWhitespace = _unindentToRemoveExtraLeadingWhitespaceDefault;
            EmitSpanClass = _emitSpanClassDefault;
            EmitSpanStyle = _emitSpanStyleDefault;
            Instance = this;
        }

        private static ToolsOptionsPage s_instance;
        internal static ToolsOptionsPage Instance
        {
            get
            {
                if (s_instance == null)
                {
                    // constructor will call this property's setter so the object isn't lost
                    new ToolsOptionsPage();
                }

                return s_instance;
            }
            private set
            {
                s_instance = value;
            }
        }

        private void LogOptionChange(bool oldValue, bool newValue, [CallerMemberName] string optionName = null)
        {
            if (oldValue != newValue)
            {
                _telemetrySession.PostEvent("VS/PPT-CopyAsHTML/OptionChanged", optionName, newValue);
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Description("Markup before the entire code snippet. Strings in { } will be substituted for actual values from the editor. You can use {font-family}, {font-size}, {font-weight}, {font-style}, {color} and {background}.")]
        [Category(_propertyCategoryName)]
        [DefaultValue(_beforeCodeSnippetDefault)]
        public string BeforeCodeSnippet { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Description("Markup after the entire code snippet.")]
        [Category(_propertyCategoryName)]
        [DefaultValue(_afterCodeSnippetDefault)]
        public string AfterCodeSnippet { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Description("All the spaces will be replaced with this string (e.g. &&nbsp;). Enter a space to keep spaces untouched.")]
        [Category(_propertyCategoryName)]
        [DefaultValue(_spaceDefault)]
        public string Space { get; set; }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Description("Unindent all lines to remove extra leading whitespace")]
        [Category(_propertyCategoryName)]
        [DefaultValue(_unindentToRemoveExtraLeadingWhitespaceDefault)]
        public bool UnindentToRemoveExtraLeadingWhitespace
        {
            get { return _unindentToRemoveExtraLeadingWhitespace; }
            set { LogOptionChange(_unindentToRemoveExtraLeadingWhitespace, value); _unindentToRemoveExtraLeadingWhitespace = value; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Description("Replace line breaks with the <br /> tag")]
        [Category(_propertyCategoryName)]
        [DefaultValue(_replaceLineBreaksWithBRDefault)]
        public bool ReplaceLineBreaksWithBR
        {
            get { return _replaceLineBreaksWithBR; }
            set { LogOptionChange(_replaceLineBreaksWithBR, value); _replaceLineBreaksWithBR = value; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Description("Replace tabs with spaces")]
        [Category(_propertyCategoryName)]
        [DefaultValue(_replaceTabsWithSpacesDefault)]
        public bool ReplaceTabsWithSpaces
        {
            get { return _replaceTabsWithSpaces; }
            set { LogOptionChange(_replaceTabsWithSpaces, value); _replaceTabsWithSpaces = value; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Description("Will hardcode the colorized span color using <span style=\"color: blue\">")]
        [Category(_propertyCategoryName)]
        [DefaultValue(_emitSpanStyleDefault)]
        public bool EmitSpanStyle
        {
            get { return _emitSpanStyle; }
            set { LogOptionChange(_emitSpanStyle, value); _emitSpanStyle = value; }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [Description("Will generate the span classification type using <span class=\"keyword\">")]
        [Category(_propertyCategoryName)]
        [DefaultValue(_emitSpanClassDefault)]
        public bool EmitSpanClass
        {
            get { return _emitSpanClass; }
            set { LogOptionChange(_emitSpanClass, value); _emitSpanClass = value; }
        }
    }
}
