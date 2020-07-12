using Pinknose.DistributedWorkers.Clients;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Pinknose.DistributedWorkers.KeyUtilityGui
{
    public partial class NewIdentityForm : Form
    {
        public NewIdentityForm()
        {
            InitializeComponent();


        }

        private void jsonFormatCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            passwordTextBox1.Enabled = jsonFormatCheckBox.Checked;
            passwordTextBox2.Enabled = jsonFormatCheckBox.Checked;
        }

        private void NewIdentityForm_Load(object sender, EventArgs e)
        {
            foreach (var curve in Enum.GetValues(typeof(ECDiffieHellmanCurve)))
            {
                curveComboBox.Items.Add(curve);
            }

            curveComboBox.SelectedIndex = 0;
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            Success = false;
            this.Close();
        }

        private void createButton_Click(object sender, EventArgs e)
        {
            Success = false;

            if (!jsonFormatCheckBox.Checked &&
                !currentUserCheckBox.Checked &&
                !localMachineCheckBox.Checked)
            {
                ShowError("At least one private key format must be selected.");
                return;
            }

            if (jsonFormatCheckBox.Checked &&
                string.IsNullOrEmpty(passwordTextBox1.Text) &&
                string.IsNullOrEmpty(passwordTextBox2.Text))
            {
                if (MessageBox.Show("Saving the private key (JSON format) without encryption is dangerous.  Are you sure you want to save it unencrypted?  Click 'OK' if you want to continue saving the encrypted key.",
                        "Empty Password",
                        MessageBoxButtons.OKCancel,
                        MessageBoxIcon.Exclamation) == DialogResult.Cancel)
                {
                    return;
                }
            }

            if (jsonFormatCheckBox.Checked &&
                passwordTextBox1.Text !=
                passwordTextBox2.Text)
            {
                ShowError("Passwords do not match.  Identity creation is canceled.");
                return;
            }

            if (string.IsNullOrEmpty(systemNameTextBox.Text) || 
                string.IsNullOrEmpty(clientNameTextBox.Text))
            {
                ShowError("System and Client names cannot be empty.");
                return;
            }

            folderBrowserDialog.SelectedPath = FolderPath;

            if (folderBrowserDialog.ShowDialog() == DialogResult.Cancel)
            {
                ShowCreationCanceledMessageBox();
                return;
            }

            FolderPath = folderBrowserDialog.SelectedPath;

            var curve = (ECDiffieHellmanCurve)curveComboBox.SelectedItem;

            var identity = MessageClientIdentity.CreateClientInfo(this.systemNameTextBox.Text, this.clientNameTextBox.Text, curve, true);

            string filePath = Path.Combine(FolderPath, this.systemNameTextBox.Text + "-" + this.clientNameTextBox.Text);

            File.WriteAllText(filePath + ".pub", identity.SerializePublicInfoToJson());
            
            if (jsonFormatCheckBox.Checked && this.passwordTextBox1.Text != "")
            {
                File.WriteAllText(filePath + ".priv", identity.SerializePrivateInfoToJson(Encryption.Password, this.passwordTextBox1.Text));
            }
            else if (jsonFormatCheckBox.Checked)
            {
                File.WriteAllText(filePath + ".priv", identity.SerializePrivateInfoToJson(Encryption.None));
            }

            if (currentUserCheckBox.Checked)
            {
                File.WriteAllText(filePath + ".privu", identity.SerializePrivateInfoToJson(Encryption.CurrentUser));
            }

            if (localMachineCheckBox.Checked)
            {
                File.WriteAllText(filePath + ".privl", identity.SerializePrivateInfoToJson(Encryption.LocalMachine));
            }

            MessageBox.Show("Client identity creation complete.", "Operation Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Success = true;
            this.Close();
        }

        public string FolderPath { get; set; }

        public bool Success { get; private set; } = false;

        private static void ShowCreationCanceledMessageBox()
        {
            MessageBox.Show("Client identity creation canceled.", "Operation Canceled", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static void ShowError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void isServerIdentityCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (isServerIdentityCheckBox.Checked)
            {
                clientNameTextBox.Enabled = false;
                clientNameTextBox.Text = "server";
            }
            else
            {
                clientNameTextBox.Enabled = true;
                clientNameTextBox.Text = "";
            }
        }
    }
}
