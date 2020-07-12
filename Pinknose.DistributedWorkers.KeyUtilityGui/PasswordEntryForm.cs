using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Pinknose.DistributedWorkers.KeyUtilityGui
{
    public partial class PasswordEntryForm : Form
    {
        public string Password { get; set; } = "";

        public PasswordEntryForm()
        {
            InitializeComponent();
        }
    }
}
