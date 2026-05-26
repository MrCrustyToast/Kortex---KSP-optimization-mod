# Kortex - Performance Suite (v1.2.0)

Kortex is an aggressive runtime performance optimization suite targeting the core bottlenecks of Kerbal Space Program 1 (Unity 2019.4). Instead of altering game physics or degrading visuals permanently, Kortex dynamically optimizes CPU overhead, GPU rendering loops, memory allocations, and disk I/O bottlenecks to maximize FPS and deliver a stutter-free experience.

---

## Key Features & Modules

### CoreKinetics (System & Disk)
* **Async Autosave:** Replaces KSP's native, game-freezing autosave mechanism with a secure background threading architecture. Your flight simulation remains perfectly fluid while your progress is saved on another CPU core. Includes rotating multi-backup protection.

### PerformanceFlux (CPU & Memory)
* **GC Evader (Memory Pooling):** Replaces repetitive and heavy frame-by-frame heap memory allocations (such as Vessel.GetActiveParts) with a static, thread-safe memory pooling system. This starves Unity's Garbage Collector, eliminating the cyclical micro-stutters every few seconds.
* **UI Throttle:** Heavy UI elements are capped to refresh at 10 Hz instead of every single frame. This frees massive CPU headroom, especially on high-part-count vessels, without sacrificing readability.
* **Adaptive Frame Stabilizer:** Dynamically monitors engine load and smooths out severe physics lag. By temporarily micro-adjusting the physics timestep, it prevents structural stutters during extreme situations.
* **Map Mode Portrait Culling:** Completely halts the 3D rendering and animation loops of Kerbal portraits when you are looking at the Map View, avoiding wasted CPU cycles.
* **Log Anti-Spam:** Intercepts, filters, and mutes infinite engine warning loops in the KSP log file. This drastically reduces intense disk write operations (I/O overhead) that cause random stuttering.

### PerformanceFlux (GPU & Rendering)
* **Cargo & Fairing Mesh Culling:** Intelligently hides the 3D meshes and geometry of all parts hidden inside closed cargo bays and fairings. This reduces GPU draw calls and VRAM pressure significantly until the payload is deployed.
* **Shadow Cascade Governor:** Restructures KSP's outdated shadow rendering pipeline per scene. Forces StableFit shadow projections to fully eliminate shadow flickering while restoring substantial FPS on the launchpad and near planetary surfaces.

---

## Installation

### Via CKAN (Recommended)
1. Open the CKAN client.
2. Search for "Kortex - Performance Suite".
3. Check the box, click Apply Changes, and let CKAN manage your dependencies automatically.

### Manual Installation
1. Download the latest Kortex_vX.X.X.zip from the GitHub Releases page or SpaceDock.
2. Extract the archive.
3. Drag and drop the Kortex folder inside your game's GameData/ directory.
4. **Dependency:** Make sure you have Harmony2 installed.

> **Note:** Do NOT manually install 0Harmony.dll inside the Kortex folder to avoid version conflicts. Let the global Harmony installation handle it.

---

## Configuration

Kortex is designed to be fully plug-and-play with sensible defaults. However, you can toggle every single optimization module individually based on your hardware needs.

* Open KSP and go to Settings.
* Navigate to Difficulty Options and select the Kortex section.
* Use the checkboxes and sliders to customize your performance footprint.

---

## License

This project is licensed under the MIT License - see the LICENSE file for details.
Feel free to fork, contribute, or reuse modules for your own performance improvements!
