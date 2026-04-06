# 🎸 NeurAmpli: Guitar DSP Amplifier

**NeurAmpli** is a minimal & powerful virtual guitar amplifier (Amp Simulator) written natively in VB.NET. By pushing the **NAudio** architecture to its limits, it transforms a dry input signal from your audio interface into a compact, saturated, and musically rich even-harmonic tube tone with near-zero latency.

### v2.5.3 (High-Fidelity Audio & User Presets Update)
The entire DSP suite, User Interface, and Audio Engine have been completely overhauled. 
- Introduces **3 Custom User Preset Slots (USR 1, USR 2, USR 3)** on the main panel. Right-click to save your exact knob/effect state; left-click to instantly load it.
- **True 24-bit PCM Capture**: The audio engine now fully utilizes the 24-bit headroom of modern audio interfaces, gaining 48dB of dynamic range compared to standard 16-bit capture.
- Introduces the **Dynamic FX Tweaker Rack**, a smart side-panel that reveals granular DSP controls (Chorus Rate/Depth, Delay Feedback/Mix, Comp Ratio/Threshold) when clicking an effect's name.
- Custom vector-rendered Flat/Neumorphic Glass Panels and Knobs.
- A **Safety Master Soft-Knee Limiter** cleanly preserves dynamics below 0.9 and smoothly soft-clips extreme transient peaks, preventing digital clipping without altering the clean tone.

<img width="918" height="365" alt="253" src="https://github.com/user-attachments/assets/d41367ba-0cfc-4917-b810-b778fbb5e847" />

## 🌟 Key Features & Strengths

- **⚡ Dynamic WASAPI Engine (Exclusive/Shared):** A dedicated UI toggle allows you to seamlessly switch between **Exclusive Mode** (completely bypassing the Windows native mixer for microscopic 10ms output buffers and maximum real-time responsiveness) and **Shared Mode** (perfect for practicing over YouTube backing tracks or Spotify with ~15ms latency). The stream gracefully auto-restarts upon switching without breaking the UI.
  
- **📈 192kHz Implicit Oversampling:** Internal DSP engine tested to run natively at `192,000Hz`. Operating at this extreme frequency not only offers crystalline precision but naturally prevents catastrophic digital aliasing ("fizz") when generating extreme high-gain distortion.
  
- **🔥 Asymmetric Tube Simulation with 2x Oversampling:** The distortion (Drive) stage doesn't rely on artificial symmetric clipping. A **Parametric DC Tube Bias offset** is applied within the `Math.Tanh` transfer function. It's now heavily upgraded with **internal 2x Oversampling** (anti-imaging and anti-aliasing filters) to perfectly emulate the color and warmth of true thermionic vacuum tubes without generating high-frequency digital "fizz" artifacts.
  
- **🔊 6-Stage Advanced Cabinet Simulator:** Instead of a basic low-pass filter, the Cab Sim algorithm mimics the acoustic mass and air movement of a large 4x12 Studio Cabinet using cascaded `BiQuadFilter` instances:
  - **70Hz HPF:** Removes subsonic "boom" for a tight low-end.
  - **90Hz Peaking EQ:** Simulates the resonant body of a large closed-back wood cabinet.
  - **4.2kHz Cascade LPF:** Cuts the harsh top end.
  - **3.5kHz Presence Notch:** Beautifully captures the distinct mid-scoop of guitar speaker cones.
  - **5.5kHz Rolloff:** Ensures a natural smooth fade-out matching real microphones off-axis.
    
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
