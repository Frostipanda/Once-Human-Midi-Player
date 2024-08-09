using System;
using System.Windows.Forms;
using System.Threading;
using OHMidiPlayer;

namespace MidiPlayer
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Show the splash screen
            using (SplashScreen splash = new SplashScreen())
            {
                splash.Show();
                Application.DoEvents();
                Thread.Sleep(3000);
            }

            // Now show the main form
            Application.Run(new Form1());
        }
    }
}
