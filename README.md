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

- `FunctionalRocketEngine` – printable liquid engine with helical regen channels (see `stl_exports/FunctionalRocketEngine*.stl.gz` and cross-section STL).
- `SculptedRocketEngine` – Leap71-inspired stylized engine with twisted fins and flow ribs (see `stl_exports/SculptedRocketEngine*.stl.gz` and cross-section STL).
- `FluidManifold` – branching fluid manifold demo (STL generated on run).

STL exports are checked in under `stl_exports/` for quick 3D printing. To regenerate, run `dotnet run` (uses the task set in `Program.cs`).
