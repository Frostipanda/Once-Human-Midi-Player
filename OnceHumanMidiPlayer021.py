import time
import sys
import mido
import keyboard as kb
import tkinter as tk
from tkinter import filedialog, messagebox
from tkinter.ttk import Progressbar
import requests
import webbrowser
from threading import Thread

CURRENT_VERSION = "0.21"
VERSION_URL = "https://raw.githubusercontent.com/Frostipanda/Once-Human-Midi-Player/main/version.txt"
UPDATE_URL = "https://github.com/Frostipanda/Once-Human-Midi-Player/tree/main"

def check_for_update():
    try:
        response = requests.get(VERSION_URL)
        response.raise_for_status()
        latest_version = response.text.strip()
        if latest_version > CURRENT_VERSION:
            if messagebox.askyesno("Update Available", "There is a new version available. Would you like to update?"):
                webbrowser.open(UPDATE_URL)
    except requests.RequestException as e:
        messagebox.showerror("Error", f"Failed to check for updates: {e}")

def map_piano_note_to_key(note, pitch_adjustment=0):
    piano_G = ['ctrl', None, 'shift']
    piano_keymap = ['q', '2', 'w', '3', 'e', 'r', '5', 't', '6', 'y', '7',
                    'u', 'q', '3', 'w', '4', 'e', 'r', '5', 't', '6', 'y', '7', 'u', '=']

    if note < 36:
        note = 36
    elif note > 96:
        note = 96

    if 36 <= note <= 59:
        change_G = piano_G[(0 + pitch_adjustment) % 3]
        baseline = 36
    elif 60 <= note <= 83:
        change_G = piano_G[(1 + pitch_adjustment) % 3]
        baseline = 60
    else:
        change_G = piano_G[(2 + pitch_adjustment) % 3]
        baseline = 84

    key_index = note - baseline

    key = None
    if 0 <= key_index < len(piano_keymap):
        key = piano_keymap[key_index]

    return change_G, key

def play_midi(midi, pitch_modulation=0, pitch_adjustment=0, hold_delay=0.02):
    start_time = time.time()
    total_ticks = sum(msg.time for msg in midi)  # Total time in ticks
    elapsed_ticks = 0

    for msg in midi.play():
        if kb.is_pressed('F6'):
            return False
        if not msg.is_meta:
            elapsed_ticks += msg.time
            progress = int((elapsed_ticks / total_ticks) * 100)
            progress_var.set(progress)
            progress_label.config(text=f"Currently Playing: {midi_path.split('/')[-1]}")
            app.update_idletasks()
            time.sleep(max(0, msg.time - (time.time() - start_time)))
            start_time = time.time()
            if msg.type == 'note_on' and msg.velocity != 0:
                change_G, key = map_piano_note_to_key(msg.note + pitch_modulation, pitch_adjustment)
                
                if change_G:
                    kb.press(change_G)
                    time.sleep(hold_delay)

                if key:
                    kb.press(key)
                    time.sleep(hold_delay)
                    kb.release(key)

                if change_G:
                    kb.release(change_G)
                    time.sleep(hold_delay)
    return True

def start_playback():
    global loop, midi_path
    midi_path = entry_file_path.get()
    pitch_adjustment = 0
    loop = var_loop.get()

    if var_pitch.get() == "+pitch":
        pitch_adjustment = 1
    elif var_pitch.get() == "-pitch":
        pitch_adjustment = -1

    midi = mido.MidiFile(midi_path)

    while True:
        if not play_midi(midi, pitch_modulation=0, pitch_adjustment=pitch_adjustment, hold_delay=0.02):
            break
        if not loop:
            break
        print("Looping in 3 seconds...")
        time.sleep(3)

def select_file():
    file_path = filedialog.askopenfilename(filetypes=[("MIDI files", "*.mid *.midi")])
    if file_path:
        entry_file_path.delete(0, tk.END)
        entry_file_path.insert(0, file_path)

def on_f5_press(e):
    global playback_thread
    if playback_thread is None or not playback_thread.is_alive():
        playback_thread = Thread(target=start_playback)
        playback_thread.start()

def on_f6_press(e):
    kb.press('F6')

app = tk.Tk()
app.title("Once Human MIDI Player")

frame = tk.Frame(app)
frame.pack(pady=20)

label_file = tk.Label(frame, text="MIDI File:")
label_file.grid(row=0, column=0, padx=5, pady=5)
entry_file_path = tk.Entry(frame, width=50)
entry_file_path.grid(row=0, column=1, padx=5, pady=5)
button_browse = tk.Button(frame, text="Browse", command=select_file)
button_browse.grid(row=0, column=2, padx=5, pady=5)

var_pitch = tk.StringVar(value="Normal")
label_pitch = tk.Label(frame, text="Pitch Adjustment:")
label_pitch.grid(row=1, column=0, padx=5, pady=5)
radio_normal = tk.Radiobutton(frame, text="Normal", variable=var_pitch, value="Normal")
radio_normal.grid(row=1, column=1, padx=5, pady=5, sticky="w")
radio_pitch_up = tk.Radiobutton(frame, text="+Pitch", variable=var_pitch, value="+pitch")
radio_pitch_up.grid(row=1, column=1, padx=5, pady=5)
radio_pitch_down = tk.Radiobutton(frame, text="-Pitch", variable=var_pitch, value="-pitch")
radio_pitch_down.grid(row=1, column=1, padx=5, pady=5, sticky="e")

var_loop = tk.BooleanVar()
check_loop = tk.Checkbutton(frame, text="Loop", variable=var_loop)
check_loop.grid(row=2, column=1, padx=5, pady=5, sticky="w")

progress_var = tk.IntVar()
progress_bar = Progressbar(app, variable=progress_var, maximum=100)
progress_bar.pack(fill=tk.X, padx=10, pady=5)

progress_label = tk.Label(app, text="Currently Playing: None")
progress_label.pack(pady=5)

instructions_label = tk.Label(app, text="Press F5 to start playing the MIDI file. Press F6 to stop playing.")
instructions_label.pack(pady=5)

version_label = tk.Label(app, text=f"Current version v{CURRENT_VERSION}")
version_label.pack(side="bottom", anchor="e", padx=5, pady=5)

playback_thread = None

# Check for updates when the application starts
check_for_update()

# Register F5 and F6 keys for global hotkeys
kb.on_press_key('f5', on_f5_press)
kb.on_press_key('f6', on_f6_press)

app.mainloop()
