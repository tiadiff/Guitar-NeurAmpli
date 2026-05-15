## [2.9.8] - 2026-04-09
### Added
- **Backing Track Player**: A dedicated form to load and play MP3/WAV/AIFF files. Includes real-time 192kHz hardware resampling (MediaFoundation) and mono downmixing to integrate perfectly with the DSP engine.
- **Dynamic Signal Chain**: Complete refactor of the audio pipeline. Users can now drag-and-drop the "Virtual Pedals" to reorder effects (Compressor, Drive, Amp, Chorus, Tremolo, Delay, Reverb) in real-time.
- **Noise Gate LED**: Visual feedback on the main panel showing exactly when the gate is active/silencing the signal.
- **Studio Rack Layout**: Automatic snap-positioning of all satellite forms. The app now detects screen bounds to prevent clipping on 1080p monitors and avoids the Windows Taskbar.
- **Independent Metronome & Looper Volume**: Fine-tuned gain stages for practicing. The Looper now features a 2.5x volume boost to prevent it from being masked by live playing.

### Changed
- Refactored `GuitarAmpEffect` core loop to support dynamic `FXType` arrays.
- Switched Signal Chain "Source of Truth" to `Form1` for persistence even when the DSP engine is stopped.
- Optimized form start positions to `Manual` with relative coordinate tracking for a "docked" UI feel.