# VR Vision Therapy and Eye Tracking Application

A 10-level Virtual Reality (VR) Vision Therapy application designed to diagnose, train, and rehabilitate patients with various oculomotor and binocular vision dysfunctions. Built with Unity 3D, this project utilizes immersive VR technologies to replace traditional analog vision therapy tools with gamified, highly measurable, and controllable digital environments.

## 🚀 Why VR for Vision Therapy?
Vision therapy is physical therapy for the brain and eyes. Traditional methods (like Brock strings or reading charts) often suffer from low patient compliance due to repetition. Virtual Reality revolutionizes this by allowing clinical practitioners to:
- **Control the visual field:** Eliminate background distractions.
- **Isolate eyes (Dichoptic Presentation):** Independently control what each eye sees, crucial for amblyopia (lazy eye).
- **Log high-precision metrics:** Track millimeter-perfect and millisecond-accurate analytics.
- **Gamify the experience:** Drastically boost patient compliance, especially in children.

## 🎮 The 10 Therapeutic Levels
The application features 10 distinct "levels", each isolating and training specific visual skills:

1. **Basic Smooth Pursuit**: Trains users to keep their gaze locked on a moving target (improves reading & hand-eye coordination).
2. **Advanced Pursuit & Tracing**: Involves less predictable trajectories and interactions (useful for post-concussion rehab).
3. **Predictive Tracking (Pendulum Motion)**: Uses perfectly simulated pendulum physics to train predictive saccades and vergence.
4. **Complex Multi-Axis Tracking**: Forces the visual system to combine smooth pursuits with depth tracking (great for sports vision).
5. **Selective Attention**: Trains visual processing and figure-ground discrimination by introducing distractor targets.
6. **Saccadic Eye Movements**: Targets rapidly jump across the screen to train ballistic eye movements and reduce saccadic latency.
7. **Dynamic Peripheral Awareness**: Expands the functional visual field by adding peripheral tasks while maintaining central focus.
8. **Speed of Visual Processing**: Uses flash (tachistoscope) effects to improve visual memory and processing speed.
9. **Color & Contrast Differentiation**: Trains color discrimination and is used heavily in dichoptic therapy.
10. **Depth Perception and Stereopsis**: Tests and trains true 3D binocular vision, essential for treating strabismus.

## 🛠️ Technical Implementation
- **Game Engine**: Built in [Unity 3D](https://unity.com/).
- **Programming Language**: C#.
- **VR Frameworks**: Hardware-agnostic camera implementations compatible with SteamVR and Oculus headsets.
- **Rendering**: Uses World Space Canvases and highly optimized emissive materials to ensure visibility and "pop" for low-contrast patients.

## ⚙️ Getting Started (For Developers)
1. Ensure you have **Unity** installed (check `ProjectSettings/ProjectVersion.txt` for the exact editor version).
2. Clone the repository.
3. Open the project folder in Unity Hub.
4. If you have a VR Headset (Oculus/Meta Quest, HTC Vive), ensure SteamVR or the Oculus Desktop App is running and your headset is connected via Link/AirLink.
5. Open the Main Menu scene (usually `Scene 0` or found in `Assets/Scenes`) and press **Play** in the editor.

## 📄 Documentation
For an exhaustive medical-technical breakdown of the therapeutic exercises and clinical rationale, please refer to the `VR_Vision_Therapy_Report.md` included in the root directory.
