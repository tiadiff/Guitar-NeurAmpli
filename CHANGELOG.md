### Added
- **Dynamic FX Tweaker Rack**: A brand new smart sidebar that dynamically reveals custom hardware-style rotary knobs to fine-tune individual effects (Chorus, Delay, Tremolo, Reverb, Compressor).
- **Deep DSP Parameter Mapping**: Full control over specific algorithm variables including `Rate`, `Depth`, `Time`, `Feedback`, `Mix`, `Threshold`, and `Ratio`.
- **Dual-Action Toggles**: New `RockSwitch` design allows left-clicking the pill to toggle the effect ON/OFF, and clicking the label text to focus the effect inside the Tweaker Rack.
- **Safety Master Limiter**: Implemented a global `Math.Tanh()` soft-clipper right before the WasapiOut buffer to gracefully catch extreme EQ/Drive combinations and physically prevent digital aliasing (crackling/hard-clipping).

### Changed
- **UI Redesign (Neumorphic/Flat)**: Completely overhauled the user interface replacing standard WinForms controls with custom double-buffered vector graphics (`GlassPanel`, `RockKnob`, `RockSwitch`) using a sleek iOS/macOS inspired dark theme.
- **Exponential Noise Gate**: Completely reworked the Noise Gate threshold math. It now uses an exponential curve, offering microscopic sensitivity at low values to gently slice away `-80dB` background hum without cutting off natural note sustain, while retaining high clamping power at maximum settings.
- **DSP Drive Optimization**: Rebalanced the tube bias and asymmetric overdrive pushing algorithm for smoother, deeper saturation without generating harsh high-frequency square-wave artifacts.
- **Triangular Component Layout**: Knobs and indicators dynamically arrange themselves in an optimized side-by-side or triangular cluster to prevent overlapping or cutting off labels in the condensed VST window format.