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
                Application.DoEvents(); // Process all windows messages
                Thread.Sleep(3000); // Wait for 3 seconds
            }

            // Now show the main form
            Application.Run(new Form1());
        }
    }
}
