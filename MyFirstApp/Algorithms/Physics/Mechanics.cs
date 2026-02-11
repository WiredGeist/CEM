using System;

namespace MyFirstApp.Algorithms.Physics
{
    public static class Mechanics
    {
        /// <summary>
        /// Calculates minimum wall thickness using Hoop Stress formula.
        /// </summary>
        public static float fGetWallThickness(float fPressureBar, float fRadiusMM, float fMaterialYieldStrengthMPa)
        {
            // 1 Bar = 0.1 MPa
            float fPressureMPa = fPressureBar * 0.1f;
            
            // Safety Factor for aerospace is typically 1.5 to 2.0
            float fSafetyFactor = 2.0f;

            // t = (P * r) / S
            float fThickness = (fPressureMPa * fRadiusMM) / fMaterialYieldStrengthMPa;
            
            // Apply safety factor and clamp to a minimum printable wall (e.g. 2mm)
            return Math.Max(2.0f, fThickness * fSafetyFactor);
        }
    }
}