# HandovAR: A Framework of AI-facilitated Handover System via Augmented Reality for ICU Nurses

https://github.com/user-attachments/assets/2ce956c4-d481-4320-8882-1ae56348dfc6

## Overview
HandovAR is a design framework proposed to assist ICU nurses during shift handovers by reducing cognitive load and improving information retention.

This repository contains a VR prototype designed to simulate the proposed AR experience. By immersing users in a virtual ICU, we evaluate spatial layouts, data visualisation, and collaborative workflows before deploying to physical AR hardware.

## Research Goals
This prototype was developed to explore three key concepts from the HandovAR proposal:
1.  **In-Situ Data Visualisation:** Displaying patient vitals and anatomical models directly at the bedside to reduce fragmentation.
2.  **ISBAR Structure:** enforcing the *Identify, Situation, Background, Assessment, Recommendation* framework via visual cues.
3.  **Collaborative Handover:** Using multiplayer networking to allow two nurses (incoming and outgoing) to stand in the same virtual space and review patient data together.

## Tech Stack
* **Engine:** Unity 6000.3.6f1
* **Platform:** Meta Quest 3 (OpenXR)
* **Networking:** Unity Netcode for GameObjects (NGO)
* **Interaction:** XR Interaction Toolkit (XRIT)

## Setup & Installation
1.  **Clone the Repository:**
    ```bash
    git clone https://github.com/snicklepickles/HandovAR.git
    ```
2.  **Open in Unity:**
    * Add the project folder to Unity Hub.
    * *Note: First launch may take time to import assets.*
3.  **Build/Run:**
    * Open scene: `Assets/Scenes/SampleScene`
    * Build targeting Android (Quest 3).
