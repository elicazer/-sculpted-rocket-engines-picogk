# Custom PicoGK Builds

This repo contains my custom PicoGK generative models and ready-to-print exports.

## Run
- Open `Program.cs` and pick the task to run (currently `FunctionalRocketEngine.Task`).
- Execute `dotnet run` to view and regenerate STL outputs (voxel size set to 0.4 mm).

## Engines and Manifold

- `FunctionalRocketEngine`  
  Smooth de Laval interior with helical regenerative channels, injector face, ribs, and bolt-on flange.  
  STL: `stl_exports/FunctionalRocketEngine.stl.gz` (full) and `stl_exports/FunctionalRocketEngine_CrossSection.stl`.  
  Print tip: Z-up (nozzle down); allow external supports only.

- `SculptedRocketEngine`  
  Stylized, Leap71-inspired exterior with twisted throat fins, flow ribs, grooves, and enlarged mounting boss; same smooth internal nozzle.  
  STL: `stl_exports/SculptedRocketEngine.stl.gz` (full) and `stl_exports/SculptedRocketEngine_CrossSection.stl`.  
  Print tip: Z-up; all features fused to the shell for single-piece printing.

- `FluidManifold`  
  Branching additive-friendly manifold with tapered radii and a cross-section output for flow visualization.  
  Run to generate STL (not stored in `stl_exports/` by default).

STL exports live in `stl_exports/`. To rebuild any model, set the task in `Program.cs` and run `dotnet run`.

### Demo video
- Functional engine print clip: https://youtube.com/shorts/4WdTu9MskqU?si=fdZ33Wvz4VQH_3zT
