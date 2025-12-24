# Fluid Manifold Test & Evaluation Plan

## Goals
- Compare the branching manifold against a straight-channel baseline for flow uniformity and pressure efficiency.
- Validate that internal channels remain printable without soluble supports.

## Hardware & Setup
- Print both parts in the same orientation (Z-up, inlet downward) using the same filament and nozzle.
- Suggested slice defaults: 0.2 mm layer height, 0.4 mm nozzle, 3 perimeters, 20–30% gyroid infill, bridges disabled (channels rely on ascending geometry instead).
- Manifold STL: `FluidManifold.stl` (exported to `Log` folder by the PicoGK run).
- Baseline: generate a matching straight channel (same inlet/outlet count and overall height) for comparison; keep wall thickness equal.

## Experiments
1) **Flow visualization with dyed water**
   - Use a low-pressure pump (gravity feed or small peristaltic) to push dyed water from the inlet.
   - Record a top view of all four outlets; look for first-arrival time and steadystate uniformity.
   - Repeat with flow restrictors swapped between outlets to test robustness.
2) **Pressure drop measurement**
   - Place a simple manometer or low-range pressure sensor at the inlet.
   - Run at two flow rates (low and moderate) and record inlet pressure for both the branching manifold and the straight-channel baseline.
3) **Printability/manufacturability**
   - Inspect internal surfaces with borescope or sectioning of a spare print.
   - Log whether any manual support removal was required; record success/failure rate across at least three prints.

## Data to capture
- Start/steady-state frames from dyed water test (timestamps per outlet).
- Inlet pressure vs. flow rate for both geometries.
- Print notes: slicer warnings, stringing inside channels, any clogging or delamination.

## Success criteria
- Outlet arrival times within ±10% of each other (qualitative uniformity).
- Lower or equal inlet pressure at matched flow compared to the straight-channel baseline.
- Zero soluble supports used; prints succeed in ≥2/3 attempts without post-processing the channels.
