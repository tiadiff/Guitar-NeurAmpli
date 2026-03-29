# 🎸 NeurAmpli: Advanced VB.NET DSP Amplifier

**NeurAmpli** is a minimal yet extremely powerful virtual guitar amplifier (Amp Simulator) written natively in VB.NET. By pushing the **NAudio** architecture to its limits, it transforms a dry input signal from your audio interface into a compact, saturated, and musically rich even-harmonic tube tone with near-zero latency.

## 🌟 Key Features & Strengths

- **⚡ Zero-Latency WASAPI Exclusive:** Completely bypasses the Windows native mixer. `WasapiOut` running in Exclusive Mode allows for microscopic output buffers (down to 10ms), guaranteeing maximum real-time responsiveness under your fingertips while playing.
- **📈 192kHz Implicit Oversampling:** Internal DSP engine tested to run natively at `192,000Hz`. Operating at this extreme frequency not only offers crystalline precision but naturally prevents catastrophic digital aliasing ("fizz") when generating extreme high-gain distortion.
- **🔥 Asymmetric Tube Simulation:** The distortion (Drive) stage does not rely on artificial symmetric clipping. A **Parametric DC Tube Bias offset** is applied within the `Math.Tanh` transfer function. The resulting asymmetrical wave deformation generates abundant and highly musical *even-order harmonics*, perfectly emulating the color and warmth of true thermionic vacuum tubes (like an overdriven 12AX7).
- **🔊 Multi-Stage Cabinet Simulator:** Instead of a basic low-pass filter, the Cab Sim algorithm mimics the acoustic mass and air movement of a large 4x12 Studio Cabinet using cascaded `BiQuadFilter` instances:
  - An **80Hz Butterworth High-Pass** entirely rolls off muddy sub-frequencies, freeing up headroom and tightening the low-end.
  - A **4.5kHz Double Low-Pass cascade** (Linkwitz-Riley 24dB/oct style) surgically slices off harsh digital high-end fizz, perfectly modeling the upper resonance of thick wood and speaker cones (e.g., Celestion V30).
- **🎛️ Comprehensive DSP FX Chain:**
  - Soft-Knee Noise Gate.
  - Dynamic VCA Compressor with Envelope Tracking.
  - Linearly Interpolated LFO Modulated Delay-Chorus.
  - Dark Analog Tape Delay.
  - 4-Tap Prime Number Diffusion Reverb (for smooth tails free of ghost resonances).
  - LFO Tremolo.
- **💾 Thread-Safe WAV Recording:** A dedicated button allows for surgical high-resolution recording (wet loop) by asynchronously dumping massive buffer *chunks* in the background. It utilizes strict `SyncLock` synchronization to guarantee thread-safe stability without audio dropouts or memory leaks.
- **🎨 Fluid UI:** Integrated fast-presets, a responsive floating-point hardware-style VUMeter, and smooth borderless dragging governed directly by Windows Native calls (WM_NCLBUTTONDOWN).

## 🚀 Quick Start / Usage
1. Select the hardware input receiving your guitar from the dropdown menu.
2. Click **Start** (Boots with a `Clean` tone preset).
3. Feel free to tweak the Gain, EQ, or play with the built-in Presets (`Crunch`, `Metal`) in real-time!

## 🛠️ Built With
- **Visual Studio / VB.NET**
- **NAudio API** (`WasapiOut`, `WaveInEvent`, DSP `BiQuadFilter`, `BufferedWaveProvider`)
- **Win32 API Integrations** (User32.dll) for borderless window dragging events.

## 📝 License
MIT License - Open Source Educational/Musical Project. Feel free to use these DSP functions as a foundation to build more complex VSTs or standalone pedalboards.
