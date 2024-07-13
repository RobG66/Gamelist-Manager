using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace GamelistManager.control
{
    public partial class SearchAndReplace : UserControl
    {
        GamelistManagerForm gamelistManagerForm;
        Stack<UndoAction> undoStack;

        public class UndoAction
        {
            public int RowIndex { get; set; }
            public string OriginalValue { get; set; }
            public string ColumnName { get; set; }
        }

        public SearchAndReplace(GamelistManagerForm form)
        {
            gamelistManagerForm = form;
            InitializeComponent();
            undoStack = new Stack<UndoAction>();

        }

        private void buttonExit_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void buttonApply_Click(object sender, EventArgs e)
        {
            string box1 = textBox1.Text;
            string box2 = textBox2.Text;
            string column = comboBox1.Text;
            DialogResult result = MessageBox.Show($"Are you absolutely sure?\n\nReplace '{box1}' with '{box2}'?",
                                       "Confirmation",
                                       MessageBoxButtons.YesNo,
                                       MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            List<DataGridViewRow> selectedRowsList = new List<DataGridViewRow>();
            if (radioButtonAll.Checked)
            {
                selectedRowsList = gamelistManagerForm.DataGridView.Rows.Cast<DataGridViewRow>().ToList();
            }
            else
            {
                selectedRowsList = gamelistManagerForm.DataGridView.SelectedRows.Cast<DataGridViewRow>().ToList();
            }

            List<string> romPathList = selectedRowsList.Select(row => row.Cells["path"].Value?.ToString()).ToList();

            BusyCursor(true);
           
            foreach (DataRow row in SharedData.DataSet.Tables["game"].Rows)
            {
                string rowPath = row.Field<string>("path");

                if (!romPathList.Contains(rowPath))
                {
                    continue;
                }

                string currentValue = row.Field<string>(column);

                if (string.IsNullOrEmpty(currentValue) || currentValue == DBNull.Value.ToString())
                {
                    currentValue = string.Empty;
                }

                string newValue;

                if (box1.Contains("*") || box1.Contains("?"))
                {
                    // If wildcard characters are present, use regex for search and replace
                    string searchPattern = Regex.Escape(box1).Replace("\\*", ".*").Replace("\\?", ".");
                    newValue = Regex.Replace(currentValue, searchPattern, box2, RegexOptions.IgnoreCase);
                }
                else
                {
                    // If no wildcard characters are present, use simple string replace (case-insensitive)
                    newValue = ReplaceIgnoreCase(currentValue, box1, box2);
                }

                // Store the original value for undo
                undoStack.Push(new UndoAction
                {
                    RowIndex = SharedData.DataSet.Tables["game"].Rows.IndexOf(row),
                    OriginalValue = currentValue,
                    ColumnName = column
                });

                row[column] = newValue;
            }

            SharedData.DataSet.AcceptChanges();

            BusyCursor(false);
            buttonUndo.Enabled = true;
            buttonApply.Enabled = false;
        }

        private void BusyCursor(bool busy)
        {
            if (busy)
            {
                this.Cursor = Cursors.WaitCursor;
            }
            else
            {
                this.Cursor = Cursors.Default;
            }
        }
            private string ReplaceIgnoreCase(string input, string search, string replacement)
        {
            // Perform case-insensitive replacement
            return Regex.Replace(input, Regex.Escape(search), replacement, RegexOptions.IgnoreCase);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex != 0 && comboBox1.Items[0].ToString() == "<select>")
            {
                comboBox1.Items.RemoveAt(0); // Remove initial value
                comboBox1.SelectedIndex = comboBox1.FindStringExact(comboBox1.Text);
                return;
            }

            textBox1.Text = "";
            textBox2.Text = "";
            buttonApply.Enabled = false ;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == 0 && comboBox1.Items[0].ToString() == "<select>")
            {
                return;
            }
            buttonApply.Enabled = true;
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == 0 && comboBox1.Items[0].ToString() == "<select>")
            {
                return;
            }
            buttonApply.Enabled = true;
        }

        private void SearchAndReplace_Load(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 0;
        }

        private void buttonUndo_Click(object sender, EventArgs e)
        {

            if (undoStack.Count == 0)
            {
                MessageBox.Show("No actions to undo.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult result = MessageBox.Show("Are you sure you want to undo the last change?",
                                    "Confirmation",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
            {
                return;
            }

            BusyCursor(true);

            while (undoStack.Count > 0)
            {
                var action = undoStack.Pop();

                // Restore the original value
                SharedData.DataSet.Tables["game"].Rows[action.RowIndex][action.ColumnName] = action.OriginalValue;
            }

            SharedData.DataSet.AcceptChanges();
            buttonUndo.Enabled = false;

            BusyCursor(false);

        }
    }
}
