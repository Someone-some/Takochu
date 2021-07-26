﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Takochu.fmt;
using Takochu.io;
using static System.Windows.Forms.TabControl;

namespace Takochu.ui
{
    public partial class BCSVEditorForm : Form
    {
        public BCSVEditorForm()
        {
            InitializeComponent();
            mEditors = new Dictionary<string, DataGridView>();
            mFiles = new Dictionary<string, BCSV>();

            int count = Properties.Settings.Default.BCSVPaths.Count;

            for (int i = count; i < count + 10; i--)
            {
                if (i == 0)
                    break;

                recentFilesListDropDown.DropDownItems.Add(Properties.Settings.Default.BCSVPaths[i - 1]);
            }
        }

        private DataGridView mDataGrid;

        private void OpenBCSV()
        {
            if (mFilesystem != null)
            {
                mFilesystem.Close();
            }

            foreach (BCSV file in mFiles.Values)
            {
                file.Close();
            }

            Properties.Settings.Default.BCSVPaths.Add(archiveTextBox.Text);
            recentFilesListDropDown.DropDownItems.Add(archiveTextBox.Text);

            // yeet 40 of the entries so we don't keep growing over and over again
            if (Properties.Settings.Default.BCSVPaths.Count == 50)
            {
                Properties.Settings.Default.BCSVPaths.RemoveRange(0, 40);

                recentFilesListDropDown.DropDownItems.Clear();

                int count = Properties.Settings.Default.BCSVPaths.Count;

                for (int i = count; i < count + 10; i--)
                {
                    if (i == 0)
                        break;

                    recentFilesListDropDown.DropDownItems.Add(Properties.Settings.Default.BCSVPaths[i - 1]);
                }
            }

            Properties.Settings.Default.Save();

            mFiles.Clear();
            mEditors.Clear();
            bcsvEditorsTabControl.TabPages.Clear();

            filesystemView.Nodes.Clear();

            mFilesystem = new RARCFilesystem(Program.sGame.mFilesystem.OpenFile(archiveTextBox.Text));

            TreeNode root = new TreeNode("/");

            PopulateTreeView(ref root, "/root");

            filesystemView.Nodes.Add(root);
        }

        private void openBCSVBtn_Click(object sender, EventArgs e)
        {
            OpenBCSV();
        }

        private void PopulateTreeView(ref TreeNode node, string parent)
        {
            List<string> dirs = mFilesystem.GetDirectories(parent);
            List<string> files = mFilesystem.GetFiles(parent);

            foreach (string file in files)
            {
                string ext = Path.GetExtension(file);

                if (ext == ".bcsv" || ext == ".pa" || ext == ".camn" || ext == ".bamnt" || ext == "" | ext == ".bcam")
                {
                    TreeNode tn = new TreeNode(file);
                    tn.Tag = Convert.ToString(parent + "/" + file);
                    node.Nodes.Add(tn);
                }
            }

            foreach (string dir in dirs)
            {
                TreeNode tn = new TreeNode(dir);
                node.Nodes.Add(tn);

                PopulateTreeView(ref tn, parent + "/" + dir);
            }
        }

        private void BCSVEditorForm_Load(object sender, EventArgs e)
        {

        }

        private RARCFilesystem mFilesystem;

        private void filesystemView_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (filesystemView.SelectedNode != null)
            {
                // this is simply a file vs folder check, since we don't assign tags to folders
                if (filesystemView.SelectedNode.Tag == null)
                    return;

                string tag = Convert.ToString(filesystemView.SelectedNode.Tag);

                if (mFiles.ContainsKey(tag))
                    return;


                BCSV file = new BCSV(mFilesystem.OpenFile(tag));
                mFiles.Add(tag, file);


                TabPage tab = new TabPage(tag);
                mDataGrid = new DataGridView();
                mDataGrid.CellValueChanged += Grid_CellValueChanged;
                mDataGrid.Dock = DockStyle.Fill;
                tab.Controls.Add(mDataGrid);
                bcsvEditorsTabControl.TabPages.Add(tab);
                mEditors.Add(tag, mDataGrid);

                saveBCSVBtn.Enabled = true;
                saveAll_Btn.Enabled = true;

                mDataGrid.Rows.Clear();
                mDataGrid.Columns.Clear();

                foreach (BCSV.Field f in file.mFields.Values)
                {
                    int columnIdx = mDataGrid.Columns.Add(f.mHash.ToString("X8"), f.mName);

                    // format floating point cells to show the first decimal point
                    if (f.mType == 2)
                        mDataGrid.Columns[columnIdx].DefaultCellStyle.Format = "N1";
                }

                foreach (BCSV.Entry entry in file.mEntries)
                {
                    object[] row = new object[entry.Count];
                    int i = 0;

                    foreach (KeyValuePair<int, object> _val in entry)
                    {
                        object val = _val.Value;
                        row[i++] = val;
                    }

                    mDataGrid.Rows.Add(row);
                }

                // now we can jump to that page
                bcsvEditorsTabControl.SelectedTab = tab;
            }
        }

        private void saveBCSVBtn_Click(object sender, EventArgs e)
        {
            TabPage curtab = bcsvEditorsTabControl.SelectedTab;
            // since we're saving, we can strip that * from tab pages
            curtab.Text = curtab.Text.Replace("*", "");
            DataGridView dataGrid = mEditors[curtab.Text];
            BCSV file = mFiles[curtab.Text];

            file.mEntries.Clear();

            foreach (DataGridViewRow r in dataGrid.Rows)
            {
                if (r.IsNewRow)
                    continue;

                BCSV.Entry entry = new BCSV.Entry();
                file.mEntries.Add(entry);

                foreach (BCSV.Field f in file.mFields.Values)
                {
                    int hash = f.mHash;
                    string valStr = r.Cells[hash.ToString("X8")].FormattedValue.ToString();

                    try
                    {
                        switch (f.mType)
                        {
                            case 0:
                            case 3:
                                entry.Add(hash, int.Parse(valStr));
                                break;
                            case 4:
                                entry.Add(hash, ushort.Parse(valStr));
                                break;
                            case 5:
                                entry.Add(hash, byte.Parse(valStr));
                                break;
                            case 2:
                                entry.Add(hash, float.Parse(valStr));
                                break;
                            case 6:
                                entry.Add(hash, valStr);
                                break;
                        }
                    }
                    catch
                    {
                        switch (f.mType)
                        {
                            case 0:
                            case 3:
                                entry.Add(hash, (int)0);
                                break;
                            case 4:
                                entry.Add(hash, (ushort)0);
                                break;
                            case 5:
                                entry.Add(hash, (byte)0);
                                break;
                            case 2:
                                entry.Add(hash, 0.0f);
                                break;
                            case 6:
                                entry.Add(hash, "");
                                break;
                        }
                    }
                }
            }

            file.Save();
            mFilesystem.Save();

            mUnsavedChanges = false;
        }

        public Dictionary<string, DataGridView> mEditors;
        public Dictionary<string, BCSV> mFiles;

        private bool mUnsavedChanges = false;

        void SaveAll()
        {
            TabPageCollection tabs = bcsvEditorsTabControl.TabPages;

            foreach (TabPage tabPage in tabs)
            {
                // since we're saving, we can strip that * from tab pages
                tabPage.Text = tabPage.Text.Replace("*", "");
                DataGridView dataGrid = mEditors[tabPage.Text];
                BCSV file = mFiles[tabPage.Text];

                file.mEntries.Clear();

                foreach (DataGridViewRow r in dataGrid.Rows)
                {
                    if (r.IsNewRow)
                        continue;

                    BCSV.Entry entry = new BCSV.Entry();
                    file.mEntries.Add(entry);

                    foreach (BCSV.Field f in file.mFields.Values)
                    {
                        int hash = f.mHash;
                        string valStr = r.Cells[hash.ToString("X8")].FormattedValue.ToString();

                        try
                        {
                            switch (f.mType)
                            {
                                case 0:
                                case 3:
                                    entry.Add(hash, int.Parse(valStr));
                                    break;
                                case 4:
                                    entry.Add(hash, ushort.Parse(valStr));
                                    break;
                                case 5:
                                    entry.Add(hash, byte.Parse(valStr));
                                    break;
                                case 2:
                                    entry.Add(hash, float.Parse(valStr));
                                    break;
                                case 6:
                                    entry.Add(hash, valStr);
                                    break;
                            }
                        }
                        catch
                        {
                            switch (f.mType)
                            {
                                case 0:
                                case 3:
                                    entry.Add(hash, (uint)0);
                                    break;
                                case 4:
                                    entry.Add(hash, (ushort)0);
                                    break;
                                case 5:
                                    entry.Add(hash, (byte)0);
                                    break;
                                case 2:
                                    entry.Add(hash, 0f);
                                    break;
                                case 6:
                                    entry.Add(hash, "");
                                    break;
                            }
                        }
                    }
                }

                file.Save();
                mFilesystem.Save();
            }

            mUnsavedChanges = false;
        }
         
        private void saveAll_Btn_Click(object sender, EventArgs e)
        {
            SaveAll();
        }

        private void Grid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            // simply check the current tab's name to see if any changes have been made
            // if so, add that onto it
            if (!bcsvEditorsTabControl.SelectedTab.Text.Contains("*"))
                bcsvEditorsTabControl.SelectedTab.Text += "*";

            mUnsavedChanges = true;
        }

        private void BCSVEditorForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (mUnsavedChanges)
            {
                DialogResult res = MessageBox.Show("You have unsaved changes! Do you want to save your changes?", "Unsaved Changes", MessageBoxButtons.YesNoCancel);

                if (res == DialogResult.Yes)
                {
                    SaveAll();
                }
                else if (res == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }

            if (mFilesystem != null)
                mFilesystem.Close();
        }

        private void openExternalBtn_Click(object sender, EventArgs e)
        {
            if (mFilesystem != null)
            {
                mFilesystem.Close();
            }

            foreach (BCSV file in mFiles.Values)
            {
                file.Close();
            }

            mFiles.Clear();
            mEditors.Clear();
            bcsvEditorsTabControl.TabPages.Clear();

            filesystemView.Nodes.Clear();

            OpenFileDialog dlg = new OpenFileDialog();

            if (dlg.ShowDialog() == DialogResult.OK)
            {
                mFilesystem = new RARCFilesystem(new ExternalFile(dlg.FileName));

                TreeNode root = new TreeNode("/");

                PopulateTreeView(ref root, "/root");

                filesystemView.Nodes.Add(root);
            }
        }

        private void archiveTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return && Program.sGame.DoesFileExist(archiveTextBox.Text))
            {
                OpenBCSV();
            }
        }

        private void recentFilesListDropDown_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            string file = e.ClickedItem.Text;
            archiveTextBox.Text = file;

            OpenBCSV();
        }

        private void archiveTextBox_TextChanged(object sender, EventArgs e)
        {
            if (Program.sGame.DoesFileExist(archiveTextBox.Text))
                openBCSVBtn.Enabled = true;
            else
                openBCSVBtn.Enabled = false;
        }

        private void RemoveEntryBtn_Click(object sender, EventArgs e)
        {
            BCSV file = mFiles[bcsvEditorsTabControl.SelectedTab.Text];
            file.mEntries.RemoveAt(mDataGrid.CurrentCell.RowIndex);
            mDataGrid.Rows.RemoveAt(mDataGrid.CurrentCell.RowIndex);
        }

        private void HashGenBtn_Click(object sender, EventArgs e)
        {
            HashGenForm hash = new HashGenForm();
            hash.Show();
        }
    }
}
