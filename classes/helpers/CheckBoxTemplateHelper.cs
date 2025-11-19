using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace GamelistManager.classes.helpers
{
    public static class CheckBoxTemplateHelper
    {
        /// <summary>
        /// Creates a DataTemplate with a centered CheckBox bound to the specified property.
        /// Optionally, Checked and Unchecked events can be attached.
        /// </summary>
        /// <param name="bindingPath">The property name to bind the CheckBox to.</param>
        /// <param name="checkedHandler">Optional Checked event handler.</param>
        /// <param name="uncheckedHandler">Optional Unchecked event handler.</param>
        /// <returns>A DataTemplate with the configured CheckBox.</returns>
        public static DataTemplate CreateCheckbox(
            string bindingPath,
            RoutedEventHandler? checkedHandler = null,
            RoutedEventHandler? uncheckedHandler = null)
        {
            if (string.IsNullOrWhiteSpace(bindingPath))
                throw new ArgumentException("Binding path cannot be null or empty.", nameof(bindingPath));

            // Create a factory for the CheckBox
            FrameworkElementFactory checkBoxFactory = new(typeof(CheckBox));
            checkBoxFactory.SetValue(CheckBox.HorizontalAlignmentProperty, HorizontalAlignment.Center);

            // Bind the IsChecked property
            Binding binding = new(bindingPath)
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            checkBoxFactory.SetBinding(CheckBox.IsCheckedProperty, binding);

            // Attach event handlers if provided
            if (checkedHandler != null)
                checkBoxFactory.AddHandler(CheckBox.CheckedEvent, checkedHandler);
            if (uncheckedHandler != null)
                checkBoxFactory.AddHandler(CheckBox.UncheckedEvent, uncheckedHandler);

            // Create and return the DataTemplate
            DataTemplate template = new()
            {
                VisualTree = checkBoxFactory
            };

            return template;
        }
    }
}
