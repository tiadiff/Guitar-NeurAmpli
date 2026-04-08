# 🎸 NeurAmpli: Guitar DSP Amplifier

[![License](https://badgen.net/github/license/tiadiff/NeurAmpli)](https://github.com/tiadiff/NeurAmpli/blob/main/LICENSE)

**NeurAmpli** is a minimal & powerful virtual guitar amplifier (Amp Simulator) written natively in VB.NET. By pushing the **NAudio** architecture to its limits, it transforms a dry input signal from your audio interface into a compact, saturated, and musically rich even-harmonic tube tone with near-zero latency.

<img width="918" height="365" alt="253" src="https://github.com/user-attachments/assets/d41367ba-0cfc-4917-b810-b778fbb5e847" />

## 🌟 Key Features & Strengths

- **⚡ Dynamic WASAPI Engine (Exclusive/Shared):** A dedicated UI toggle allows you to seamlessly switch between **Exclusive Mode** (completely bypassing the Windows native mixer for microscopic 10ms output buffers and maximum real-time responsiveness) and **Shared Mode** (perfect for practicing over YouTube backing tracks or Spotify with ~15ms latency). The stream gracefully auto-restarts upon switching without breaking the UI.
  
- **📈 192kHz Implicit Oversampling:** Internal DSP engine tested to run natively at `192,000Hz`. Operating at this extreme frequency not only offers crystalline precision but naturally prevents catastrophic digital aliasing ("fizz") when generating extreme high-gain distortion.
  
- **🔥 Asymmetric Tube Simulation with 2x Oversampling:** The distortion (Drive) stage doesn't rely on artificial symmetric clipping. A **Parametric DC Tube Bias offset** is applied within the `Math.Tanh` transfer function. It's now heavily upgraded with **internal 2x Oversampling** (anti-imaging and anti-aliasing filters) to perfectly emulate the color and warmth of true thermionic vacuum tubes without generating high-frequency digital "fizz" artifacts.
  
- **🔊 6-Stage Advanced Cabinet Simulator:** Instead of a basic low-pass filter, the Cab Sim algorithm mimics the acoustic mass and air movement of a large 4x12 Studio Cabinet using cascaded `BiQuadFilter` instances:
  - **75Hz HPF:** Removes subsonic "boom" for a tight low-end.
  - **100Hz Peaking EQ:** Simulates the resonant body of a large closed-back wood cabinet.
  - **4.8kHz Cascade LPF:** Cuts the harsh top end while letting the tone breathe.
  - **3.5kHz Presence Notch:** Beautifully captures the distinct mid-scoop of guitar speaker cones.
  - **6.0kHz Rolloff:** Ensures a natural smooth fade-out matching real microphones off-axis.
    
- **🎛️ Comprehensive DSP FX Chain (With Dynamic Tweaker Rack):** ALL effects are meticulously calculated to be completely **Sample-Rate Independent**.
  - **Soft-Knee Noise Gate** (Exponential dial curve, time constants auto-scaling with sample rate).
  - **Dynamic VCA Compressor** with Envelope Tracking (Adjustable `Ratio` and `Threshold`).
  - **LFO Modulated Chorus** highly upgraded with **Cubic Hermite Interpolation** for extremely clean (artifact-free) delay modulation.
  - **Dark Analog Tape Delay** (Low-pass filtered feedback loop for a degrading BBD tone).
  - **8-Tap Diffusion Reverb** upgraded with High-Frequency Damping to absorb treble in the tail, simulating real acoustic rooms/springs.
  - **LFO Tremolo** (Adjustable `Rate` and `Depth`).
    
- **💾 Thread-Safe WAV Recording:** A dedicated button allows for surgical high-resolution recording (wet loop) by asynchronously dumping massive buffer *chunks* in the background. It utilizes strict `SyncLock` synchronization to guarantee thread-safe stability without audio dropouts or memory leaks.
  
- **🎨 Fluid UI:** Integrated fast-presets, a responsive floating-point hardware-style VUMeter, and smooth borderless dragging governed directly by Windows Native calls (WM_NCLBUTTONDOWN).

## 🚀 Quick Start / Usage
1. Select the hardware input receiving your guitar from the dropdown menu.
2. Click **ON** (Boots with a `Clean` tone preset).
3. Feel free to tweak the Gain, EQ, or play with the built-in Presets (`Crunch`, `Heavy Metal`) in real-time!
4. **Custom Presets:** Tweak your tone to perfection, right-click any `USR` slot to save, and left-click it later to instantly recall your setup.

## 🛠️ Built With
- **Visual Studio / VB.NET**
- **NAudio API** (`WasapiOut`, `WaveInEvent`, DSP `BiQuadFilter`, `BufferedWaveProvider`)
- **Win32 API Integrations** (User32.dll) for borderless window dragging events.

## 📝 License
MIT License - Open Source Educational/Musical Project. Feel free to use these DSP functions as a foundation to build more complex VSTs or standalone pedalboards.
