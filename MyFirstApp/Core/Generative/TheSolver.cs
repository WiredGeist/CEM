using System;
using System.Numerics;
using PicoGK;
using MyFirstApp.Algorithms.Physics; // For physics math

namespace MyFirstApp.Core.Generative
{
    public static class TheSolver
    {
        public static Blueprint Solve(DesignRequirements reqs)
        {
            Blueprint bp = new Blueprint();
            string type = reqs.GetText("Type");

            // =========================================================
            // GOAL: CONTAINER (e.g. Coffee Cup, Tank)
            // =========================================================
            if (type == "Container")
            {
                // 1. GET REQUIREMENTS
                float volume = reqs.GetNum("VolumeML", 300);
                float temp = reqs.GetNum("MaxTemp", 20);
                
                // 2. APPLY PHYSICS (Calculate Dimensions)
                // Volume = PI * r^2 * h. Constraint: Height = 2.5 * Radius
                // r = CubeRoot(Vol / 2.5 PI)
                float radius = MathF.Pow((volume * 1000f) / (2.5f * MathF.PI), 1f/3f);
                float height = radius * 2.5f;

                // 3. APPLY PHYSICS (Material Constraints)
                float wallThick = 2.0f; // Standard
                bool activeCooling = false;

                if (temp > 60) 
                {
                    wallThick = 4.0f; // Thicker for hot liquid
                }
                if (temp > 1000)
                {
                    wallThick = 10.0f; // Very thick for molten metal
                    activeCooling = true; // Needs infill
                }

                // 4. GENERATE STEPS
                // A. Base Shape
                bp.Steps.Add(new Step_Cylinder(radius, height));

                // B. Modifiers based on conditions
                if (activeCooling)
                {
                    // Create a hollow shell, then fill with lattice
                    bp.Steps.Add(new Step_HollowShell(wallThick));
                    bp.Steps.Add(new Step_GyroidInfill(5.0f, 1.0f));
                }
                else
                {
                    // Just a standard cup
                    bp.Steps.Add(new Step_HollowShell(wallThick));
                }

                // C. Finalization
                bp.Steps.Add(new Step_OpenTop(height));
            }

            // =========================================================
            // GOAL: WING
            // =========================================================
            else if (type == "Wing")
            {
                // Placeholder for future wing logic
                // float lift = reqs.GetNum("LiftKG", 100);
            }

            return bp;
        }
    }
}