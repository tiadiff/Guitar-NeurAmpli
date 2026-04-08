# Contributing to NeurAmpli (GuitarAmp)

First off, thanks for taking the time to contribute!

The following is a set of guidelines for contributing to NeurAmpli. These are mostly guidelines, not rules. Use your best judgment, and feel free to propose changes to this document in a pull request.

## Development Setup

### Opening the project in Visual Studio
- Download and install [Visual Studio Community 2022](https://visualstudio.microsoft.com/vs/community/) (or higher) with the **.NET desktop development** workload.
- Clone the repository: `git clone https://github.com/tiadiff/Guitar-NeurAmpli.git`
- Open the `.sln` file (or the `.vbproj` file) directly inside Visual Studio.

### Dependencies
This project heavily relies on the **NAudio** audio library for managing WASAPI audio streams and applying the DSP filters.
- **NAudio**: Usually automatically restored via NuGet upon building the solution. If not, right-click on the Solution -> `Manage NuGet Packages`, search for `NAudio` and install/restore it.
- No other external compiled DLLs are required. Everything is natively managed!

## Architecture & DSP Logic

To help you get up to speed quickly, it's important to know where the core signal processing takes place.

- **`GuitarAmp/GuitarAmpEffect.vb`**: 🧠 **The Core DSP Engine**. This class implements the `ISampleProvider` interface from NAudio. This is where the magic happens:
  - The `Read` method loops through the audio stream samples in real-time.
  - The effects chain is applied here: **Noise Gate -> Distortion (Asymmetric Tube Bias & Oversampling) -> Cabinet Simulator (BiQuad Filters) -> Chorus (Cubic Hermite Mod) -> Delay -> Reverb -> Tremolo -> Compressor**.
- **`GuitarAmp/Form1.vb`**: Handles UI interactions, Windows API imports (for borderless window drag), saving/loading presets, thread-safe asynchronous WAV recording, and NAudio lifecycle (Exclusive/Shared WasapiOut stream initialization).
- **`GuitarAmp/ModernControls.vb`**: Contains the custom-drawn GDI+ UI elements (knobs, toggles, level meters).

## Submitting Pull Requests
- Keep your commits atomic and well-documented.
- If you're adding a new DSP effect, make sure to add it into the `GuitarAmpEffect.vb` class and keep it strictly rate-independent so it works perfectly across 44.1kHz up to 192kHz streams.

Thank you for contributing!
