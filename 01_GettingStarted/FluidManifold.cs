//
// SPDX-License-Identifier: CC0-1.0
//
// Computationally generated branching fluid manifold optimized for additive manufacturing
//

using PicoGK;
using System.IO;
using System.Numerics;
using System.Collections.Generic;

namespace PicoGKExamples
{
    /// <summary>
    /// Generates a smooth, branching fluid manifold with printable internal channels.
    /// Channels ascend gently to avoid trapped overhangs and include area tapering to
    /// encourage uniform flow distribution.
    /// </summary>
    class FluidManifold
    {
        // Geometry controls
        static float fWallThickness = 2.5f;      // mm wall thickness around every channel
        static float fJunctionBlend = 6f;        // mm spherical blend at merge/split nodes
        static float fBasePlateThickness = 6f;   // mm foundation for stable printing
        static float fManifoldHeight = 110f;     // mm overall height to top outlets

        // Flow radii (inner)
        static float fInletRadius = 8f;          // mm at supply port
        static float fTrunkRadius = 6f;          // mm at branch junction
        static float fBranchRadius = 5f;         // mm branch start
        static float fOutletRadius = 4f;         // mm at outlets (tapers for balanced velocity)

        // Sampling fidelity
        static int nTubeSegments = 80;           // tube segmentation for smooth curvature

        public static void Task()
        {
            Library.oViewer().SetGroupMaterial(0, "B0C4DE", 0.25f, 0.85f); // light steel shell
            Library.oViewer().SetGroupMaterial(1, "2A6F97AA", 0.05f, 0.0f); // translucent channel preview
            Library.oViewer().SetGroupMaterial(2, "FF5500", 0.5f, 0.15f);   // cross-section highlight

            Library.Log("========================================");
            Library.Log("Generating branching fluid manifold...");
            Library.Log("  - ascending channels for support-free printing");
            Library.Log("  - tapered areas for pressure uniformity");
            Library.Log("  - blended junctions, no sharp corners");

            // Define flow graph (inlet -> split into four outlets)
            Vector3 vInlet = new Vector3(0, 0, -20f);
            Vector3 vTrunkMid = new Vector3(0, 0, 35f);
            Vector3 vSplit = new Vector3(0, 0, 70f);

            float fOutletZ = fManifoldHeight;
            Vector3[] vOutlets = new Vector3[]
            {
                new Vector3(35f, 25f, fOutletZ),
                new Vector3(-35f, 25f, fOutletZ),
                new Vector3(35f, -25f, fOutletZ),
                new Vector3(-35f, -25f, fOutletZ),
            };

            List<FlowPath> lstPaths = new List<FlowPath>();
            lstPaths.Add(new FlowPath
            {
                p0 = vInlet,
                p1 = Vector3.Lerp(vInlet, vTrunkMid, 0.4f),
                p2 = Vector3.Lerp(vTrunkMid, vSplit, 0.6f),
                p3 = vSplit,
                fR0 = fInletRadius,
                fR1 = fTrunkRadius
            });

            foreach (Vector3 vOutlet in vOutlets)
            {
                Vector3 vLead = Vector3.Lerp(vSplit, vOutlet, 0.35f);
                Vector3 vMid = Vector3.Lerp(vSplit, vOutlet, 0.7f);
                lstPaths.Add(new FlowPath
                {
                    p0 = vSplit,
                    p1 = new Vector3(vLead.X * 0.6f, vLead.Y * 0.6f, vLead.Z), // gentle flare before turn
                    p2 = vMid,
                    p3 = vOutlet,
                    fR0 = fBranchRadius,
                    fR1 = fOutletRadius
                });
            }

            // Build outer shell and channels
            Voxels voxOuter = voxCreateEnvelope(lstPaths);
            Voxels voxInner = voxCreateChannels(lstPaths);
            Voxels voxBase = voxCreateBasePlate();
            Voxels voxShell = (voxOuter + voxBase) - voxInner;

            // Cross-section for visualization
            float fSectionZ = vSplit.Z + 5f;
            Voxels voxSection = voxCreateSection(voxShell, fSectionZ, 3f, 90f);

            // Send to viewer
            Library.oViewer().Add(voxShell, 0);
            Library.oViewer().Add(voxInner, 1);
            Library.oViewer().Add(voxSection, 2);

            // Export STL assets
            Mesh mshManifold = new Mesh(voxShell);
            string strManifoldPath = Path.Combine(Library.strLogFolder, "FluidManifold.stl");
            mshManifold.SaveToStlFile(strManifoldPath);

            Mesh mshSection = new Mesh(voxSection);
            string strSectionPath = Path.Combine(Library.strLogFolder, "FluidManifold_CrossSection.stl");
            mshSection.SaveToStlFile(strSectionPath);

            Library.Log($"Manifold STL saved to: {strManifoldPath}");
            Library.Log($"Cross-section STL saved to: {strSectionPath}");
            Library.Log("========================================");
        }

        /// <summary>
        /// Union of thick tubes plus blended junctions to form exterior envelope.
        /// </summary>
        static Voxels voxCreateEnvelope(List<FlowPath> lstPaths)
        {
            Voxels vox = new Voxels();
            foreach (FlowPath path in lstPaths)
            {
                vox.BoolAdd(voxBezierTube(path, fWallThickness));
            }

            // Add blend spheres at inlet and split to remove stress risers
            vox.BoolAdd(Voxels.voxSphere(lstPaths[0].p0, fInletRadius + fWallThickness + fJunctionBlend * 0.3f));
            vox.BoolAdd(Voxels.voxSphere(lstPaths[0].p3, fTrunkRadius + fWallThickness + fJunctionBlend));

            return vox;
        }

        /// <summary>
        /// Hollow channels following the flow graph.
        /// </summary>
        static Voxels voxCreateChannels(List<FlowPath> lstPaths)
        {
            Voxels vox = new Voxels();
            foreach (FlowPath path in lstPaths)
            {
                vox.BoolAdd(voxBezierTube(path, 0f));
            }
            return vox;
        }

        /// <summary>
        /// Thin slice of the solid model for visualization / inspection.
        /// </summary>
        static Voxels voxCreateSection(Voxels voxSource, float fSectionZ, float fThickness, float fHalfSpan)
        {
            Voxels voxSlice = voxCreateSlab(fSectionZ, fThickness, fHalfSpan);
            return voxSource & voxSlice;
        }

        /// <summary>
        /// Creates a printable base pad under the inlet for build stability.
        /// </summary>
        static Voxels voxCreateBasePlate()
        {
            float fPadRadius = fInletRadius + fWallThickness + 12f;

            Lattice lat = new Lattice();
            Vector3 vBottom = new Vector3(0, 0, -fBasePlateThickness);
            Vector3 vTop = new Vector3(0, 0, 0);
            lat.AddBeam(vBottom, vTop, fPadRadius, fPadRadius, false);

            // Fillet rim for peel strength
            Voxels voxFillet = Voxels.voxSphere(vTop, fPadRadius * 0.65f);
            Voxels voxPad = new Voxels(lat) + voxFillet;
            return voxPad;
        }

        /// <summary>
        /// Creates a box-like slab using a grid of overlapping beams.
        /// </summary>
        static Voxels voxCreateSlab(float fSectionZ, float fThickness, float fHalfSpan)
        {
            Lattice lat = new Lattice();
            float fRadius = fThickness * 0.5f;
            float fStep = fRadius * 1.1f;

            for (float y = -fHalfSpan; y <= fHalfSpan; y += fStep)
            {
                Vector3 v1 = new Vector3(-fHalfSpan, y, fSectionZ);
                Vector3 v2 = new Vector3(fHalfSpan, y, fSectionZ);
                lat.AddBeam(v1, v2, fRadius, fRadius, false);
            }

            return new Voxels(lat);
        }

        /// <summary>
        /// Sweeps a tube along a cubic Bezier curve with linearly varying radius.
        /// </summary>
        static Voxels voxBezierTube(FlowPath path, float fRadiusOffset)
        {
            Lattice lat = new Lattice();

            Vector3 vPrev = vEvalBezier(path, 0f);
            for (int i = 1; i <= nTubeSegments; i++)
            {
                float t = i / (float)nTubeSegments;
                Vector3 vCurr = vEvalBezier(path, t);

                float fRadius = fLerp(path.fR0, path.fR1, fSmoothStep(t)) + fRadiusOffset;
                lat.AddBeam(vPrev, vCurr, fRadius, fRadius, false);
                vPrev = vCurr;
            }

            return new Voxels(lat);
        }

        /// <summary>
        /// Evaluate cubic Bezier at t.
        /// </summary>
        static Vector3 vEvalBezier(FlowPath path, float t)
        {
            float u = 1f - t;
            float b0 = u * u * u;
            float b1 = 3f * u * u * t;
            float b2 = 3f * u * t * t;
            float b3 = t * t * t;

            return b0 * path.p0 + b1 * path.p1 + b2 * path.p2 + b3 * path.p3;
        }

        static float fLerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        static float fSmoothStep(float t)
        {
            return t * t * (3f - 2f * t);
        }

        struct FlowPath
        {
            public Vector3 p0, p1, p2, p3;
            public float fR0;
            public float fR1;
        }
    }
}
