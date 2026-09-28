# Pitt Bradford Panther Showcase & Customizer (WebXR)

An interactive Mixed Reality and 3D web experience created for the **University of Pittsburgh at Bradford's Alumni & Family Weekend 2026**, highlighting the campus VR Lab in Marilyn Horne Hall!

![Pitt Bradford Panther Showcase](./src/assets/ogimage.png)

## Features

- **3D Campus Panther Statue**: High-fidelity 3D photogrammetry scan of the University of Pittsburgh at Bradford Panther mascot statue.
- **Customizable Statue Finishes**:
  - **Campus Scan**: The authentic photogrammetric scan of the outdoor statue.
  - **Pitt Royal Blue**: Official UPitt Royal Blue (`#003594`) satin finish.
  - **Pitt Athletic Gold**: Official UPitt Gold (`#FFB81C`) metallic shine.
  - **Classic Cast Bronze**: Patina/bronze statue finish.
  - **White Marble**: Polished stone sculpture finish.
  - **Bradford Onyx**: Midnight dark slate stone.
- **Interactive Resizing System**:
  - Scale Presets: Desk Mini (20 cm), Tabletop (50 cm), Pedestal (1.2 m), Life-Size Statue 1:1 (2.7 m), Monumental (4.0 m).
  - Continuous slider control (0.05x to 1.5x) with fine-tuning step buttons and live metric readouts.
  - In-XR sizing panel and interactive controller scaling.
- **Mixed Reality & WebXR**:
  - Pass-through AR on supported WebXR headsets with 6DOF controller grabbing and inspection.
  - Interactive 3D OrbitControls for desktop/laptop/tablet browsers with smooth damping, zoom, and lighting.

## Running Locally

1. Install dependencies:
   ```bash
   npm install
   ```

2. Start the local HTTPS development server:
   ```bash
   npm run serve
   ```
   Open [https://localhost:8081](https://localhost:8081) in a desktop browser or a WebXR-compatible headset browser.

3. Build production bundle:
   ```bash
   npm run build
   ```

4. Run automated test suite:
   ```bash
   npm test
   ```

## Publishing to GitHub Pages

### Option A: Automatic CI/CD via GitHub Actions (Recommended)
This repository includes an automated workflow (`.github/workflows/deploy-pages.yml`).
1. In your GitHub repository, navigate to **Settings** -> **Pages**.
2. Under **Build and deployment** -> **Source**, select **GitHub Actions**.
3. Every push to `main` automatically runs tests, builds the WebXR app, and deploys to GitHub Pages:
   ```
   https://jcallinan.github.io/Alumni-Weekend-2026/
   ```

### Option B: Manual Deploy from Desktop
To build and publish directly from your desktop machine via command line:
```bash
npm run deploy
```
This builds `dist/` and pushes it directly to the `gh-pages` branch on GitHub. (If using this method, set GitHub Pages Source to **Deploy from a branch** -> `gh-pages` / `(root)`).

---

## Viewing in WebXR / AR

1. Put on a **WebXR-compatible headset** in the Marilyn Horne Hall VR Lab.
2. Open the browser on the headset.
3. Navigate to:
   ```
   https://jcallinan.github.io/Alumni-Weekend-2026/
   ```
4. Click **"Enter AR"** or **"View in Mixed Reality"**.
5. Look at the VR Lab floor or a table and tap the controller trigger to place the campus Panther statue into your room with full color passthrough.
6. Use the floating UI panel to switch materials (Campus Bronze, Pitt Gold, Royal Blue) and adjust scale from desk miniature up to life-size monument.
