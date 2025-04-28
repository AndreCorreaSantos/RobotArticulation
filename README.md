---
# Inverse Kinematics on GPU

This project demonstrates how compute shaders can be used to accelerate inverse kinematics (IK) by sampling the configuration space of a two-jointed articulated arm entirely on the GPU. By computing the error between possible joint configurations and a target end-effector position, we identify optimal solutions in real time.

🎥 **Watch the demo video here**:\
[![Watch the video](https://img.youtube.com/vi/OH7EDbhSaNU/0.jpg)](https://youtu.be/OH7EDbhSaNU)


## 💡 Overview

- The **colored plane** behind the robotic arm visualizes the configuration space (C-space).

  - **Blue areas** indicate configurations with minimal error—i.e., joint angles that closely match the desired end-effector (EOF) position.
  - **Red areas** show high-error regions—joint angles that poorly match the target.

- The system finds the lowest-error configuration in the C-space using a GPU-based parallel search and updates the arm's pose accordingly.

## ⚠️ Current Limitations

- The arm may **jitter or oscillate** between multiple valid configurations when they exist. This occurs due to the lack of a selection heuristic or smoothing mechanism.
- Future improvements could include:
  - Adding **heuristics** to favor consistent solution selection.
  - Implementing **inertia** or motion smoothing to reduce jitter in ambiguous zones.

---
