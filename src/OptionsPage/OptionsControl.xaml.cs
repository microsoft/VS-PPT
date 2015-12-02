using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Text.Editor;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Microsoft.VisualStudio.PowerTools.OptionsPage
{
    /// <summary>
    /// Interaction logic for OptionsControl.xaml
    /// </summary>
    public partial class OptionsControl : UserControl
    {
        private IEditorOptions _globalOptions;
        private const string _globalOptionsRegistryPath = @"Text Editor\Global Options";
        private readonly List<CheckBox> _allOptions = new List<CheckBox>();

        public OptionsControl(IEditorOptions globalOptions, IEnumerable<LabeledOptionDefinitionGroup> optionGroups)
        {
            InitializeComponent();

            _globalOptions = globalOptions;

            foreach (var optionGroup in optionGroups)
            {
                GroupBox groupBox = null;
                StackPanel itemList = null;

                // Create a group if the group heading is not null or empty.
                if (!string.IsNullOrEmpty(optionGroup.GroupHeading))
                {
                    groupBox = new GroupBox();
                    groupBox.Header = optionGroup.GroupHeading;

                    itemList = new StackPanel();
                    itemList.Orientation = Orientation.Vertical;

                    groupBox.Content = itemList;

                    groupBox.Padding = new Thickness(4.0, 6.0, 4.0, 2.0);
                    groupBox.Margin = new Thickness(0.0, 4.0, 0.0, 0.0);
                    groupBox.Foreground = SystemColors.WindowTextBrush;
                }

                bool atLeastOneOptionIsDefined = false;
                foreach (var labeledOption in optionGroup.LabeledOptionDefinitions)
                {
                    if (_globalOptions.IsOptionDefined(labeledOption.Name, localScopeOnly: true))
                    {
                        // Add a checkbox to the group if it exists; if the group doesn't exist, then
                        // add the checkbox to the list of options.
                        CheckBox box = new CheckBox();
                        _allOptions.Add(box);

                        box.IsEnabled = true;
                        box.IsChecked = _globalOptions.GetOptionValue<bool>(labeledOption.Name);

                        box.Tag = labeledOption;
                        box.Content = labeledOption.Label;

                        box.Margin = new Thickness(0.0, 1.0, 0.0, 1.0);

                        // We found at least one option that can is defined in global options.
                        // Now we can add the group to the options page (if there is a group).
                        if (!atLeastOneOptionIsDefined)
                        {
                            if (groupBox != null)
                            {
                                this.Options.Items.Add(groupBox);
                            }

                            atLeastOneOptionIsDefined = true;
                        }

                        if (itemList != null)
                        {
                            itemList.Children.Add(box);
                        }
                        else
                        {
                            this.Options.Items.Add(box);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Reset all options to default (in GlobalOptions)
        /// </summary>
        public void Reset()
        {
            foreach (CheckBox box in _allOptions)
            {
                LabeledOptionDefinition definition = box.Tag as LabeledOptionDefinition;
                if (definition != null)
                {
                    box.IsChecked = _globalOptions.GetOptionValue<bool>(definition.Name);
                }
            }
        }

        /// <summary>
        /// Save the options that are selected into the global options
        /// </summary>
        public void Apply()
        {
            foreach (CheckBox box in _allOptions)
            {
                LabeledOptionDefinition definition = box.Tag as LabeledOptionDefinition;
                if (definition != null)
                {
                    bool oldValue = _globalOptions.GetOptionValue<bool>(definition.Name);
                    if (box.IsChecked != oldValue)
                    {
                        _globalOptions.SetOptionValue(definition.Name, box.IsChecked);

                        this.SaveOption(definition.Name);
                    }
                }
            }
        }

        /// <summary>
        /// Reset the options.
        /// </summary>
        public void Clear()
        {
            this.Reset();
        }

        /// <summary>
        /// Save an option in the settings store so that it is persisted.
        /// </summary>
        /// <param name="optionId">The id of the option to save</param>
        private void SaveOption(string optionId)
        {
            using (ServiceProvider serviceProvider = new ServiceProvider(Common.GlobalServiceProvider))
            {
                SettingsManager settingsManager = new ShellSettingsManager(serviceProvider);
                WritableSettingsStore settingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);

                if (settingsStore != null)
                {
                    settingsStore.CreateCollection(_globalOptionsRegistryPath);
                    object value = _globalOptions.GetOptionValue(optionId);
                    if (value.GetType() == typeof(bool))
                    {
                        var newValue = (bool)value;
                        settingsStore.SetBoolean(_globalOptionsRegistryPath, optionId, newValue);

                        // Report the option change to telemetry.
                        OptionsPagePackage.TelemetrySession.PostEvent("VS/PPT-Options/OptionChanged", optionId, newValue);
                    }
                }
            }
        }
    }
}
