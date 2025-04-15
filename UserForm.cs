using Autodesk.AutoCAD.ApplicationServices;
using System;
using System.Windows.Forms;

namespace SpiralStaircasePlugin
{
    public partial class UserForm : Form
    {
        public double CenterPoleDia { get; private set; }
        public double OverallHeight { get; private set; }
        public double OutsideDia { get; private set; }
        public double RotationDeg { get; private set; }
        public bool IsClockwise { get; private set; }
        public bool FormSubmitted { get; private set; }

        public UserForm()
        {
            InitializeComponent();
            FormSubmitted = false;
        }

        private void InitializeComponent()
        {
            // Form settings
            this.Text = "Spiral Staircase Input";
            this.Size = new System.Drawing.Size(350, 300);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Controls
            int yPosition = 20;
            int spacing = 30;

            // Center Pole Diameter
            Label lblCenterPoleDia = new Label
            {
                Text = "Center Pole Diameter (inches):",
                Location = new System.Drawing.Point(20, yPosition),
                Size = new System.Drawing.Size(200, 20)
            };
            TextBox txtCenterPoleDia = new TextBox
            {
                Location = new System.Drawing.Point(220, yPosition),
                Size = new System.Drawing.Size(100, 20)
            };
            yPosition += spacing;

            // Overall Height
            Label lblOverallHeight = new Label
            {
                Text = "Overall Height (inches):",
                Location = new System.Drawing.Point(20, yPosition),
                Size = new System.Drawing.Size(200, 20)
            };
            TextBox txtOverallHeight = new TextBox
            {
                Location = new System.Drawing.Point(220, yPosition),
                Size = new System.Drawing.Size(100, 20)
            };
            yPosition += spacing;

            // Outside Diameter
            Label lblOutsideDia = new Label
            {
                Text = "Outside Diameter (inches):",
                Location = new System.Drawing.Point(20, yPosition),
                Size = new System.Drawing.Size(200, 20)
            };
            TextBox txtOutsideDia = new TextBox
            {
                Location = new System.Drawing.Point(220, yPosition),
                Size = new System.Drawing.Size(100, 20)
            };
            yPosition += spacing;

            // Total Rotation
            Label lblRotationDeg = new Label
            {
                Text = "Total Rotation (degrees):",
                Location = new System.Drawing.Point(20, yPosition),
                Size = new System.Drawing.Size(200, 20)
            };
            TextBox txtRotationDeg = new TextBox
            {
                Location = new System.Drawing.Point(220, yPosition),
                Size = new System.Drawing.Size(100, 20)
            };
            yPosition += spacing;

            // Rotation Direction
            Label lblDirection = new Label
            {
                Text = "Rotation Direction:",
                Location = new System.Drawing.Point(20, yPosition),
                Size = new System.Drawing.Size(200, 20)
            };
            RadioButton optClockwise = new RadioButton
            {
                Text = "Clockwise",
                Location = new System.Drawing.Point(220, yPosition),
                Size = new System.Drawing.Size(100, 20),
                Checked = true
            };
            yPosition += spacing / 2;
            RadioButton optCounterClockwise = new RadioButton
            {
                Text = "Counter-Clockwise",
                Location = new System.Drawing.Point(220, yPosition),
                Size = new System.Drawing.Size(120, 20)
            };
            yPosition += spacing;

            // Buttons
            Button btnOk = new Button
            {
                Text = "OK",
                Location = new System.Drawing.Point(120, yPosition),
                Size = new System.Drawing.Size(75, 30),
                DialogResult = DialogResult.OK
            };
            btnOk.Click += (s, e) =>
            {
                if (ValidateInputs(txtCenterPoleDia, txtOverallHeight, txtOutsideDia, txtRotationDeg))
                {
                    CenterPoleDia = Convert.ToDouble(txtCenterPoleDia.Text);
                    OverallHeight = Convert.ToDouble(txtOverallHeight.Text);
                    OutsideDia = Convert.ToDouble(txtOutsideDia.Text);
                    RotationDeg = Convert.ToDouble(txtRotationDeg.Text);
                    IsClockwise = optClockwise.Checked;
                    FormSubmitted = true;
                    this.Close();
                }
            };

            Button btnCancel = new Button
            {
                Text = "Cancel",
                Location = new System.Drawing.Point(205, yPosition),
                Size = new System.Drawing.Size(75, 30),
                DialogResult = DialogResult.Cancel
            };
            btnCancel.Click += (s, e) =>
            {
                FormSubmitted = false;
                this.Close();
            };

            // Set CancelButton after btnCancel is declared
            this.CancelButton = btnCancel;

            // Add controls to form
            this.Controls.AddRange(new Control[] { lblCenterPoleDia, txtCenterPoleDia, lblOverallHeight, txtOverallHeight, lblOutsideDia, txtOutsideDia, lblRotationDeg, txtRotationDeg, lblDirection, optClockwise, optCounterClockwise, btnOk, btnCancel });
        }

        private bool ValidateInputs(TextBox txtCenterPoleDia, TextBox txtOverallHeight, TextBox txtOutsideDia, TextBox txtRotationDeg)
        {
            if (!double.TryParse(txtCenterPoleDia.Text, out _))
            {
                MessageBox.Show("Please enter a valid numeric value for Center Pole Diameter.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (!double.TryParse(txtOverallHeight.Text, out _))
            {
                MessageBox.Show("Please enter a valid numeric value for Overall Height.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (!double.TryParse(txtOutsideDia.Text, out _))
            {
                MessageBox.Show("Please enter a valid numeric value for Outside Diameter.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            if (!double.TryParse(txtRotationDeg.Text, out _))
            {
                MessageBox.Show("Please enter a valid numeric value for Total Rotation.", "Input Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }
    }
}