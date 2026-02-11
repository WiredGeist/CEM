using System;

namespace MyFirstApp.Algorithms.Physics
{
    public static class FluidDynamics
    {
        // Now accepts specific Z-coordinates for the sections
        public static float fGetDeLavalRadius(float z, float zThroat, float zChamberStart, float fChamberR, float fThroatR, float fExitR)
        {
            // 1. DIVERGING NOZZLE (Bottom to Throat)
            if (z < zThroat)
            {
                // Normalize 0..Throat
                float t = z / zThroat; 
                float invT = 1.0f - t; // 1 at bottom, 0 at throat
                
                // Parabolic Bell Curve
                return fThroatR + (fExitR - fThroatR) * MathF.Pow(invT, 1.5f);
            }
            // 2. CONVERGING SECTION (Throat to Chamber)
            else if (z < zChamberStart)
            {
                // Normalize Throat..Chamber
                float t = (z - zThroat) / (zChamberStart - zThroat);
                
                // Cosine Blend
                // t=0 (Throat) -> Cos=1 -> Result 0
                // t=1 (Chamber) -> Cos=-1 -> Result 1
                float blend = (1.0f - MathF.Cos(t * MathF.PI)) * 0.5f;
                
                return fThroatR + (fChamberR - fThroatR) * blend;
            }
            // 3. COMBUSTION CHAMBER (Top)
            else
            {
                return fChamberR;
            }
        }

        // Backward compatibility wrappers (assume default ratios if old code calls this)
        public static float fGetNozzleRadiusProfile(float z, float height, float fChamberR, float fThroatR, float fExitR)
        {
            float zThroat = height * 0.3f;
            float zChamber = height * 0.65f;
            return fGetDeLavalRadius(z, zThroat, zChamber, fChamberR, fThroatR, fExitR);
        }
    }
}