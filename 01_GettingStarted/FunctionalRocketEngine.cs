//
// SPDX-License-Identifier: CC0-1.0
//
// Functional liquid rocket engine geometry with regenerative cooling,
// injector face, and mounting flange. Designed for smooth curvature
// and additive manufacturability in PicoGK.
//

using PicoGK;
using System;
using System.IO;
using System.Numerics;

namespace PicoGKExamples
{
    class FunctionalRocketEngine
    {
        // Core flow path parameters (all in mm)
        static float fChamberRadius = 32f;
        static float fChamberLength = 110f;
        static float fThroatRadius = 12f;
        static float fExitRadius = 52f;
        static float fConvergingLength = 40f;
        static float fDivergingLength = 150f;

        // Structure
        static float fWallThickness = 3f;
        static float fOuterSkinOffset = 2f; // cosmetic allowance outside wall

        // Cooling channels
        static int nHelicalChannels = 12;
        static float fChannelWidth = 2.2f;
        static float fChannelDepthMin = 1.6f;
        static float fChannelDepthMax = 3.2f;
        static float fChannelStartZ = 20f;
        static float fChannelEndOffset = 15f;
        static float fChannelTwists = 1.35f; // turns from start to end

        // Injector face
        static int nInjectorRings = 3;
        static int nInjectorsPerRing = 10;
        static float fInjectorHoleRadius = 1.4f;
        static float fInjectorDepth = 6f;

        // Structural ribs
        static int nRibs = 5;
        static float fRibHeight = 2.5f;
        static float fRibWidth = 6f;

        // Mounting flange
        static float fFlangeThickness = 7f;
        static float fFlangeMargin = 12f;
        static int nBoltHoles = 6;
        static float fBoltCircleRadius = 42f;
        static float fBoltHoleRadius = 2.7f;

        // Fidelity
        static int nProfileSteps = 240;

        public static void Task()
        {
            Library.oViewer().SetGroupMaterial(0, "C0C0C0", 0.35f, 0.85f);   // shell
            Library.oViewer().SetGroupMaterial(1, "FF660022", 0.05f, 0.0f);   // channels preview
            Library.oViewer().SetGroupMaterial(2, "0099FF", 0.4f, 0.2f);      // section highlight

            float fTotalLength = fChamberLength + fConvergingLength + fDivergingLength;

            Library.Log("========================================");
            Library.Log("Functional Rocket Engine");
            Library.Log($"Chamber radius: {fChamberRadius} mm, length: {fChamberLength} mm");
            Library.Log($"Throat radius: {fThroatRadius} mm, Exit radius: {fExitRadius} mm");
            Library.Log($"Cooling channels: {nHelicalChannels} helical");
            Library.Log("========================================");

            Voxels voxOuter = voxCreateOuterShell(fTotalLength);
            Voxels voxInner = voxCreateInnerFlow(fTotalLength);
            Voxels voxFlange = voxCreateFlange();
            Voxels voxChannels = voxCreateCooling(fTotalLength);
            Voxels voxInjectors = voxCreateInjectors();
            Voxels voxRibs = voxCreateRibs(fTotalLength);

            Voxels voxShell = (voxOuter + voxFlange + voxRibs) - voxInner - voxChannels - voxInjectors;

            // Cross-section near throat for inspection
            float fSectionZ = fChamberLength + fConvergingLength * 0.35f;
            Voxels voxSection = voxCreateSection(voxShell, fSectionZ, 4f, fExitRadius + 20f);

            Library.oViewer().Add(voxShell, 0);
            Library.oViewer().Add(voxChannels, 1);
            Library.oViewer().Add(voxSection, 2);

            Mesh msh = new Mesh(voxShell);
            string strOutput = Path.Combine(Library.strLogFolder, "FunctionalRocketEngine.stl");
            msh.SaveToStlFile(strOutput);

            Mesh mshSection = new Mesh(voxSection);
            string strSection = Path.Combine(Library.strLogFolder, "FunctionalRocketEngine_CrossSection.stl");
            mshSection.SaveToStlFile(strSection);

            Library.Log($"Exported: {strOutput}");
            Library.Log($"Section:  {strSection}");
            Library.Log("========================================");
        }

        static Voxels voxCreateOuterShell(float fTotalLength)
        {
            Lattice lat = new Lattice();
            float fStep = fTotalLength / nProfileSteps;

            for (int i = 0; i < nProfileSteps; i++)
            {
                float z = i * fStep;
                float r = fGetOuterRadius(z);
                Vector3 v1 = new Vector3(0, 0, z);
                Vector3 v2 = new Vector3(0, 0, z + fStep);
                lat.AddBeam(v1, v2, r, r, false);
            }

            return new Voxels(lat);
        }

        static Voxels voxCreateInnerFlow(float fTotalLength)
        {
            Lattice lat = new Lattice();
            float fStep = fTotalLength / nProfileSteps;

            for (int i = 0; i < nProfileSteps; i++)
            {
                float z = i * fStep;
                float r = fGetFlowRadius(z);
                Vector3 v1 = new Vector3(0, 0, z);
                Vector3 v2 = new Vector3(0, 0, z + fStep);
                lat.AddBeam(v1, v2, r, r, false);
            }

            return new Voxels(lat);
        }

        static Voxels voxCreateCooling(float fTotalLength)
        {
            Voxels vox = new Voxels();
            float fZStart = fChannelStartZ;
            float fZEnd = fTotalLength - fChannelEndOffset;
            int nSteps = 220;

            for (int c = 0; c < nHelicalChannels; c++)
            {
                Lattice lat = new Lattice();
                float fAngleStart = (float)(c * 2.0 * Math.PI / nHelicalChannels);

                Vector3 vPrev = vHelixPoint(fAngleStart, fZStart, fZStart, fZEnd, 0f);

                for (int i = 1; i <= nSteps; i++)
                {
                    float t = i / (float)nSteps;
                    float z = fZStart + t * (fZEnd - fZStart);
                    float angle = fAngleStart + (float)(2.0 * Math.PI * fChannelTwists * t);

                    Vector3 vCurr = vHelixPoint(angle, z, fZStart, fZEnd, t);
                    lat.AddBeam(vPrev, vCurr, fChannelWidth * 0.5f, fChannelWidth * 0.5f, false);
                    vPrev = vCurr;
                }

                vox.BoolAdd(new Voxels(lat));
            }

            return vox;
        }

        static Vector3 vHelixPoint(float angle, float z, float fZStart, float fZEnd, float t)
        {
            float rOuter = fGetOuterRadius(z);
            float fDepth = fGetChannelDepth(z);
            float r = rOuter - fDepth;
            float x = r * (float)Math.Cos(angle);
            float y = r * (float)Math.Sin(angle);
            // gentle radial breathing to avoid constant cross section
            float breathe = 0.1f * (float)Math.Sin(t * Math.PI * 2);
            return new Vector3(x * (1f + breathe), y * (1f + breathe), z);
        }

        static Voxels voxCreateInjectors()
        {
            Voxels vox = new Voxels();

            // central core
            vox.BoolAdd(Voxels.voxSphere(new Vector3(0, 0, 0), fInjectorHoleRadius));

            for (int ring = 1; ring <= nInjectorRings; ring++)
            {
                float fRingRadius = ring * (fChamberRadius - fInjectorHoleRadius * 3) / (nInjectorRings + 1);
                int nHoles = nInjectorsPerRing + ring * 2;

                for (int i = 0; i < nHoles; i++)
                {
                    float angle = (float)(i * 2.0 * Math.PI / nHoles);
                    float x = fRingRadius * (float)Math.Cos(angle);
                    float y = fRingRadius * (float)Math.Sin(angle);

                    Lattice lat = new Lattice();
                    lat.AddBeam(new Vector3(x, y, -fInjectorDepth),
                                new Vector3(x, y, 2f),
                                fInjectorHoleRadius,
                                fInjectorHoleRadius,
                                false);
                    vox.BoolAdd(new Voxels(lat));
                }
            }

            return vox;
        }

        static Voxels voxCreateRibs(float fTotalLength)
        {
            Voxels vox = new Voxels();
            float fRegion = fChamberLength + fConvergingLength * 0.7f;

            for (int i = 0; i < nRibs; i++)
            {
                float z = fChamberLength * 0.15f + i * fRegion / nRibs;
                float r = fGetOuterRadius(z) + fRibHeight * 0.35f; // tuck ribs onto shell so they touch

                Lattice lat = new Lattice();
                int nSegments = 48;
                for (int s = 0; s < nSegments; s++)
                {
                    float a0 = (float)(s * 2.0 * Math.PI / nSegments);
                    float a1 = (float)((s + 1) * 2.0 * Math.PI / nSegments);

                    Vector3 v0 = new Vector3(r * (float)Math.Cos(a0), r * (float)Math.Sin(a0), z - fRibWidth * 0.5f);
                    Vector3 v1 = new Vector3(r * (float)Math.Cos(a1), r * (float)Math.Sin(a1), z - fRibWidth * 0.5f);
                    Vector3 v2 = new Vector3(r * (float)Math.Cos(a0), r * (float)Math.Sin(a0), z + fRibWidth * 0.5f);
                    Vector3 v3 = new Vector3(r * (float)Math.Cos(a1), r * (float)Math.Sin(a1), z + fRibWidth * 0.5f);

                    float fRad = fRibHeight * 0.45f;
                    lat.AddBeam(v0, v1, fRad, fRad, false);
                    lat.AddBeam(v2, v3, fRad, fRad, false);
                    lat.AddBeam(v0, v2, fRad, fRad, false);
                }

                vox.BoolAdd(new Voxels(lat));
            }

            return vox;
        }

        static Voxels voxCreateFlange()
        {
            float fRadius = fChamberRadius + fWallThickness + fFlangeMargin;

            Lattice lat = new Lattice();
            Vector3 vBottom = new Vector3(0, 0, -fFlangeThickness);
            Vector3 vTop = new Vector3(0, 0, 0);
            lat.AddBeam(vBottom, vTop, fRadius, fRadius, false);

            Voxels vox = new Voxels(lat);

            for (int i = 0; i < nBoltHoles; i++)
            {
                float angle = (float)(i * 2.0 * Math.PI / nBoltHoles);
                float x = fBoltCircleRadius * (float)Math.Cos(angle);
                float y = fBoltCircleRadius * (float)Math.Sin(angle);

                Lattice latHole = new Lattice();
                latHole.AddBeam(new Vector3(x, y, -fFlangeThickness - 2f),
                                new Vector3(x, y, 4f),
                                fBoltHoleRadius,
                                fBoltHoleRadius,
                                false);
                vox = vox - new Voxels(latHole);
            }

            return vox;
        }

        static Voxels voxCreateSection(Voxels voxSource, float fSectionZ, float fThickness, float fHalfSpan)
        {
            Voxels voxSlab = voxCreateSlab(fSectionZ, fThickness, fHalfSpan);
            return voxSource & voxSlab;
        }

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

        static float fGetFlowRadius(float z)
        {
            if (z < fChamberLength)
            {
                return fChamberRadius;
            }
            else if (z < fChamberLength + fConvergingLength)
            {
                float t = (z - fChamberLength) / fConvergingLength;
                return fLerp(fChamberRadius, fThroatRadius, fSmoothStep(t));
            }
            else
            {
                float t = (z - fChamberLength - fConvergingLength) / fDivergingLength;
                return fLerp(fThroatRadius, fExitRadius, fSmoothStep(t));
            }
        }

        static float fGetOuterRadius(float z)
        {
            return fGetFlowRadius(z) + fWallThickness + fOuterSkinOffset;
        }

        static float fGetChannelDepth(float z)
        {
            float fThroatZ = fChamberLength + fConvergingLength;
            float fDist = Math.Abs(z - fThroatZ);
            float fFactor = (float)Math.Exp(-fDist * fDist / 1800f);
            return fLerp(fChannelDepthMin, fChannelDepthMax, fFactor);
        }

        static float fLerp(float a, float b, float t) => a + (b - a) * t;

        static float fSmoothStep(float t) => t * t * (3f - 2f * t);
    }
}
