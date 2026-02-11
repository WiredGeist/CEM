using System;

namespace MyFirstApp.Algorithms.Physics
{
    public static class GasDynamics
    {
        // --- YOUR EXISTING CONSTANTS ---
        private const float GAMMA = 1.22f;          // Specific Heat Ratio
        private const float R_UNIV = 8314.46f;      // Universal Gas Constant
        private const float MOLAR_MASS = 24.0f;     // Average Molecular Mass
        private const float T_CHAMBER = 3300f;      // Combustion Temp (Kelvin)

        // ====================================================================
        // PART 1: YOUR SIMULATION LOGIC (Preserved)
        // ====================================================================

        public static float CalculateExhaustVelocity(float fChamberPressureBar, float fExitPressureBar)
        {
            if (fExitPressureBar >= fChamberPressureBar) return 0f;

            float term1 = (2f * GAMMA) / (GAMMA - 1f);
            float term2 = (R_UNIV * T_CHAMBER) / MOLAR_MASS;
            
            float pressureRatio = fExitPressureBar / fChamberPressureBar;
            float exponent = (GAMMA - 1f) / GAMMA;
            float term3 = 1f - MathF.Pow(pressureRatio, exponent);

            return MathF.Sqrt(term1 * term2 * term3);
        }

        public static float CalculateMassFlow(float fChamberPressureBar, float fThroatRadiusMM)
        {
            float P_pa = fChamberPressureBar * 100000f; 
            float At_m2 = MathF.PI * MathF.Pow(fThroatRadiusMM / 1000f, 2); 
            float c_star = 1750f; 

            return (P_pa * At_m2) / c_star; 
        }

        public static float CalculateThrust(float m_dot, float ve, float fExitPressureBar, float fAmbientPressureBar, float fExitRadiusMM)
        {
            float momentumThrust = m_dot * ve;
            float P_exit_Pa = fExitPressureBar * 100000f;
            float P_amb_Pa = fAmbientPressureBar * 100000f;
            float A_exit_m2 = MathF.PI * MathF.Pow(fExitRadiusMM / 1000f, 2);

            float pressureThrust = (P_exit_Pa - P_amb_Pa) * A_exit_m2;

            return (momentumThrust + pressureThrust) / 1000f; // Return in kN
        }

        public static float CalculateExpansionRatio(float fThroatR, float fExitR)
        {
            float At = fThroatR * fThroatR;
            float Ae = fExitR * fExitR;
            return Ae / At;
        }

        // ====================================================================
        // PART 2: THE MISSING HELPERS (Required for Geometry Construction)
        // ====================================================================

        /// <summary>
        /// Inverse Calculation: What Throat Area (m2) do I need for a specific Thrust?
        /// Uses a simplified Cf (Thrust Coefficient) estimation.
        /// </summary>
        public static float CalculateThroatArea(float targetThrustNewtons, float chamberPressurePa)
        {
            // F = P * A * Cf. 
            // Cf is roughly 1.6 for a well-expanded nozzle.
            float Cf = 1.6f; 
            return targetThrustNewtons / (chamberPressurePa * Cf);
        }

        /// <summary>
        /// Calculates Exit Area (m2) from Throat Area and Ratio.
        /// </summary>
        public static float CalculateExitArea(float throatAreaM2, float expansionRatio)
        {
            return throatAreaM2 * expansionRatio;
        }

        /// <summary>
        /// Helper to convert Area (m2) to Radius (mm).
        /// </summary>
        public static float AreaToRadiusMM(float areaM2)
        {
            float r_meters = MathF.Sqrt(areaM2 / MathF.PI);
            return r_meters * 1000.0f;
        }

        /// <summary>
        /// Calculates Wall Thickness based on Hoop Stress.
        /// </summary>
        public static float CalculateWallThickness(float pressureBar, float radiusMM)
        {
            float pressureMPa = pressureBar * 0.1f;
            float yieldStrength = 900f; // Inconel 718 Yield
            float safetyFactor = 2.0f;

            float t = (pressureMPa * radiusMM * safetyFactor) / yieldStrength;
            
            // Min printable wall 2.0mm
            return Math.Max(t, 2.0f);
        }
    }
}