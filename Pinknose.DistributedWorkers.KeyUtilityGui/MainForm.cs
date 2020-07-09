using Pinknose.DistributedWorkers.Clients;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Pinknose.DistributedWorkers.KeyUtilityGui
{
    public partial class MainForm : Form
    {
        private const string publicFileSearchPattern = "*.pub";
        private const string privateFileSearchPattern = "*.priv?";

        public MainForm()
        {
            InitializeComponent();

            openDirectoryToolStripMenuItem.Click += OpenDirectoryToolStripMenuItem_Click;
            fileTreeView.AfterSelect += FileTreeView_AfterSelect;
            newToolStripMenuItem.Click += NewToolStripMenuItem_Click;

        }

        private void NewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using var form = new NewIdentityForm();
            form.ShowDialog();

            if (form.Success)
            {
                OpenDirectory(form.FolderPath);
            }
        }

        private void FileTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            try
            {
                jsonTextBox.Text = File.ReadAllText((string)e.Node.Tag);
                OpenKeyFile((string)e.Node.Tag);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void OpenKeyFile(string path)
        {
            var identity = MessageClientIdentity.ImportFromFile(path);

            propertyGrid.SelectedObject = identity;
        }

        private void OpenDirectoryToolStripMenuItem_Click(object sender, System.EventArgs e)
        {
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                OpenDirectory(folderBrowserDialog.SelectedPath);
            }
        }

        private void OpenDirectory(string directory)
        {
            directoryStatusLabel.Text = directory;
            propertyGrid.SelectedObject = null;
            jsonTextBox.Text = "";


            List<string> files = new List<string>();

            files.AddRange(Directory.EnumerateFiles(directory, publicFileSearchPattern));
            files.AddRange(Directory.EnumerateFiles(directory, privateFileSearchPattern));

            files.Sort();

            fileTreeView.BeginUpdate();
            fileTreeView.Nodes.Clear();

            foreach (string file in files)
            {
                var node = new TreeNode(Path.GetFileName(file))
                {
                    Tag = file
                };
                fileTreeView.Nodes.Add(node);
            }

            fileTreeView.EndUpdate();
        }
    }
}
