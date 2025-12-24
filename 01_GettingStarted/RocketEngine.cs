//
// SPDX-License-Identifier: CC0-1.0
//
// Parametric Liquid Rocket Engine Generator
// Features: De Laval nozzle, regenerative cooling channels, injector pattern, structural ribs
//

using PicoGK;
using System.Numerics;

namespace PicoGKExamples
{
    /// <summary>
    /// Computationally generated liquid rocket engine with parametric design
    /// </summary>
    class RocketEngine
    {
        // ===== ENGINE PARAMETERS =====
        
        // Chamber parameters
        static float fChamberRadius = 30f;           // mm
        static float fChamberLength = 100f;          // mm
        
        // Nozzle parameters
        static float fThroatRadius = 15f;            // mm - critical dimension
        static float fExitRadius = 45f;              // mm
        static float fConvergingLength = 40f;        // mm
        static float fDivergingLength = 120f;        // mm
        
        // Wall thickness
        static float fWallThickness = 3f;            // mm - uniform structural wall
        
        // Cooling channels
        static int nCoolingChannels = 24;            // number of channels around circumference
        static float fChannelWidth = 2f;             // mm
        static float fChannelDepthMin = 1.5f;        // mm - at chamber/exit
        static float fChannelDepthMax = 3f;          // mm - at throat (highest heat flux)
        static float fChannelStartZ = 20f;           // mm - start channels after injector
        
        // Injector pattern
        static int nInjectorRings = 4;               // concentric rings of injector holes
        static int nInjectorsPerRing = 8;            // holes per ring
        static float fInjectorHoleRadius = 1.5f;     // mm
        static float fInjectorDepth = 5f;            // mm
        
        // Structural ribs
        static int nStructuralRibs = 6;              // external reinforcement rings
        static float fRibHeight = 4f;                // mm
        static float fRibWidth = 6f;                 // mm

        public static void Task()
        {
            Library.oViewer().SetGroupMaterial(0, "CCCCCC", 0.3f, 0.9f);  // Metallic gray
            Library.oViewer().SetGroupMaterial(1, "FF6600", 0.5f, 0.2f);  // Orange (for visualization)
            
            Library.Log("========================================");
            Library.Log("Generating Parametric Rocket Engine");
            Library.Log("========================================");
            Library.Log($"Throat Radius: {fThroatRadius} mm");
            Library.Log($"Chamber Radius: {fChamberRadius} mm");
            Library.Log($"Wall Thickness: {fWallThickness} mm");
            Library.Log($"Cooling Channels: {nCoolingChannels}");
            Library.Log("");

            // Create the complete engine geometry
            Voxels voxEngine = voxCreateEngine();
            
            // Add to viewer
            Library.oViewer().Add(voxEngine);
            
            // Export as STL
            Library.Log("Converting to mesh and exporting...");
            Mesh mshEngine = new Mesh(voxEngine);
            string strOutputPath = Path.Combine(Library.strLogFolder, "RocketEngine.stl");
            mshEngine.SaveToStlFile(strOutputPath);
            
            Library.Log($"Engine exported to: {strOutputPath}");
            Library.Log("========================================");
        }

        /// <summary>
        /// Create the complete rocket engine assembly
        /// </summary>
        static Voxels voxCreateEngine()
        {
            // Calculate total length
            float fTotalLength = fChamberLength + fConvergingLength + fDivergingLength;
            
            // 1. Create outer shell (solid body)
            Library.Log("Creating outer shell...");
            Voxels voxOuter = voxCreateOuterShell(fTotalLength);
            
            // 2. Create inner flow path
            Library.Log("Creating internal flow path...");
            Voxels voxInner = voxCreateInnerFlowPath(fTotalLength);
            
            // 3. Subtract inner from outer to create hollow structure
            Voxels voxShell = voxOuter - voxInner;
            
            // 4. Add regenerative cooling channels
            Library.Log($"Adding {nCoolingChannels} regenerative cooling channels...");
            Voxels voxChannels = voxCreateCoolingChannels(fTotalLength);
            voxShell = voxShell - voxChannels;
            
            // 5. Add injector face pattern
            Library.Log("Adding injector face pattern...");
            Voxels voxInjector = voxCreateInjectorPattern();
            voxShell = voxShell - voxInjector;
            
            // 6. Add structural ribs
            Library.Log("Adding structural reinforcement ribs...");
            Voxels voxRibs = voxCreateStructuralRibs(fTotalLength);
            voxShell = voxShell + voxRibs;
            
            return voxShell;
        }

        /// <summary>
        /// Create outer shell with smooth contours
        /// </summary>
        static Voxels voxCreateOuterShell(float fTotalLength)
        {
            Lattice lat = new Lattice();
            
            int nSteps = 200;  // Resolution for smooth curves
            float fStep = fTotalLength / nSteps;
            
            for (int i = 0; i < nSteps; i++)
            {
                float z = i * fStep;
                float r = fGetOuterRadius(z) + fWallThickness;
                
                Vector3 v1 = new Vector3(0, 0, z);
                Vector3 v2 = new Vector3(0, 0, z + fStep);
                
                lat.AddBeam(v1, v2, r, r, false);
            }
            
            return new Voxels(lat);
        }

        /// <summary>
        /// Create internal flow path (De Laval nozzle profile)
        /// </summary>
        static Voxels voxCreateInnerFlowPath(float fTotalLength)
        {
            Lattice lat = new Lattice();
            
            int nSteps = 200;
            float fStep = fTotalLength / nSteps;
            
            for (int i = 0; i < nSteps; i++)
            {
                float z = i * fStep;
                float r = fGetInnerRadius(z);
                
                Vector3 v1 = new Vector3(0, 0, z);
                Vector3 v2 = new Vector3(0, 0, z + fStep);
                
                lat.AddBeam(v1, v2, r, r, false);
            }
            
            return new Voxels(lat);
        }

        /// <summary>
        /// Get outer radius at axial position z
        /// </summary>
        static float fGetOuterRadius(float z)
        {
            if (z < fChamberLength)
            {
                // Cylindrical chamber
                return fChamberRadius;
            }
            else if (z < fChamberLength + fConvergingLength)
            {
                // Converging section - smooth transition
                float t = (z - fChamberLength) / fConvergingLength;
                t = fSmoothStep(t);  // Smooth interpolation
                return fLerp(fChamberRadius, fThroatRadius, t);
            }
            else
            {
                // Diverging section - smooth transition
                float t = (z - fChamberLength - fConvergingLength) / fDivergingLength;
                t = fSmoothStep(t);
                return fLerp(fThroatRadius, fExitRadius, t);
            }
        }

        /// <summary>
        /// Get inner radius at axial position z (flow path)
        /// </summary>
        static float fGetInnerRadius(float z)
        {
            return fGetOuterRadius(z) - fWallThickness;
        }

        /// <summary>
        /// Create regenerative cooling channels with variable depth
        /// </summary>
        static Voxels voxCreateCoolingChannels(float fTotalLength)
        {
            Voxels voxChannels = new Voxels();
            
            float fEndZ = fTotalLength - 10f;  // Stop before exit
            
            // Create channels around circumference
            for (int i = 0; i < nCoolingChannels; i++)
            {
                float fAngle = (float)(i * 2.0 * Math.PI / nCoolingChannels);
                
                Lattice latChannel = new Lattice();
                
                int nSteps = 150;
                for (int j = 0; j < nSteps; j++)
                {
                    float z = fChannelStartZ + j * (fEndZ - fChannelStartZ) / nSteps;
                    float zNext = fChannelStartZ + (j + 1) * (fEndZ - fChannelStartZ) / nSteps;
                    
                    // Get radius at this position
                    float r = fGetOuterRadius(z);
                    
                    // Variable channel depth based on position (deeper at throat)
                    float fDepth = fGetChannelDepth(z);
                    float fChannelRadius = r - fDepth;
                    
                    // Position on circumference
                    float x = fChannelRadius * (float)Math.Cos(fAngle);
                    float y = fChannelRadius * (float)Math.Sin(fAngle);
                    
                    float rNext = fGetOuterRadius(zNext);
                    float fDepthNext = fGetChannelDepth(zNext);
                    float fChannelRadiusNext = rNext - fDepthNext;
                    float xNext = fChannelRadiusNext * (float)Math.Cos(fAngle);
                    float yNext = fChannelRadiusNext * (float)Math.Sin(fAngle);
                    
                    Vector3 v1 = new Vector3(x, y, z);
                    Vector3 v2 = new Vector3(xNext, yNext, zNext);
                    
                    latChannel.AddBeam(v1, v2, fChannelWidth / 2, fChannelWidth / 2, false);
                }
                
                voxChannels.BoolAdd(new Voxels(latChannel));
            }
            
            return voxChannels;
        }

        /// <summary>
        /// Calculate channel depth (deepest at throat where heat flux is highest)
        /// </summary>
        static float fGetChannelDepth(float z)
        {
            float fThroatZ = fChamberLength + fConvergingLength;
            float fDistanceFromThroat = Math.Abs(z - fThroatZ);
            
            // Gaussian-like distribution centered at throat
            float fFactor = (float)Math.Exp(-fDistanceFromThroat * fDistanceFromThroat / 2000f);
            
            return fLerp(fChannelDepthMin, fChannelDepthMax, fFactor);
        }

        /// <summary>
        /// Create injector face pattern
        /// </summary>
        static Voxels voxCreateInjectorPattern()
        {
            Voxels voxPattern = new Voxels();
            
            // Central injector
            Voxels voxCenter = Voxels.voxSphere(new Vector3(0, 0, 0), fInjectorHoleRadius);
            voxPattern.BoolAdd(voxCenter);
            
            // Concentric rings of injectors
            for (int ring = 1; ring <= nInjectorRings; ring++)
            {
                float fRingRadius = ring * (fChamberRadius - fInjectorHoleRadius * 3) / (nInjectorRings + 1);
                int nHoles = nInjectorsPerRing * ring;  // More holes in outer rings
                
                for (int i = 0; i < nHoles; i++)
                {
                    float fAngle = (float)(i * 2.0 * Math.PI / nHoles);
                    float x = fRingRadius * (float)Math.Cos(fAngle);
                    float y = fRingRadius * (float)Math.Sin(fAngle);
                    
                    // Create cylindrical hole extending into injector face
                    Lattice lat = new Lattice();
                    lat.AddBeam(new Vector3(x, y, -fInjectorDepth),
                               new Vector3(x, y, 1f),
                               fInjectorHoleRadius,
                               fInjectorHoleRadius,
                               false);
                    
                    voxPattern.BoolAdd(new Voxels(lat));
                }
            }
            
            return voxPattern;
        }

        /// <summary>
        /// Create structural reinforcement ribs
        /// </summary>
        static Voxels voxCreateStructuralRibs(float fTotalLength)
        {
            Voxels voxRibs = new Voxels();
            
            // Distribute ribs along chamber and converging section
            float fRibRegion = fChamberLength + fConvergingLength * 0.5f;
            
            for (int i = 0; i < nStructuralRibs; i++)
            {
                float z = fChamberLength * 0.2f + i * fRibRegion / nStructuralRibs;
                float r = fGetOuterRadius(z) + fWallThickness;
                
                // Create ring
                Lattice latRib = new Lattice();
                
                int nSegments = 36;
                for (int j = 0; j < nSegments; j++)
                {
                    float fAngle1 = (float)(j * 2.0 * Math.PI / nSegments);
                    float fAngle2 = (float)((j + 1) * 2.0 * Math.PI / nSegments);
                    
                    float x1 = (r + fRibHeight) * (float)Math.Cos(fAngle1);
                    float y1 = (r + fRibHeight) * (float)Math.Sin(fAngle1);
                    float x2 = (r + fRibHeight) * (float)Math.Cos(fAngle2);
                    float y2 = (r + fRibHeight) * (float)Math.Sin(fAngle2);
                    
                    Vector3 v1 = new Vector3(x1, y1, z - fRibWidth / 2);
                    Vector3 v2 = new Vector3(x2, y2, z - fRibWidth / 2);
                    Vector3 v3 = new Vector3(x1, y1, z + fRibWidth / 2);
                    Vector3 v4 = new Vector3(x2, y2, z + fRibWidth / 2);
                    
                    latRib.AddBeam(v1, v2, fRibHeight / 2, fRibHeight / 2, false);
                    latRib.AddBeam(v3, v4, fRibHeight / 2, fRibHeight / 2, false);
                    latRib.AddBeam(v1, v3, fRibHeight / 2, fRibHeight / 2, false);
                }
                
                voxRibs.BoolAdd(new Voxels(latRib));
            }
            
            return voxRibs;
        }

        // ===== UTILITY FUNCTIONS =====
        
        /// <summary>
        /// Linear interpolation
        /// </summary>
        static float fLerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        /// <summary>
        /// Smooth step interpolation (S-curve) for continuous curvature
        /// </summary>
        static float fSmoothStep(float t)
        {
            return t * t * (3f - 2f * t);
        }
    }
}
