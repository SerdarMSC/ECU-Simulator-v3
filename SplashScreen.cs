using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace ECUSimulator_2
{
    public partial class SplashScreen : Form
    {
        public SplashScreen()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.White;
            this.Size = new Size(500, 400);

            PictureBox pictureBox = new PictureBox
            {
                Dock = DockStyle.Fill,
                SizeMode = PictureBoxSizeMode.Zoom
            };


            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string resourceName = "ECUSimulator_2.splash.png";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        pictureBox.Image = Image.FromStream(stream);
                    }
                    else
                    {

                        pictureBox.BackColor = Color.LightGray;
                        Label lbl = new Label
                        {
                            Text = "splash.png kaynağı bulunamadı!",
                            Dock = DockStyle.Fill,
                            TextAlign = ContentAlignment.MiddleCenter,
                            ForeColor = Color.Gray,
                            Font = new Font("Consolas", 12)
                        };
                        pictureBox.Controls.Add(lbl);
                    }
                }
            }
            catch (Exception ex)
            {

                pictureBox.BackColor = Color.LightGray;
            }

            this.Controls.Add(pictureBox);
        }
    }
}