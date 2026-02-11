# Computational Design: Algorithmic Plane Prototype

![Plane](CEM/Public/plane.png)

This repository contains the experimental C# code used to generate the algorithmic aircraft geometry featured in my YouTube video. This project is shared as a technical reference for those interested in computational engineering and voxel-based design.

## 📺 Project Context
This code is a demonstration of moving away from manual CAD toward algorithmic geometry generation.
* **Watch the Video:** [Link to your YouTube Video]

## 🛠 Required Frameworks & Dependencies
This project is built on the open-source stack developed by **Leap71**. To run or understand this code, you should familiarize yourself with:

*   **PicoGK:** The compact Voxel-based Geometry Kernel. [GitHub Repo](https://github.com/leap71/PicoGK)
*   **LEAP71_ShapeKernel:** A high-level library for generating complex engineering shapes. [GitHub Repo](https://github.com/leap71/LEAP71_ShapeKernel)
*   **Leap71 Documentation:** More information on the paradigm of Computational Engineering can be found at [leap71.com/picogk](https://leap71.com/picogk/).

## 🚀 Getting Started

1.  **Clone this repository.**
2.  **Ensure you have the .NET 9.0 SDK installed.**
3.  **Dependency Setup:** This project references the PicoGK NuGet package and includes specific implementations from the LEAP71 ShapeKernel. Ensure you have access to the Leap71 repositories listed above for full context.
4.  **Run the application:**
    ```bash
    dotnet run
    ```

## ⚙️ Technical Notes

### Voxel Resolution
Geometry detail is managed via `AppConfig.VoxelSize`. 
*   **Lower values** increase resolution and detail but require more system memory (RAM) and processing power.
*   **Higher values** allow for faster generation and lower resource usage during testing.

### Automated Log Management
To prevent "File in Use" errors during frequent restarts, the application creates a unique timestamped folder for each session:
```csharp
string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
string strLogFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log", timestamp);
```
PicoGK requires a log directory to initialize. These folders are stored locally in the build directory and can be cleared manually to save disk space.

### Namespace Management
The `.csproj` file is configured to remove default `System.Drawing` and `System.Windows.Forms` global usings. This is necessary to avoid naming conflicts with the PicoGK geometry kernel while still allowing for a Windows-based UI.

## 📂 Repository Structure
*   **Algorithms/**: C# logic for generating the aerodynamic shell and internal structural lattices.
*   **ShapeKernel/**: My implementation and integration of the LEAP71 ShapeKernel logic.
*   **UI/**: The interface used to interact with the generation parameters.
*   **Products/**: Logic defining the final assembly of the aircraft components.

## ⚠️ Disclaimer
This is an experimental prototype. It is provided "as-is" for educational and research purposes. It is not intended for production engineering without further validation and testing.
