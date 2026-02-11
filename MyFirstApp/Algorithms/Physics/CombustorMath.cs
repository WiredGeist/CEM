using System;

namespace MyFirstApp.Algorithms.Physics
{
    public static class CombustorMath
    {
        // Based on "Design and Numerical Analysis of an Annular Combustion Chamber"
        // Equations 1 - 6

        /// <summary>
        /// Eq (1): Calculates Reference Area based on Mass Flow and Pressure/Temp.
        /// </summary>
        public static float CalculateReferenceArea(float massFlow, float tempIn, float pressureIn)
        {
            // Simplified approximation of Eq 1 for the software demo
            // A_ref = (R_gas * mass_flow * Sqrt(Temp)) / (Pressure * P_loss_factor)
            
            float R = 287f; // Gas constant for air
            float pressureLossFactor = 18f; // Recommended by paper for annular
            
            // Note: Pressure in Pa, Temp in K
            float numerator = massFlow * MathF.Sqrt(tempIn);
            float denominator = pressureIn * pressureLossFactor; // Simplified denominator
            
            // This is a rough scalar representation of Eq 1
            return (numerator / denominator) * 1000000f; // Scale up for visibility in mm units
        }

        /// <summary>
        /// Eq (2): Calculates Height of the section (Casing Height).
        /// </summary>
        public static float CalculateRefHeight(float refArea, float meanDiameter)
        {
            // h_ref = A_ref / (PI * D_m)
            return refArea / (MathF.PI * meanDiameter);
        }

        /// <summary>
        /// Eq (3): Calculates Flame Tube (Liner) Area.
        /// </summary>
        public static float CalculateFlameTubeArea(float refArea)
        {
            // A_ft = 0.66 * A_ref
            return 0.66f * refArea;
        }

        /// <summary>
        /// Eq (4): Calculates Flame Tube Height (The actual combustion space).
        /// </summary>
        public static float CalculateFlameTubeHeight(float flameTubeArea, float meanDiameter)
        {
            return flameTubeArea / (MathF.PI * meanDiameter);
        }

        /// <summary>
        /// Eq (28) & (29): Calculate Zoning Lengths.
        /// </summary>
        public static (float Primary, float Secondary, float Dilution) CalculateZoneLengths(float refHeight, float ftHeight)
        {
            // L_pz = 0.75 * h_ref
            float Lpz = 0.75f * refHeight;
            
            // L_sz = 0.5 * h_ft
            float Lsz = 0.5f * ftHeight;
            
            // L_dz estimated (Eq 34 uses complex TTQ, we simplify to 1.5x ft height)
            float Ldz = ftHeight * 1.5f;

            return (Lpz, Lsz, Ldz);
        }
    }
}