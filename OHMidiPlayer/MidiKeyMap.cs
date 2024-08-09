using System.Collections.Generic;
using WindowsInput.Native;

namespace OHMidiPlayer
{
    public static class MidiKeyMap
    {
        private static readonly Dictionary<int, List<VirtualKeyCode>> _midiToKeyMap = new Dictionary<int, List<VirtualKeyCode>>
        {
            { 48, new List<VirtualKeyCode> { VirtualKeyCode.LCONTROL, VirtualKeyCode.VK_Q } },  // C3
            { 49, new List<VirtualKeyCode> { VirtualKeyCode.LCONTROL, VirtualKeyCode.VK_2 } },  // C#3
            { 50, new List<VirtualKeyCode> { VirtualKeyCode.LCONTROL, VirtualKeyCode.VK_W } },  // D3
            { 51, new List<VirtualKeyCode> { VirtualKeyCode.LCONTROL, VirtualKeyCode.VK_3 } },  // D#3
            { 52, new List<VirtualKeyCode> { VirtualKeyCode.LCONTROL, VirtualKeyCode.VK_E } },  // E3
            { 53, new List<VirtualKeyCode> { VirtualKeyCode.LCONTROL, VirtualKeyCode.VK_R } },  // F3
            { 54, new List<VirtualKeyCode> { VirtualKeyCode.LCONTROL, VirtualKeyCode.VK_5 } },  // F#3
            { 55, new List<VirtualKeyCode> { VirtualKeyCode.LCONTROL, VirtualKeyCode.VK_T } },  // G3
            { 56, new List<VirtualKeyCode> { VirtualKeyCode.LCONTROL, VirtualKeyCode.VK_6 } },  // G#3
            { 57, new List<VirtualKeyCode> { VirtualKeyCode.LCONTROL, VirtualKeyCode.VK_Y } },  // A3
            { 58, new List<VirtualKeyCode> { VirtualKeyCode.LCONTROL, VirtualKeyCode.VK_7 } },  // A#3
            { 59, new List<VirtualKeyCode> { VirtualKeyCode.LCONTROL, VirtualKeyCode.VK_U } },  // B3

            { 60, new List<VirtualKeyCode> { VirtualKeyCode.VK_Q } },  // C4
            { 61, new List<VirtualKeyCode> { VirtualKeyCode.VK_2 } },  // C#4
            { 62, new List<VirtualKeyCode> { VirtualKeyCode.VK_W } },  // D4
            { 63, new List<VirtualKeyCode> { VirtualKeyCode.VK_3 } },  // D#4
            { 64, new List<VirtualKeyCode> { VirtualKeyCode.VK_E } },  // E4
            { 65, new List<VirtualKeyCode> { VirtualKeyCode.VK_R } },  // F4
            { 66, new List<VirtualKeyCode> { VirtualKeyCode.VK_5 } },  // F#4
            { 67, new List<VirtualKeyCode> { VirtualKeyCode.VK_T } },  // G4
            { 68, new List<VirtualKeyCode> { VirtualKeyCode.VK_6 } },  // G#4
            { 69, new List<VirtualKeyCode> { VirtualKeyCode.VK_Y } },  // A4
            { 70, new List<VirtualKeyCode> { VirtualKeyCode.VK_7 } },  // A#4
            { 71, new List<VirtualKeyCode> { VirtualKeyCode.VK_U } },  // B4

            { 72, new List<VirtualKeyCode> { VirtualKeyCode.LSHIFT, VirtualKeyCode.VK_Q } },  // C5
            { 73, new List<VirtualKeyCode> { VirtualKeyCode.LSHIFT, VirtualKeyCode.VK_2 } },  // C#5
            { 74, new List<VirtualKeyCode> { VirtualKeyCode.LSHIFT, VirtualKeyCode.VK_W } },  // D5
            { 75, new List<VirtualKeyCode> { VirtualKeyCode.LSHIFT, VirtualKeyCode.VK_3 } },  // D#5
            { 76, new List<VirtualKeyCode> { VirtualKeyCode.LSHIFT, VirtualKeyCode.VK_E } },  // E5
            { 77, new List<VirtualKeyCode> { VirtualKeyCode.LSHIFT, VirtualKeyCode.VK_R } },  // F5
            { 78, new List<VirtualKeyCode> { VirtualKeyCode.LSHIFT, VirtualKeyCode.VK_5 } },  // F#5
            { 79, new List<VirtualKeyCode> { VirtualKeyCode.LSHIFT, VirtualKeyCode.VK_T } },  // G5
            { 80, new List<VirtualKeyCode> { VirtualKeyCode.LSHIFT, VirtualKeyCode.VK_6 } },  // G#5
            { 81, new List<VirtualKeyCode> { VirtualKeyCode.LSHIFT, VirtualKeyCode.VK_Y } },  // A5
            { 82, new List<VirtualKeyCode> { VirtualKeyCode.LSHIFT, VirtualKeyCode.VK_7 } },  // A#5
            { 83, new List<VirtualKeyCode> { VirtualKeyCode.LSHIFT, VirtualKeyCode.VK_U } },  // B5
        };

        public static IEnumerable<KeyValuePair<int, List<VirtualKeyCode>>> MidiToKeyMapEnumerable => _midiToKeyMap;
        public static Dictionary<int, List<VirtualKeyCode>> MidiToKey => _midiToKeyMap;
        public static bool ContainsKey(int midiKey) => _midiToKeyMap.ContainsKey(midiKey);
    }
}
