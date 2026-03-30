# 🎸 NeurAmpli: Guitar DSP Amplifier

**NeurAmpli** is a minimal & powerful virtual guitar amplifier (Amp Simulator) written natively in VB.NET. By pushing the **NAudio** architecture to its limits, it transforms a dry input signal from your audio interface into a compact, saturated, and musically rich even-harmonic tube tone with near-zero latency.

### v2.5.0 (Rack Update)
The entire DSP suite and User Interface have been completely overhauled. 
- Introduces the **Dynamic FX Tweaker Rack**, a smart side-panel that reveals granular DSP controls (Chorus Rate/Depth, Delay Feedback/Mix, Comp Ratio/Threshold) when clicking an effect's name.
- Custom vector-rendered Flat/Neumorphic Glass Panels and Knobs.
- A **Safety Master Soft-Clipper** (Math.Tanh) ensures extreme signal chains never cause digital aliasing or Wasapi hard-clipping.

<img width="918" height="365" alt="Screenshot 2026-03-31 011236" src="https://github.com/user-attachments/assets/129d1307-3181-4da9-9726-18bce0caee89" />

## 🌟 Key Features & Strengths

- **⚡ Dynamic WASAPI Engine (Exclusive/Shared):** A dedicated UI toggle allows you to seamlessly switch between **Exclusive Mode** (completely bypassing the Windows native mixer for microscopic 10ms output buffers and maximum real-time responsiveness) and **Shared Mode** (perfect for practicing over YouTube backing tracks or Spotify with ~15ms latency). The stream gracefully auto-restarts upon switching without breaking the UI.
  
- **📈 192kHz Implicit Oversampling:** Internal DSP engine tested to run natively at `192,000Hz`. Operating at this extreme frequency not only offers crystalline precision but naturally prevents catastrophic digital aliasing ("fizz") when generating extreme high-gain distortion.
  
- **🔥 Asymmetric Tube Simulation:** The distortion (Drive) stage does not rely on artificial symmetric clipping. A **Parametric DC Tube Bias offset** is applied within the `Math.Tanh` transfer function. The resulting asymmetrical wave deformation generates abundant and highly musical *even-order harmonics*, perfectly emulating the color and warmth of true thermionic vacuum tubes (like an overdriven 12AX7).
  
- **🔊 Multi-Stage Cabinet Simulator:** Instead of a basic low-pass filter, the Cab Sim algorithm mimics the acoustic mass and air movement of a large 4x12 Studio Cabinet using cascaded `BiQuadFilter` instances:
  - An **80Hz Butterworth High-Pass** entirely rolls off muddy sub-frequencies, freeing up headroom and tightening the low-end.
  - A **4.5kHz Double Low-Pass cascade** (Linkwitz-Riley 24dB/oct style) surgically slices off harsh digital high-end fizz, perfectly modeling the upper resonance of thick wood and speaker cones (e.g., Celestion V30).
    
- **🎛️ Comprehensive DSP FX Chain (With Dynamic Tweaker Rack):**
  - **Soft-Knee Noise Gate** (Exponential dial curve slicing out perfectly at `-80dB`).
  - **Dynamic VCA Compressor** with Envelope Tracking (Adjustable `Ratio` and `Threshold`).
  - **LFO Modulated Chorus** (Adjustable `Rate` and `Depth`).
  - **Dark Analog Tape Delay** (Adjustable `Time`, `Feedback`, and `Mix`).
  - **4-Tap Prime Number Diffusion Reverb** (Adjustable `Decay` and `Mix`).
  - **LFO Tremolo** (Adjustable `Rate` and `Depth`).
    
- **💾 Thread-Safe WAV Recording:** A dedicated button allows for surgical high-resolution recording (wet loop) by asynchronously dumping massive buffer *chunks* in the background. It utilizes strict `SyncLock` synchronization to guarantee thread-safe stability without audio dropouts or memory leaks.
  
- **🎨 Fluid UI:** Integrated fast-presets, a responsive floating-point hardware-style VUMeter, and smooth borderless dragging governed directly by Windows Native calls (WM_NCLBUTTONDOWN).

## 🚀 Quick Start / Usage
1. Select the hardware input receiving your guitar from the dropdown menu.
2. Click **ON** (Boots with a `Clean` tone preset).
3. Feel free to tweak the Gain, EQ, or play with the built-in Presets (`Crunch`, `Metal`) in real-time!

## 🛠️ Built With
- **Visual Studio / VB.NET**
- **NAudio API** (`WasapiOut`, `WaveInEvent`, DSP `BiQuadFilter`, `BufferedWaveProvider`)
- **Win32 API Integrations** (User32.dll) for borderless window dragging events.

## 📝 License
MIT License - Open Source Educational/Musical Project. Feel free to use these DSP functions as a foundation to build more complex VSTs or standalone pedalboards.
