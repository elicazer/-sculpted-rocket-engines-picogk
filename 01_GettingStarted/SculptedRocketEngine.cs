//
// SPDX-License-Identifier: CC0-1.0
//
// Sculpted rocket engine inspired by Leap71/Noyron aesthetics:
// - smooth de Laval interior
// - twisted external fins on the throat band
// - sculpted flow lines on chamber/nozzle
// - reinforced base boss for mounting
//

using PicoGK;
using System;
using System.IO;
using System.Numerics;

namespace PicoGKExamples
{
    class SculptedRocketEngine
    {
        // Core dimensions (mm)
        static float fChamberRadius = 34f;
        static float fChamberLength = 120f;
        static float fThroatRadius = 11f;
        static float fExitRadius = 58f;
        static float fConvergingLength = 45f;
        static float fDivergingLength = 160f;

        // Wall and skin
        static float fWallThickness = 3.2f;
        static float fOuterSkinOffset = 1.5f;

        // Twisted fins band
        static int nTwistFins = 36;
        static float fTwistFinHeight = 4.0f;
        static float fTwistFinWidth = 4.0f;
        static float fTwistBandZStart = 90f;
        static float fTwistBandZEnd = 130f;
        static float fTwistTurns = 1.25f;

        // Sculpted ribs (flow lines)
        static int nFlowRibs = 28;
        static float fFlowRibHeight = 2.0f;
        static float fFlowRibWidth = 4.0f;

        // Mounting boss
        static float fBossRadius = 30f;
        static float fBossHeight = 16f;
        static int nBossHoles = 6;
        static float fBossHoleRadius = 2.6f;
        static float fBossHoleCircle = 20f;

        // Cooling-ish grooves (visual)
        static int nGrooves = 18;
        static float fGrooveDepth = 1.4f;
        static float fGrooveWidth = 2.4f;

        // Injector face
        static int nInjectorRings = 3;
        static int nInjectorsPerRing = 12;
        static float fInjectorHoleRadius = 1.3f;
        static float fInjectorDepth = 6f;

        static int nProfileSteps = 260;

        public static void Task()
        {
            Library.oViewer().SetGroupMaterial(0, "CC8855", 0.35f, 0.65f); // coppery shell
            Library.oViewer().SetGroupMaterial(1, "5599FF44", 0.05f, 0.0f); // translucent interior preview
            Library.oViewer().SetGroupMaterial(2, "FF6600", 0.5f, 0.2f);    // section highlight

            float fTotalLength = fChamberLength + fConvergingLength + fDivergingLength;

            Library.Log("========================================");
            Library.Log("Sculpted Rocket Engine (stylized)");
            Library.Log($"Chamber R: {fChamberRadius} mm, Throat R: {fThroatRadius} mm, Exit R: {fExitRadius} mm");
            Library.Log($"Twist fins: {nTwistFins}, Flow ribs: {nFlowRibs}");
            Library.Log("========================================");

            Voxels voxOuter = voxCreateOuterShell(fTotalLength);
            Voxels voxInner = voxCreateInnerFlow(fTotalLength);
            Voxels voxBoss = voxCreateBoss();
            Voxels voxTwist = voxCreateTwistFins();
            Voxels voxFlow = voxCreateFlowRibs(fTotalLength);
            Voxels voxGrooves = voxCreateGrooves(fTotalLength);
            Voxels voxInjectors = voxCreateInjectors();

            Voxels voxShell = (voxOuter + voxBoss + voxTwist + voxFlow) - voxInner - voxGrooves - voxInjectors;

            // Cross-section for inspection at throat region
            float fSectionZ = fChamberLength + fConvergingLength * 0.5f;
            Voxels voxSection = voxCreateSection(voxShell, fSectionZ, 4f, fExitRadius + 25f);

            Library.oViewer().Add(voxShell, 0);
            Library.oViewer().Add(voxInner, 1);
            Library.oViewer().Add(voxSection, 2);

            Mesh msh = new Mesh(voxShell);
            string strOut = Path.Combine(Library.strLogFolder, "SculptedRocketEngine.stl");
            msh.SaveToStlFile(strOut);

            Mesh mshSec = new Mesh(voxSection);
            string strSec = Path.Combine(Library.strLogFolder, "SculptedRocketEngine_CrossSection.stl");
            mshSec.SaveToStlFile(strSec);

            Library.Log($"Exported: {strOut}");
            Library.Log($"Section:  {strSec}");
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

        static Voxels voxCreateBoss()
        {
            // Base boss for mounting
            Lattice lat = new Lattice();
            // Extend slightly above z=0 to guarantee fusion with shell
            Vector3 vBottom = new Vector3(0, 0, -fBossHeight);
            Vector3 vTop = new Vector3(0, 0, 6f);
            lat.AddBeam(vBottom, vTop, fBossRadius, fBossRadius, false);
            Voxels vox = new Voxels(lat);

            // Blend into shell at junction for print robustness
            vox.BoolAdd(Voxels.voxSphere(new Vector3(0, 0, 0), fBossRadius * 0.6f));

            for (int i = 0; i < nBossHoles; i++)
            {
                float a = (float)(i * 2.0 * Math.PI / nBossHoles);
                float x = fBossHoleCircle * (float)Math.Cos(a);
                float y = fBossHoleCircle * (float)Math.Sin(a);
                Lattice latHole = new Lattice();
                latHole.AddBeam(new Vector3(x, y, -fBossHeight - 3f),
                                new Vector3(x, y, 3f),
                                fBossHoleRadius,
                                fBossHoleRadius,
                                false);
                vox = vox - new Voxels(latHole);
            }
            return vox;
        }

        static Voxels voxCreateTwistFins()
        {
            Voxels vox = new Voxels();
            float fBandLen = fTwistBandZEnd - fTwistBandZStart;
            int nSteps = 120;
            for (int i = 0; i < nTwistFins; i++)
            {
                float aStart = (float)(i * 2.0 * Math.PI / nTwistFins);
                Lattice lat = new Lattice();
                Vector3 vPrev = Vector3.Zero;
                bool bInit = true;
                for (int s = 0; s <= nSteps; s++)
                {
                    float t = s / (float)nSteps;
                    float z = fTwistBandZStart + t * fBandLen;
                    float a = aStart + (float)(2.0 * Math.PI * fTwistTurns * t);
                    float r = fGetOuterRadius(z) + fTwistFinHeight * 0.55f; // ensure overlap with shell
                    float x = r * (float)Math.Cos(a);
                    float y = r * (float)Math.Sin(a);
                    Vector3 v = new Vector3(x, y, z);
                    if (!bInit)
                    {
                        lat.AddBeam(vPrev, v, fTwistFinWidth * 0.6f, fTwistFinWidth * 0.6f, false);
                    }
                    vPrev = v;
                    bInit = false;
                }
                vox.BoolAdd(new Voxels(lat));
            }
            return vox;
        }

        static Voxels voxCreateFlowRibs(float fTotalLength)
        {
            Voxels vox = new Voxels();
            int nSegments = 200;
            for (int i = 0; i < nFlowRibs; i++)
            {
                float phase = (float)(i * 2.0 * Math.PI / nFlowRibs);
                Lattice lat = new Lattice();
                Vector3 vPrev = Vector3.Zero;
                bool bInit = true;
                for (int s = 0; s <= nSegments; s++)
                {
                    float t = s / (float)nSegments;
                    float z = t * fTotalLength;
                    float baseR = fGetOuterRadius(z);
                    float ripple = 1f + 0.05f * (float)Math.Sin(t * 6f * Math.PI + phase);
                    float r = baseR + fFlowRibHeight * 0.35f + fFlowRibHeight * 0.2f * ripple; // bias inward to touch shell
                    float a = phase + t * 1.2f * (float)Math.PI;
                    float x = r * (float)Math.Cos(a);
                    float y = r * (float)Math.Sin(a);
                    Vector3 v = new Vector3(x, y, z);
                    if (!bInit)
                    {
                        float fRad = Math.Max(0.8f, fFlowRibWidth * 0.35f);
                        lat.AddBeam(vPrev, v, fRad, fRad, false);
                    }
                    vPrev = v;
                    bInit = false;
                }
                vox.BoolAdd(new Voxels(lat));
            }
            return vox;
        }

        static Voxels voxCreateGrooves(float fTotalLength)
        {
            Voxels vox = new Voxels();
            int nSteps = 200;
            for (int i = 0; i < nGrooves; i++)
            {
                float aStart = (float)(i * 2.0 * Math.PI / nGrooves);
                Lattice lat = new Lattice();
                Vector3 vPrev = Vector3.Zero;
                bool bInit = true;
                for (int s = 0; s <= nSteps; s++)
                {
                    float t = s / (float)nSteps;
                    float z = t * fTotalLength;
                    float r = fGetOuterRadius(z) - fGrooveDepth;
                    float a = aStart + t * 0.8f * (float)Math.PI;
                    float x = r * (float)Math.Cos(a);
                    float y = r * (float)Math.Sin(a);
                    Vector3 v = new Vector3(x, y, z);
                    if (!bInit)
                    {
                        lat.AddBeam(vPrev, v, fGrooveWidth * 0.5f, fGrooveWidth * 0.5f, false);
                    }
                    vPrev = v;
                    bInit = false;
                }
                vox.BoolAdd(new Voxels(lat));
            }
            return vox;
        }

        static Voxels voxCreateInjectors()
        {
            Voxels vox = new Voxels();
            vox.BoolAdd(Voxels.voxSphere(new Vector3(0, 0, 0), fInjectorHoleRadius));
            for (int ring = 1; ring <= nInjectorRings; ring++)
            {
                float fRingRadius = ring * (fChamberRadius - fInjectorHoleRadius * 3) / (nInjectorRings + 1);
                int nHoles = nInjectorsPerRing + ring * 3;
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
                return fChamberRadius;
            if (z < fChamberLength + fConvergingLength)
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

        static float fLerp(float a, float b, float t) => a + (b - a) * t;
        static float fSmoothStep(float t) => t * t * (3f - 2f * t);
    }
}
