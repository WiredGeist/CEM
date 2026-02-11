using System;
using Leap71.ShapeKernel; 

// *** FIX: Namespace vereinheitlicht ***
namespace MyFirstApp.Algorithms.Physics 
{
    public static class Thermal
    {
        // --- ROCKET ENGINE LOGIC ---
        public static float CalculateMaxWallThickness(float fHeatFlux, float fConductivity, float fMaxTempC, float fCoolantTempC)
        {
            float fDeltaT = fMaxTempC - fCoolantTempC;
            float fThicknessMeters = (fConductivity * fDeltaT) / fHeatFlux;
            return Math.Clamp(fThicknessMeters * 1000.0f, 0.5f, 20.0f);
        }

        public static float CalculateLatticeDensity(float fCurrentTemp, float fMaxTemp)
        {
            float fRatio = fCurrentTemp / fMaxTemp;
            return Uf.fTransSmooth(0.2f, 0.8f, fRatio, 0.5f, 0.2f);
        }

        // --- COFFEE CUP LOGIC ---
        public static float CalculateInsulationThickness(float innerTempC, float targetOuterTempC)
        {
            float deltaT = innerTempC - targetOuterTempC;
            deltaT = Math.Abs(deltaT);
            float requiredThickness = deltaT / 4.0f;
            return Math.Max(requiredThickness, 2.0f);
        }

        // --- COOLING FIN LOGIC ---
        public static float fCalculateFinHeight(float fHeatFlux, float fTargetTemp)
        {
            float fBaselineFlux = 10_000_000f; 
            float fBaselineHeight = 20.0f;

            float fRatio = fHeatFlux / fBaselineFlux;
            return Math.Clamp(fBaselineHeight * fRatio, 5.0f, 50.0f);
        }

        public static int nCalculateFinCount(float fDiameter, float fMinAirGap)
        {
            float fCircumference = MathF.PI * fDiameter;
            return (int)(fCircumference / (fMinAirGap * 2));
        }
    }
}