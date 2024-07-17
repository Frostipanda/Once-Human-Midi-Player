import mido
import keyboard as kb
import tkinter as tk
from tkinter import filedialog, messagebox
from tkinter.ttk import Progressbar
import requests
import webbrowser
from threading import Thread
import time
import os
import sys

CURRENT_VERSION = "0.29"
VERSION_URL = "https://raw.githubusercontent.com/Frostipanda/Once-Human-Midi-Player/main/version.txt"
UPDATE_URL = "https://github.com/Frostipanda/Once-Human-Midi-Player/tree/main"
CHANGELOG_URL = "https://github.com/Frostipanda/Once-Human-Midi-Player/blob/main/changelog.txt"
DISCORD_INVITE_URL = "https://discord.gg/bSeZ8EDYAj"

def resource_path(relative_path):
    """ Get absolute path to resource, works for dev and for PyInstaller """
    try:
        base_path = sys._MEIPASS
    except Exception:
        base_path = os.path.abspath(".")
    return os.path.join(base_path, relative_path)

class Logger:
    def __init__(self):
        self.log_window = None
        self.log_text = None

    def open_log_window(self):
        if self.log_window:
            return
        self.log_window = tk.Toplevel(app)
        self.log_window.title("Debug Log")
        self.log_window.geometry("600x400")
        self.log_text = tk.Text(self.log_window, state=tk.DISABLED)
        self.log_text.pack(expand=True, fill=tk.BOTH)
        self.log_text.config(state=tk.NORMAL)
        self.log_text.insert(tk.END, "Debug log initialized...\n")
        self.log_text.config(state=tk.DISABLED)
        self.log_window.protocol("WM_DELETE_WINDOW", self.close_log_window)

    def close_log_window(self):
        self.log_window.destroy()
        self.log_window = None

    def log(self, message):
        print(message)  # Print to the console as well
        if self.log_window and self.log_text:
            self.log_text.config(state=tk.NORMAL)
            self.log_text.insert(tk.END, f"{message}\n")
            self.log_text.see(tk.END)
            self.log_text.config(state=tk.DISABLED)

logger = Logger()

def check_for_update():
    logger.log("Checking for updates...")
    try:
        response = requests.get(VERSION_URL)
        response.raise_for_status()
        latest_version = response.text.strip()
        logger.log(f"Latest version: {latest_version}")
        if latest_version > CURRENT_VERSION:
            if messagebox.askyesno("Update Available", "There is a new version available. Would you like to update?"):
                webbrowser.open(UPDATE_URL)
    except requests.RequestException as e:
        logger.log(f"Error in check_for_update: {e}")
        messagebox.showerror("Error", f"Failed to check for updates: {e}")

def map_piano_note_to_key(note, pitch_adjustment=0):
    logger.log(f"Mapping piano note to key: note={note}, pitch_adjustment={pitch_adjustment}")
    piano_G = ['ctrl', None, 'shift']
    piano_keymap = ['q', '2', 'w', '3', 'e', 'r', '5', 't', '6', 'y', '7', 'u', 'q', '3', 'w', '4', 'e', 'r', '5', 't', '6', 'y', '7', 'u', '=']

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

def release_all_keys(last_change_G):
    logger.log(f"Releasing all keys, last_change_G={last_change_G}")
    if last_change_G:
        kb.release(last_change_G)

def reset_progress():
    logger.log("Resetting progress")
    progress_var.set(0)
    progress_label.config(text="Currently Playing: None")
    current_time_label.config(text="0:00")
    total_time_label.config(text="0:00")

def update_progress(elapsed_ticks, total_ticks, tempo, ticks_per_beat):
    logger.log(f"Updating progress: elapsed_ticks={elapsed_ticks}, total_ticks={total_ticks}")
    progress = int((elapsed_ticks / total_ticks) * 100)
    progress_var.set(progress)
    elapsed_seconds = mido.tick2second(elapsed_ticks, ticks_per_beat, tempo)
    formatted_elapsed_time = format_time(elapsed_seconds)
    current_time_label.config(text=formatted_elapsed_time)

def play_midi(midi, track_index=0, play_all_tracks=False, pitch_modulation=0, pitch_adjustment=0, hold_delay=0.02):
    logger.log(f"Playing MIDI: track_index={track_index}, play_all_tracks={play_all_tracks}, pitch_modulation={pitch_modulation}, pitch_adjustment={pitch_adjustment}, hold_delay={hold_delay}")
    start_time = time.time()
    
    # Collect all messages from all tracks and sort them by time
    all_messages = []
    if play_all_tracks:
        for track in midi.tracks:
            elapsed_time = 0
            for msg in track:
                elapsed_time += msg.time
                all_messages.append((elapsed_time, msg))
    else:
        elapsed_time = 0
        for msg in midi.tracks[track_index]:
            elapsed_time += msg.time
            all_messages.append((elapsed_time, msg))
    
    all_messages.sort(key=lambda x: x[0])
    total_ticks = all_messages[-1][0] if all_messages else 0
    
    elapsed_ticks = 0
    last_change_G = None
    last_key = None
    speed = speed_var.get() / 100.0  # Get the speed from the slider

    logger.log(f"Total ticks: {total_ticks}")

    # Default tempo
    tempo = 500000  # Default tempo in microseconds per beat (120 BPM)
    ticks_per_beat = midi.ticks_per_beat

    try:
        for msg_time, msg in all_messages:
            if kb.is_pressed('F6'):
                release_all_keys(last_change_G)
                reset_progress()
                logger.log("Playback stopped by user (F6 pressed)")
                return False

            if msg.type == 'set_tempo':
                tempo = msg.tempo
                logger.log(f"Tempo change: {tempo} microseconds per beat")

            if not msg.is_meta:
                delay = mido.tick2second(msg_time - elapsed_ticks, ticks_per_beat, tempo) / speed
                logger.log(f"Message: {msg}, delay: {delay} seconds")

                elapsed_ticks = msg_time
                logger.log(f"Elapsed ticks: {elapsed_ticks}")

                update_progress(elapsed_ticks, total_ticks, tempo, ticks_per_beat)
                progress_label.config(text=f"Currently Playing: {os.path.basename(midi_path)}")
                app.update_idletasks()

                if msg.type == 'note_on' and msg.velocity != 0:
                    change_G, key = map_piano_note_to_key(msg.note + pitch_modulation, pitch_adjustment)

                    # Release the last change_G if different from the current one
                    if change_G != last_change_G:
                        if last_change_G:
                            kb.release(last_change_G)
                            if last_change_G == "shift":
                                shift_button.config(bg="white", fg="black")
                            elif last_change_G == "ctrl":
                                ctrl_button.config(bg="white", fg="black")
                        if change_G:
                            kb.press(change_G)
                        last_change_G = change_G

                    # Release and re-press the key even if it's the same as the last one
                    if last_key:
                        kb.release(last_key)
                        if last_key in piano_keys:
                            piano_keys[last_key].config(bg="white" if last_key.isalpha() else "black", fg="black" if last_key.isalpha() else "white")
                    if key:
                        kb.press(key)
                    last_key = key

                    # Update virtual keyboard
                    update_virtual_keyboard(change_G, key)

                if delay > 0:
                    logger.log(f"Sleeping for {delay} seconds")
                    time.sleep(delay)

        release_all_keys(last_change_G)
        if last_key:
            kb.release(last_key)
            if last_key in piano_keys:
                piano_keys[last_key].config(bg="white" if last_key.isalpha() else "black", fg="black" if last_key.isalpha() else "white")
        reset_progress()
        logger.log("Playback finished")
    
    except Exception as e:
        release_all_keys(last_change_G)
        if last_key:
            kb.release(last_key)
            if last_key in piano_keys:
                piano_keys[last_key].config(bg="white" if last_key.isalpha() else "black", fg="black" if last_key.isalpha() else "white")
        reset_progress()
        logger.log(f"Error in play_midi: {e}")
        raise e

    return True

def update_virtual_keyboard(change_G, key):
    logger.log(f"Updating virtual keyboard: change_G={change_G}, key={key}")
    gentle_sky_blue = "#87CEEB"
    for button in piano_keys.values():
        if button.cget("bg") == gentle_sky_blue:
            button.config(bg="white" if button.cget("text").isalpha() else "black", fg="black" if button.cget("text").isalpha() else "white")
    if change_G == "shift":
        shift_button.config(bg=gentle_sky_blue, fg="black")
    elif change_G == "ctrl":
        ctrl_button.config(bg=gentle_sky_blue, fg="black")
    if key and key in piano_keys:
        piano_keys[key].config(bg=gentle_sky_blue, fg="black")

def start_playback():
    logger.log("Starting playback")
    try:
        global loop, midi_path
        midi_path = entry_file_path.get()
        pitch_adjustment = 0
        loop = var_loop.get()

        if var_pitch.get() == "+pitch":
            pitch_adjustment = 1
        elif var_pitch.get() == "-pitch":
            pitch_adjustment = -1

        play_all_tracks = var_play_all_tracks.get()
        logger.log(f"Selected track index: {current_track_index}, play_all_tracks: {play_all_tracks}")

        midi = mido.MidiFile(midi_path)
        total_time_label.config(text=format_time(int(midi.length)))
        play_midi(midi, track_index=current_track_index, play_all_tracks=play_all_tracks, pitch_modulation=0, pitch_adjustment=pitch_adjustment, hold_delay=0.02)
    except Exception as e:
        logger.log(f"Error in start_playback: {e}")
        raise e

def select_file():
    logger.log("Selecting file")
    try:
        file_paths = filedialog.askopenfilenames(filetypes=[("MIDI files", "*.mid *.midi")])
        if file_paths:
            entry_file_path.delete(0, tk.END)
            entry_file_path.insert(0, file_paths[0])
            midi = mido.MidiFile(file_paths[0])
            midi_length = midi.length
            formatted_total_time = format_time(int(midi_length))
            total_time_label.config(text=formatted_total_time)
            global current_track_index
            current_track_index = 0
            update_track_label(midi)
    except Exception as e:
        logger.log(f"Error in select_file: {e}")
        messagebox.showerror("Error", f"Failed to load files: {e}")

def format_time(seconds):
    minutes = int(seconds) // 60
    seconds = int(seconds) % 60
    return f"{minutes}:{seconds:02d}"

def on_f5_press(e):
    logger.log("F5 pressed")
    try:
        global playback_thread
        if playback_thread is None or not playback_thread.is_alive():
            playback_thread = Thread(target=start_playback)
            playback_thread.start()
    except Exception as e:
        logger.log(f"Error in on_f5_press: {e}")
        raise e

def on_f6_press(e):
    logger.log("F6 pressed")
    try:
        kb.press('F6')
        kb.release('F6')
        release_all_keys(None)
        reset_progress()
    except Exception as e:
        logger.log(f"Error in on_f6_press: {e}")
        raise e

def open_changelog():
    logger.log("Opening changelog")
    webbrowser.open(CHANGELOG_URL)

def open_discord_invite():
    logger.log("Opening Discord invite")
    webbrowser.open(DISCORD_INVITE_URL)

def toggle_always_on_top():
    logger.log("Toggling always on top")
    try:
        app.attributes("-topmost", var_always_on_top.get())
        playlist_window.attributes("-topmost", var_always_on_top.get())
    except Exception as e:
        logger.log(f"Error in toggle_always_on_top: {e}")
        raise e

def update_playlist_window_position():
    logger.log("Updating playlist window position")
    try:
        app.update_idletasks()
        x = app.winfo_x() + app.winfo_width()
        y = app.winfo_y()
        playlist_window.geometry(f"+{x}+{y}")
    except Exception as e:
        logger.log(f"Error in update_playlist_window_position: {e}")
        messagebox.showerror("Error", f"Failed to update playlist window position: {e}")

def on_main_window_move(event):
    update_playlist_window_position()

def on_playlist_window_move(event):
    update_main_window_position()

def update_main_window_position():
    logger.log("Updating main window position")
    try:
        playlist_window.update_idletasks()
        x = playlist_window.winfo_x() - app.winfo_width()
        y = playlist_window.winfo_y()
        app.geometry(f"+{x}+{y}")
    except Exception as e:
        logger.log(f"Error in update_main_window_position: {e}")
        messagebox.showerror("Error", f"Failed to update main window position: {e}")

def on_playlist_select(event):
    logger.log("Selecting playlist item")
    try:
        selection = playlist_box.curselection()
        if selection:
            midi_name = playlist_box.get(selection[0])
            entry_file_path.delete(0, tk.END)
            entry_file_path.insert(0, playlist_paths[midi_name])
            global current_track_index
            current_track_index = 0
            update_track_label(mido.MidiFile(playlist_paths[midi_name]))
    except Exception as e:
        logger.log(f"Error in on_playlist_select: {e}")
        messagebox.showerror("Error", f"Failed to select playlist item: {e}")

def start_drag(event):
    playlist_box._drag_start_index = playlist_box.nearest(event.y)

def on_drag_motion(event):
    playlist_box._drag_end_index = playlist_box.nearest(event.y)
    if playlist_box._drag_start_index != playlist_box._drag_end_index:
        item = playlist_box.get(playlist_box._drag_start_index)
        playlist_box.delete(playlist_box._drag_start_index)
        playlist_box.insert(playlist_box._drag_end_index, item)
        playlist_box._drag_start_index = playlist_box._drag_end_index

def delete_selected_song(event):
    logger.log("Deleting selected song")
    try:
        selection = playlist_box.curselection()
        if selection:
            midi_name = playlist_box.get(selection[0])
            del playlist_paths[midi_name]
            playlist_box.delete(selection[0])
    except Exception as e:
        logger.log(f"Error in delete_selected_song: {e}")
        messagebox.showerror("Error", f"Failed to delete playlist item: {e}")

def clear_playlist():
    logger.log("Clearing playlist")
    try:
        playlist_box.delete(0, tk.END)
        playlist_paths.clear()
    except Exception as e:
        logger.log(f"Error in clear_playlist: {e}")
        messagebox.showerror("Error", f"Failed to clear playlist: {e}")

def update_track_label(midi):
    track_label.config(text=f"Track {current_track_index + 1}/{len(midi.tracks)}")

def increment_track(midi):
    global current_track_index
    if current_track_index < len(midi.tracks) - 1:
        current_track_index += 1
    update_track_label(midi)

def decrement_track(midi):
    global current_track_index
    if current_track_index > 0:
        current_track_index -= 1
    update_track_label(midi)

app = tk.Tk()
app.title("Once Human MIDI Player")

# Set the window icon
app.iconphoto(False, tk.PhotoImage(file=resource_path('icon.png')))

current_track_index = 0
var_play_all_tracks = tk.BooleanVar()

# Create a dictionary to store full paths
playlist_paths = {}

# Create main frames
file_frame = tk.LabelFrame(app, text="File")
file_frame.pack(padx=10, pady=5, fill="x")

settings_frame = tk.LabelFrame(app, text="Settings")
settings_frame.pack(padx=10, pady=5, fill="x")

controls_frame = tk.LabelFrame(app, text="Controls")
controls_frame.pack(padx=10, pady=5, fill="x")

piano_frame = tk.Frame(app)
piano_frame.pack(pady=10)

status_frame = tk.Frame(app)
status_frame.pack(fill="x")

# File selection
label_file = tk.Label(file_frame, text="MIDI File:")
label_file.grid(row=0, column=0, padx=5, pady=5, sticky="w")
entry_file_path = tk.Entry(file_frame, width=50)
entry_file_path.grid(row=0, column=1, padx=5, pady=5)
button_browse = tk.Button(file_frame, text="Browse", command=select_file)
button_browse.grid(row=0, column=2, padx=5, pady=5)

# Track selection
label_track = tk.Label(file_frame, text="Track:")
label_track.grid(row=1, column=0, padx=5, pady=5, sticky="w")
track_label = tk.Label(file_frame, text="Track 1")
track_label.grid(row=1, column=1, padx=5, pady=5, sticky="w")
button_track_up = tk.Button(file_frame, text="↑", command=lambda: increment_track(mido.MidiFile(entry_file_path.get())))
button_track_up.grid(row=1, column=2, padx=2, pady=5, sticky="w")
button_track_down = tk.Button(file_frame, text="↓", command=lambda: decrement_track(mido.MidiFile(entry_file_path.get())))
button_track_down.grid(row=1, column=3, padx=2, pady=5, sticky="w")
check_play_all_tracks = tk.Checkbutton(file_frame, text="Play All Tracks", variable=var_play_all_tracks)
check_play_all_tracks.grid(row=1, column=4, padx=2, pady=5, sticky="w")

# Settings
var_pitch = tk.StringVar(value="Normal")
label_pitch = tk.Label(settings_frame, text="Pitch Adjustment:")
label_pitch.grid(row=0, column=0, padx=5, pady=5, sticky="w")
radio_normal = tk.Radiobutton(settings_frame, text="Normal", variable=var_pitch, value="Normal")
radio_normal.grid(row=0, column=1, padx=5, pady=5, sticky="w")
radio_pitch_up = tk.Radiobutton(settings_frame, text="+Pitch", variable=var_pitch, value="+pitch")
radio_pitch_up.grid(row=0, column=2, padx=5, pady=5)
radio_pitch_down = tk.Radiobutton(settings_frame, text="-Pitch", variable=var_pitch, value="-pitch")
radio_pitch_down.grid(row=0, column=3, padx=5, pady=5, sticky="w")

var_loop = tk.BooleanVar()
check_loop = tk.Checkbutton(settings_frame, text="Loop", variable=var_loop)
check_loop.grid(row=0, column=4, padx=5, pady=5)

label_speed = tk.Label(settings_frame, text="Speed:")
label_speed.grid(row=0, column=5, padx=5, pady=5)
speed_var = tk.IntVar(value=100)
speed_slider = tk.Scale(settings_frame, from_=50, to=150, orient=tk.HORIZONTAL, variable=speed_var)
speed_slider.grid(row=0, column=6, padx=5, pady=5)

var_always_on_top = tk.BooleanVar()
check_always_on_top = tk.Checkbutton(settings_frame, text="Always on Top", variable=var_always_on_top, command=toggle_always_on_top)
check_always_on_top.grid(row=0, column=7, padx=5, pady=5)

# Controls
instructions_label = tk.Label(controls_frame, text="Press F5 to start playing the MIDI file. Press F6 to stop playing.")
instructions_label.grid(row=0, column=0, columnspan=2, padx=5, pady=5, sticky="w")

# Piano keys
control_frame = tk.Frame(piano_frame)
control_frame.pack(side=tk.LEFT, padx=1)

shift_button = tk.Button(control_frame, text="Shift", width=3, height=4, bg='lightgray')
shift_button.pack(side=tk.TOP, padx=1)
ctrl_button = tk.Button(control_frame, text="Ctrl", width=3, height=4, bg='lightgray')
ctrl_button.pack(side=tk.BOTTOM, padx=1)

piano_keys = {}
piano_keymap = ['Q', '2', 'W', '3', 'E', 'R', '5', 'T', '6', 'Y', '7', 'U']

for key in piano_keymap:
    if key in ['2', '3', '5', '6', '7']:
        button = tk.Button(piano_frame, text=key, width=3, height=8, bg='black', fg='white')
    else:
        button = tk.Button(piano_frame, text=key, width=3, height=8, bg='white', fg='black')
    button.pack(side=tk.LEFT, padx=1)
    piano_keys[key] = button

# Status bar
progress_var = tk.IntVar()
progress_bar = Progressbar(status_frame, variable=progress_var, maximum=100)
progress_bar.pack(fill=tk.X, padx=10, pady=5)

status_time_frame = tk.Frame(status_frame)
status_time_frame.pack(fill="x")

current_time_label = tk.Label(status_time_frame, text="0:00")
current_time_label.pack(side="left", padx=10)

total_time_label = tk.Label(status_time_frame, text="0:00")
total_time_label.pack(side="right", padx=10)

progress_label = tk.Label(status_frame, text="Currently Playing: None")
progress_label.pack(pady=5)

# Discord button
discord_button_image = tk.PhotoImage(file=resource_path("button.png"))
button_discord = tk.Button(app, image=discord_button_image, command=open_discord_invite)
button_discord.pack(side="bottom", anchor="e", padx=5, pady=5)

# Version and Help
version_label = tk.Label(app, text=f"Current version v{CURRENT_VERSION}")
version_label.pack(side="bottom", anchor="e", padx=5, pady=5)

button_help = tk.Button(app, text="?", command=open_changelog)
button_help.pack(side="bottom", anchor="w", padx=5, pady=5)

playback_thread = None

# Check for updates when the application starts
check_for_update()

# Register F5 and F6 keys for global hotkeys
kb.on_press_key('f5', on_f5_press)
kb.on_press_key('f6', on_f6_press)

# Playlist window
playlist_window = tk.Toplevel(app)
playlist_window.title("Playlist")
playlist_window.protocol("WM_DELETE_WINDOW", lambda: var_playlist_mode.set(False))
playlist_window.withdraw()
playlist_window.attributes("-toolwindow", 1)  # Hide maximize button

playlist_box = tk.Listbox(playlist_window, selectmode=tk.SINGLE)
playlist_box.pack(fill=tk.BOTH, expand=True, padx=10, pady=10)
playlist_box.bind('<<ListboxSelect>>', on_playlist_select)
playlist_box.bind('<Button-1>', start_drag)
playlist_box.bind('<B1-Motion>', on_drag_motion)
playlist_box.bind('<Delete>', delete_selected_song)

clear_playlist_button = tk.Button(playlist_window, text="Clear Playlist", command=clear_playlist)
clear_playlist_button.pack(side="bottom", pady=5, padx=5, anchor="e")

# Update the window to get its final size after all widgets are packed
app.update_idletasks()
width = app.winfo_width()
height = app.winfo_height()
app.minsize(width, height)
app.maxsize(width, height)

# Disable resizing of the window
app.resizable(False, False)

# Adjust playlist window size
playlist_window.geometry(f"{width//2}x{height}")

# Bind window move events
app.bind("<Configure>", on_main_window_move)
playlist_window.bind("<Configure>", on_playlist_window_move)

# Register Shift+F4 to open the debug log window
kb.add_hotkey('shift+f4', logger.open_log_window)

app.mainloop()
