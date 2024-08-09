using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NAudio.Midi;
using WindowsInput;
using WindowsInput.Native;
using System.Diagnostics;
using System.Timers;

namespace OHMidiPlayer
{
    public partial class Form1 : Form
    {
        private InputSimulator inputSimulator;
        private MidiFile midiFile;
        private bool isPlaying;
        private Bitmap backgroundImage;
        private string selectedFolderPath;
        private VirtualKeyCode? currentModifier; // Class-level variable
        private CancellationTokenSource cancellationTokenSource; // For stopping playback
        private System.Timers.Timer checkProcessTimer; // Timer for checking process
        private bool isOnceHumanRunning; // Flag for process status
        private System.Windows.Forms.Timer scrollTimer;
        private int scrollPosition;
        private string originalText;
        private bool isPaused;
        private int pauseCounter;
        private bool isInitialPause;
        private MidiIn midiIn;
        private bool playAllTracks = true;
        private int currentTrackIndex = 0;
        private int totalTracks = 0;
        private const uint MOD_NOREPEAT = 0x4000; // Prevents the keyboard from repeating the key press

        public Form1()
        {
            InitializeComponent();
            inputSimulator = new InputSimulator();
            isPlaying = false;

            // Set the form style to none
            this.FormBorderStyle = FormBorderStyle.None;
            this.DoubleBuffered = true; // Reduce flicker

            // Load the custom background image from resources
            backgroundImage = Properties.Resources.mainbody; // Assuming the image is named mainbody.png in Resources

            // Add event handlers for picture boxes
            exit.Click += Exit_Click;
            minimize.Click += Minimize_Click;

            // Initialize process check timer
            checkProcessTimer = new System.Timers.Timer(2000); // Check every 2 seconds
            checkProcessTimer.Elapsed += CheckProcessTimer_Elapsed;
            checkProcessTimer.Start();

            // Initialize scroll timer for loadedsong label
            scrollTimer = new System.Windows.Forms.Timer();
            scrollTimer.Interval = 200; // Adjust the interval for scrolling speed (slower)
            scrollTimer.Tick += ScrollTimer_Tick;
            scrollPosition = 0;
            originalText = loadedsong.Text;
            isPaused = false;
            pauseCounter = 0;
            isInitialPause = true; // Set initial pause

            // Add event handlers for picture boxes
            exit.Click += Exit_Click;
            minimize.Click += Minimize_Click;

            // Add event handlers for the new picture boxes
            tracktoggle.Click += tracktoggle_Click;
            prevtrack.Click += prevtrack_Click;
            nexttrack.Click += nexttrack_Click;

            speedScrollBar.Scroll += speedScrollBar_Scroll;

            // Detect MIDI devices
            DetectMidiDevices();
        }

        // Event handler for exit button
        private void Exit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        // Event handler for minimize button
        private void Minimize_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        // Timer event to check if ONCE_HUMAN.exe is running
        private void CheckProcessTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            var process = Process.GetProcessesByName("ONCE_HUMAN").FirstOrDefault();
            isOnceHumanRunning = process != null;

            this.Invoke(new Action(() =>
            {
                if (isOnceHumanRunning)
                {
                    gameind.Image = Properties.Resources.good;
                }
                else
                {
                    gameind.Image = Properties.Resources.error;
                }
            }));
        }

        // Importing the necessary functions from the Windows API
        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll", ExactSpelling = true, PreserveSig = true, SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll", ExactSpelling = true, PreserveSig = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true, PreserveSig = true, SetLastError = true)]
        public static extern IntPtr SelectObject(IntPtr hDC, IntPtr h);

        [DllImport("gdi32.dll", ExactSpelling = true, PreserveSig = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(IntPtr hObject);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;

            public POINT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SIZE
        {
            public int cx;
            public int cy;

            public SIZE(int cx, int cy)
            {
                this.cx = cx;
                this.cy = cy;
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_LAYERED = 0x80000;
        public const int ULW_ALPHA = 2;
        public const byte AC_SRC_OVER = 0;
        public const byte AC_SRC_ALPHA = 1;

        // Importing the necessary functions from the Windows API
        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);


        public static class Keyboard
        {
            [DllImport("user32.dll")]
            public static extern short GetAsyncKeyState(Keys vKey);

            public static bool IsKeyDown(Keys key)
            {
                return (GetAsyncKeyState(key) & 0x8000) == 0x8000;
            }
        }


        private const int HOTKEY_ID_F5 = 1;
        private const int HOTKEY_ID_F6 = 2;

        private void Form1_Load(object sender, EventArgs e)
        {
            // Registering the F5 and F6 hotkeys
            RegisterHotKey(this.Handle, HOTKEY_ID_F5, MOD_NOREPEAT, (uint)Keys.F5);
            RegisterHotKey(this.Handle, HOTKEY_ID_F6, MOD_NOREPEAT, (uint)Keys.F6);
            Console.WriteLine("Hotkeys registered: F5 and F6");
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            if (m.Msg == WM_HOTKEY)
            {
                int hotkeyId = m.WParam.ToInt32();
                if (hotkeyId == HOTKEY_ID_F6) // HOTKEY_ID_F6 corresponds to F6
                {
                    Console.WriteLine("F6 pressed: Stopping playback");
                    StopMidiPlayback(true);
                    return;
                }
                if (hotkeyId == HOTKEY_ID_F5) // HOTKEY_ID_F5 corresponds to F5
                {
                    Console.WriteLine("F5 pressed: Starting playback");
                    StartMidiPlayback();
                    return;
                }
            }
            base.WndProc(ref m);
        }

        // Methods for handling mouse down and up events
        private void MouseDownHandler(object sender, MouseEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                button.BackColor = Color.LightSkyBlue;
            }
        }

        private void MouseUpHandler(object sender, MouseEventArgs e)
        {
            Button button = sender as Button;
            if (button != null)
            {
                button.BackColor = SystemColors.Control;
                // For number keys, restore text color as well
                if (button == TWO || button == THREE || button == FIVE || button == SIX || button == SEVEN)
                {
                    button.ForeColor = Color.White;
                    button.BackColor = Color.Black;
                }
            }
        }

        private void ScrollTimer_Tick(object sender, EventArgs e)
        {
            if (isPaused)
            {
                pauseCounter++;
                if (pauseCounter >= 5) // Approximately 1 second if interval is 200ms
                {
                    isPaused = false;
                    pauseCounter = 0;
                }
            }
            else
            {
                int textWidth = TextRenderer.MeasureText(originalText, loadedsong.Font).Width;
                if (textWidth > loadedsong.Width)
                {
                    if (scrollPosition >= originalText.Length)
                    {
                        isPaused = true;
                        scrollPosition = 0;
                    }
                    else
                    {
                        string displayText = originalText.Substring(scrollPosition);
                        if (TextRenderer.MeasureText(displayText, loadedsong.Font).Width < loadedsong.Width)
                        {
                            displayText += " " + originalText;
                        }
                        loadedsong.Text = displayText;
                        scrollPosition++;
                    }
                }
                else
                {
                    loadedsong.Text = originalText; // Reset to original text if it fits
                }
            }
        }

        private void DetectMidiDevices()
        {
            int deviceCount = MidiIn.NumberOfDevices;
            if (deviceCount > 0)
            {
                midiIn = new MidiIn(0); // Assuming we use the first MIDI device found
                midiIn.MessageReceived += MidiIn_MessageReceived;
                midiIn.ErrorReceived += MidiIn_ErrorReceived;
                midiIn.Start();
                pianoind.Image = Properties.Resources.good; // Assuming the image is named good.png in Resources
            }
            else
            {
                pianoind.Image = Properties.Resources.error; // Assuming the image is named error.png in Resources
            }
        }

        private void MidiIn_MessageReceived(object sender, MidiInMessageEventArgs e)
        {
            if (e.MidiEvent is NoteOnEvent noteOn)
            {
                int noteNumber = noteOn.NoteNumber;
                if (MidiKeyMap.ContainsKey(noteNumber))
                {
                    var keyMapping = MidiKeyMap.MidiToKey[noteNumber];
                    foreach (var key in keyMapping)
                    {
                        inputSimulator.Keyboard.KeyPress(key);
                    }
                }
            }
        }

        private void MidiIn_ErrorReceived(object sender, MidiInMessageEventArgs e)
        {
            // Handle MIDI input errors if necessary
        }

        private void tracktoggle_Click(object sender, EventArgs e)
        {
            playAllTracks = !playAllTracks;

            if (playAllTracks)
            {
                playalltracks.Image = Properties.Resources.good; // Assuming good.png indicates play all tracks is on
                currenttrack.Text = "All Tracks";
            }
            else
            {
                playalltracks.Image = Properties.Resources.off; // Assuming off.png indicates play all tracks is off
                UpdateCurrentTrackLabel();
            }
        }

        private void prevtrack_Click(object sender, EventArgs e)
        {
            if (!playAllTracks)
            {
                currentTrackIndex--;
                if (currentTrackIndex < 0)
                {
                    currentTrackIndex = totalTracks - 1; // Wrap around to the last track
                }
                UpdateCurrentTrackLabel();
            }
        }

        private void nexttrack_Click(object sender, EventArgs e)
        {
            if (!playAllTracks)
            {
                currentTrackIndex++;
                if (currentTrackIndex >= totalTracks)
                {
                    currentTrackIndex = 0; // Wrap around to the first track
                }
                UpdateCurrentTrackLabel();
            }
        }

        private void UpdateCurrentTrackLabel()
        {
            currenttrack.Text = $"Track {currentTrackIndex + 1}/{totalTracks}";
        }

        private void speedScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            // Calculate the speed as a percentage (50% to 150%)
            double speed = 0.5 + (speedScrollBar.Value / 100.0);
            Speed.Text = $"Speed: {(int)(speed * 100)}%";
        }

        private void SetButtonDownColor(Button button)
        {
            if (button.InvokeRequired)
            {
                button.Invoke(new Action(() => {
                    button.BackColor = Color.LightSkyBlue;
                    if (button == TWO || button == THREE || button == FIVE || button == SIX || button == SEVEN)
                    {
                        button.BackColor = Color.White;
                    }
                }));
            }
            else
            {
                button.BackColor = Color.LightSkyBlue;
                if (button == TWO || button == THREE || button == FIVE || button == SIX || button == SEVEN)
                {
                    button.BackColor = Color.White;
                }
            }
        }

        private void SetButtonUpColor(Button button)
        {
            if (button.InvokeRequired)
            {
                button.Invoke(new Action(() => {
                    button.BackColor = SystemColors.Control;
                    // For number keys, restore text color as well
                    if (button == TWO || button == THREE || button == FIVE || button == SIX || button == SEVEN)
                    {
                        button.ForeColor = Color.White;
                        button.BackColor = Color.Black;
                    }
                }));
            }
            else
            {
                button.BackColor = SystemColors.Control;
                // For number keys, restore text color as well
                if (button == TWO || button == THREE || button == FIVE || button == SIX || button == SEVEN)
                {
                    button.ForeColor = Color.White;
                    button.BackColor = Color.Black;
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Unregistering the hotkeys when the form is closing
            UnregisterHotKey(this.Handle, HOTKEY_ID_F5);
            UnregisterHotKey(this.Handle, HOTKEY_ID_F6);

            // Dispose of the MIDI input device
            midiIn?.Dispose();
        }

        private void StartMidiPlayback()
        {
            if (!isPlaying && midiFile != null)
            {
                isPlaying = true;
                Console.WriteLine("Playback started");
                cancellationTokenSource = new CancellationTokenSource();
                var token = cancellationTokenSource.Token;
                Task.Run(() => PlayMidi(midiFile, token));
            }
        }

        private void StopMidiPlayback(bool forceStop = false)
        {
            if (forceStop)
            {
                // Terminate the task forcefully
                cancellationTokenSource?.Cancel();
                Thread.Sleep(100); // Give it a moment to stop
            }

            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel(); // Cancel the playback task
                isPlaying = false;
                midiFile = null; // Clear the loaded MIDI file

                // Release any pressed keys
                if (currentModifier.HasValue)
                {
                    inputSimulator.Keyboard.KeyUp(currentModifier.Value);
                    currentModifier = null;
                }

                // Reset button colors (if any were pressed during playback)
                SetButtonUpColor(shift);
                SetButtonUpColor(control);

                // Update the UI
                this.Invoke(new Action(() =>
                {
                    library.ClearSelected(); // Deselect any selected item in the library
                    loadedsong.Text = "No Midi Selected"; // Reset the loadedsong label
                    scrollTimer.Stop(); // Stop the scroll timer
                    scrollPosition = 0; // Reset the scroll position
                }));

                Console.WriteLine("Playback stopped");
            }
        }

        private void PlayMidi(MidiFile midiFile, CancellationToken token)
        {
            try
            {
                int ticksPerQuarterNote = midiFile.DeltaTicksPerQuarterNote;
                int tempo = 500000; // Default tempo (microseconds per quarter note)

                // Calculate the speed as a percentage (50% to 150%)
                double speed = 0.5 + (speedScrollBar.Value / 100.0);

                // Find initial tempo
                foreach (var track in midiFile.Events)
                {
                    foreach (MidiEvent midiEvent in track)
                    {
                        if (midiEvent is MetaEvent metaEvent && metaEvent.MetaEventType == MetaEventType.SetTempo)
                        {
                            tempo = ((TempoEvent)metaEvent).MicrosecondsPerQuarterNote;
                            break;
                        }
                    }
                }

                List<(MidiEvent midiEvent, int absoluteTime)> allEvents = new List<(MidiEvent, int)>();
                int[] absoluteTimes = new int[midiFile.Tracks]; // Array to keep track of the absolute time for each track

                // Collect all events with their absolute times
                for (int trackIndex = 0; trackIndex < midiFile.Events.Tracks; trackIndex++)
                {
                    // Only add events from the current track if playAllTracks is false
                    if (playAllTracks || trackIndex == currentTrackIndex)
                    {
                        foreach (MidiEvent midiEvent in midiFile.Events[trackIndex])
                        {
                            allEvents.Add((midiEvent, absoluteTimes[trackIndex]));
                            absoluteTimes[trackIndex] += midiEvent.DeltaTime;
                        }
                    }
                }

                // Sort events by their absolute time
                allEvents.Sort((x, y) => x.absoluteTime.CompareTo(y.absoluteTime));

                int lastTime = 0;
                foreach (var (midiEvent, absoluteTime) in allEvents)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    // Check if F6 has been pressed to stop playback
                    if (Keyboard.IsKeyDown(Keys.F6))
                    {
                        StopMidiPlayback(true);
                        return;
                    }

                    int delay = (int)((absoluteTime - lastTime) * (tempo / ticksPerQuarterNote) / 1000 / speed); // Adjust delay based on speed
                    Thread.Sleep(delay);
                    lastTime = absoluteTime;

                    if (midiEvent is NoteOnEvent noteOn && noteOn.CommandCode == MidiCommandCode.NoteOn)
                    {
                        if (MidiKeyMap.MidiToKey.ContainsKey(noteOn.NoteNumber) && noteOn.NoteName.Length < 4)
                        {
                            var keyMapping = MidiKeyMap.MidiToKey[noteOn.NoteNumber];

                            // Trigger the button press for visual feedback
                            switch (keyMapping[keyMapping.Count - 1])
                            {
                                case VirtualKeyCode.VK_Q: SetButtonDownColor(Q); Task.Delay(100).ContinueWith(_ => SetButtonUpColor(Q)); break;
                                case VirtualKeyCode.VK_W: SetButtonDownColor(W); Task.Delay(100).ContinueWith(_ => SetButtonUpColor(W)); break;
                                case VirtualKeyCode.VK_E: SetButtonDownColor(E); Task.Delay(100).ContinueWith(_ => SetButtonUpColor(E)); break;
                                case VirtualKeyCode.VK_R: SetButtonDownColor(R); Task.Delay(100).ContinueWith(_ => SetButtonUpColor(R)); break;
                                case VirtualKeyCode.VK_T: SetButtonDownColor(T); Task.Delay(100).ContinueWith(_ => SetButtonUpColor(T)); break;
                                case VirtualKeyCode.VK_Y: SetButtonDownColor(Y); Task.Delay(100).ContinueWith(_ => SetButtonUpColor(Y)); break;
                                case VirtualKeyCode.VK_U: SetButtonDownColor(U); Task.Delay(100).ContinueWith(_ => SetButtonUpColor(U)); break;
                                case VirtualKeyCode.VK_2: SetButtonDownColor(TWO); Task.Delay(100).ContinueWith(_ => SetButtonUpColor(TWO)); break;
                                case VirtualKeyCode.VK_3: SetButtonDownColor(THREE); Task.Delay(100).ContinueWith(_ => SetButtonUpColor(THREE)); break;
                                case VirtualKeyCode.VK_5: SetButtonDownColor(FIVE); Task.Delay(100).ContinueWith(_ => SetButtonUpColor(FIVE)); break;
                                case VirtualKeyCode.VK_6: SetButtonDownColor(SIX); Task.Delay(100).ContinueWith(_ => SetButtonUpColor(SIX)); break;
                                case VirtualKeyCode.VK_7: SetButtonDownColor(SEVEN); Task.Delay(100).ContinueWith(_ => SetButtonUpColor(SEVEN)); break;
                                case VirtualKeyCode.LSHIFT: SetButtonDownColor(shift); Task.Delay(100).ContinueWith(_ => SetButtonUpColor(shift)); break;
                                case VirtualKeyCode.LCONTROL: SetButtonDownColor(control); Task.Delay(100).ContinueWith(_ => SetButtonUpColor(control)); break;
                            }

                            if (keyMapping.Count == 2) // If there is a modifier key
                            {
                                if (currentModifier != keyMapping[0])
                                {
                                    if (currentModifier.HasValue)
                                    {
                                        inputSimulator.Keyboard.KeyUp(currentModifier.Value);
                                    }
                                    inputSimulator.Keyboard.KeyDown(keyMapping[0]);
                                    currentModifier = keyMapping[0];
                                }
                            }
                            else
                            {
                                if (currentModifier.HasValue)
                                {
                                    inputSimulator.Keyboard.KeyUp(currentModifier.Value);
                                    currentModifier = null;
                                }
                            }

                            inputSimulator.Keyboard.KeyPress(keyMapping[keyMapping.Count - 1]);
                        }
                    }
                }

                // Automatically stop playback when the song ends
                ResetAfterPlayback();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }
        }

        private void PressButton(Button button)
        {
            if (button.InvokeRequired)
            {
                button.Invoke(new Action(() => {
                    button.BackColor = Color.LightSkyBlue; // Set color on key down
                    Task.Delay(100).ContinueWith(_ => button.Invoke(new Action(() =>
                    {
                        button.BackColor = SystemColors.Control; // Reset color on key up
                    })));
                }));
            }
            else
            {
                button.BackColor = Color.LightSkyBlue; // Set color on key down
                Task.Delay(100).ContinueWith(_ => button.Invoke(new Action(() =>
                {
                    button.BackColor = SystemColors.Control; // Reset color on key up
                })));
            }
        }

        private void ResetAfterPlayback()
        {
            isPlaying = false;
            midiFile = null;
            if (currentModifier.HasValue)
            {
                inputSimulator.Keyboard.KeyUp(currentModifier.Value);
                currentModifier = null;
            }
            Console.WriteLine("Playback reset after completion");
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Custom drawing logic
            Graphics g = e.Graphics;

            // Draw custom background image
            if (backgroundImage != null)
            {
                g.DrawImage(backgroundImage, new Rectangle(0, 0, this.Width, this.Height));
            }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            // Allow dragging the form
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        // P/Invoke declarations
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HT_CAPTION = 0x2;

        private void browse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                if (folderDialog.ShowDialog() == DialogResult.OK)
                {
                    selectedFolderPath = folderDialog.SelectedPath; // Store the selected folder path
                    string[] midiFiles = Directory.GetFiles(selectedFolderPath, "*.mid");
                    foreach (string midiFile in midiFiles)
                    {
                        library.Items.Add(Path.GetFileName(midiFile)); // Add only file name
                    }

                    // Hide the browse button
                    browse.Visible = false;
                }
            }
        }

        private void library_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (library.SelectedItem != null)
            {
                string selectedFileName = library.SelectedItem.ToString();
                string fullPath = Path.Combine(selectedFolderPath, selectedFileName); // Combine the folder path with the selected file name
                loadedsong.Text = selectedFileName;
                originalText = selectedFileName; // Update the original text
                scrollPosition = 0; // Reset scroll position
                isPaused = true; // Start with an initial pause
                pauseCounter = 0; // Reset pause counter
                isInitialPause = true; // Set initial pause
                scrollTimer.Start(); // Start the scroll timer

                try
                {
                    midiFile = new MidiFile(fullPath, false); // Load the MIDI file from the correct path
                    totalTracks = midiFile.Events.Tracks; // Update the total number of tracks
                    currentTrackIndex = 0; // Reset to the first track

                    // Update the play all tracks button based on the current state
                    playalltracks.Image = playAllTracks ? Properties.Resources.good : Properties.Resources.off;
                    currenttrack.Text = playAllTracks ? "All Tracks" : $"Track {currentTrackIndex + 1}/{totalTracks}";

                    Console.WriteLine("MIDI file loaded: " + fullPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error loading MIDI file: " + ex.Message);
                }
            }
            else
            {
                // Ensure the label resets correctly when no item is selected
                loadedsong.Text = "No Midi Selected";
            }
        }
    }
}
