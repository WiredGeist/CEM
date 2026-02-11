using System;
using PicoGK;

namespace MyFirstApp.Algorithms.Physics
{
    public static class ThermalMath
    {
        public static ColorFloat GetHeatColor(float z, float height, float radiusAtZ, float throatRadius)
        {
            // Simple visual approximation: 
            // The closer the radius is to the throat radius, the hotter it is.
            float fRatio = throatRadius / radiusAtZ; // 1.0 at throat, smaller elsewhere
            
            // Exaggerate for contrast
            float heat = MathF.Pow(fRatio, 2.0f); 
            heat = Math.Clamp(heat, 0f, 1f);

            // 0 = Cold (Blue), 0.5 = Warm (Red), 1.0 = Hot (White/Yellow)
            if (heat < 0.3f)
            {
                // Steel/Blue
                return new ColorFloat(0.2f, 0.2f, 0.5f + heat); 
            }
            else if (heat < 0.8f)
            {
                // Red/Orange transition
                float t = (heat - 0.3f) / 0.5f;
                return new ColorFloat(0.5f + (0.5f * t), 0.2f, 0.2f);
            }
            else
            {
                // Glowing White/Yellow
                float t = (heat - 0.8f) / 0.2f;
                return new ColorFloat(1.0f, 0.5f + (0.5f*t), 0.5f + (0.5f*t));
            }
        }
    }
}