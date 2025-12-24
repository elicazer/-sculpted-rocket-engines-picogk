# Getting started with PicoGK

PicoGK ("peacock") is a compact and robust geometry kernel for Computational Engineering.

You can find general information on [PicoGK.org](https://picogk.org) and the the [PicoGK repository on GitHub](https://leap71.com/PicoGK).

This repository contains example code, which showcases various aspects of PicoGK.

You can download this repository's source code to get an instant PicoGK-ready environment to play around with.

For more information, see the [PicoGK documentation on PicoGK.org](https://picogk.org/doc/)

# Running PicoGK

Download this example repository, open in VisualStudio Code, and run the code `Program.cs`.

The examples are organized into subfolders, according to the their category.

## Added engines and exports

- `FunctionalRocketEngine`  
  - Smooth de Laval interior with variable-radius throat and helical regenerative channels, injector face, ribs, and bolt-on flange.  
  - STL exports: `stl_exports/FunctionalRocketEngine.stl.gz` (full) and `stl_exports/FunctionalRocketEngine_CrossSection.stl`.  
  - Print tip: Z-up (nozzle down) to keep channels self-supporting; let slicer add external supports only.
- `SculptedRocketEngine`  
  - Leap71-inspired exterior: twisted throat fins, flowing surface ribs, sculpted grooves, and enlarged mounting boss; same smooth internal nozzle.  
  - STL exports: `stl_exports/SculptedRocketEngine.stl.gz` (full) and `stl_exports/SculptedRocketEngine_CrossSection.stl`.  
  - Print tip: Z-up; the sculpted features are fused to the shell for single-piece printing.
- `FluidManifold`  
  - Branching additive-friendly manifold with tapered radii and cross-section output for flow visualization.  
  - Run to generate fresh STL (not stored in `stl_exports/` by default).

STL exports are checked in under `stl_exports/` for quick 3D printing. To regenerate, set the desired task in `Program.cs` and run `dotnet run` (voxel size currently 0.4 mm).***

### Demo video
- Functional engine print clip: https://youtube.com/shorts/4WdTu9MskqU?si=fdZ33Wvz4VQH_3zT
