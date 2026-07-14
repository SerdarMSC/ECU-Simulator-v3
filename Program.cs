using System;
using System.Threading;
using System.Windows.Forms;

namespace ECUSimulator_2
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            SplashScreen splash = new SplashScreen();

            Thread splashThread = new Thread(() =>
            {
                Application.Run(splash);
            });

            splashThread.SetApartmentState(ApartmentState.STA);
            splashThread.IsBackground = true;
            splashThread.Start();

            // Splash'ın görünmesini bekle
            Thread.Sleep(3000);

            MainForm mainForm = new MainForm();

            mainForm.Shown += (sender, e) =>
            {
                if (!splash.IsDisposed)
                {
                    splash.Invoke(new Action(() => splash.Close()));
                }
            };

            Application.Run(mainForm);
        }
    }
}