using System;

namespace MyFirstApp.Algorithms.Physics
{
    public static class RocketMath
    {
        public static float CalculateMassFlow(float fPressureBar, float fThroatRadiusMM)
        {
            float P_pa = fPressureBar * 100000f; 
            float At_m2 = MathF.PI * MathF.Pow(fThroatRadiusMM / 1000f, 2);
            float c_star = 1750f;
            return (P_pa * At_m2) / c_star;
        }

        public static float CalculateIsp(float fThroatRadius, float fExitRadius, float fChamberRadius, float fLength)
        {
            float areaRatio = (fExitRadius * fExitRadius) / (fThroatRadius * fThroatRadius);
            float expansionEfficiency = 1.0f + (0.15f * MathF.Log10(Math.Max(1f, areaRatio)));
            float chamberVol_cm3 = (MathF.PI * fChamberRadius * fChamberRadius * fLength) / 1000f; 
            float combustionEfficiency = Math.Clamp(chamberVol_cm3 / 5000f, 0.8f, 1.0f);
            return 300f * expansionEfficiency * combustionEfficiency;
        }

        public static float CalculateThrustReal(float fMassFlow, float fIsp, float fExitRadiusMM, float fAmbientPressureBar)
        {
            float momentumThrust = fMassFlow * fIsp * 9.81f;
            float A_exit_m2 = MathF.PI * MathF.Pow(fExitRadiusMM / 1000f, 2);
            float P_ambient_Pa = fAmbientPressureBar * 100000f;
            float P_exit_Pa = 20000f; 
            float pressureThrust = (P_exit_Pa - P_ambient_Pa) * A_exit_m2;
            return (momentumThrust + pressureThrust) / 1000f; 
        }
    }
}