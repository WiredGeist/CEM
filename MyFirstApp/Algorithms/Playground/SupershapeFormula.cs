// FILE: SupershapeFormula.cs

using System;
using System.Numerics;
using PicoGK;
using Leap71.ShapeKernel;
using MyFirstApp.Core;
using MyFirstApp.Core.Engine;

namespace MyFirstApp.Algorithms.Playground
{
    // ALGORITHM 1: Supershape Formula
    // CONCEPT: A single, powerful generalization of the sphere. By changing a few parameters
    // (m, n1, n2, n3), this formula can generate an incredible variety of complex, beautiful
    // shapes that look like anything from flowers and starfish to futuristic gears.
    //
    // IMPLEMENTATION: We start with a simple BaseSphere. We then apply a transformation that
    // runs on every point of the sphere's surface. For each point, we calculate what its radius
    // *should* be according to the supershape formula, and move the point to that new radius.
    public class SupershapeFormula : EngineeringComponent
    {
        // --- Supershape Parameters ---
        protected float m_fRadius   = 200f; // Overall size
        protected float m_fM_Lat    = 6f;   // 'm' for latitude (around the equator) - determines major corners
        protected float m_fN1_Lat   = 1f;
        protected float m_fN2_Lat   = 1f;
        protected float m_fN3_Lat   = 1f;
        
        protected float m_fM_Lon    = 3f;   // 'm' for longitude (pole to pole)
        protected float m_fN1_Lon   = 2f;
        protected float m_fN2_Lon   = 2f;
        protected float m_fN3_Lon   = 2f;


        public SupershapeFormula() 
        {
            Name = "ALGORITHM: Supershape"; 
        }

        protected override List<Parameter> GetComponentParameters() => new List<Parameter>
        {
            new Parameter { Name = "Radius (mm)", Value = m_fRadius, Min = 50, Max = 500, OnChange = v => m_fRadius = v },
            new Parameter { Name = "Lat m (Corners)", Value = m_fM_Lat, Min = 0, Max = 20, OnChange = v => m_fM_Lat = v },
            new Parameter { Name = "Lat n1", Value = m_fN1_Lat, Min = -10, Max = 20, OnChange = v => m_fN1_Lat = v },
            new Parameter { Name = "Lat n2", Value = m_fN2_Lat, Min = -10, Max = 20, OnChange = v => m_fN2_Lat = v },
            new Parameter { Name = "Lat n3", Value = m_fN3_Lat, Min = -10, Max = 20, OnChange = v => m_fN3_Lat = v },
            
            new Parameter { Name = "Lon m (Lobes)", Value = m_fM_Lon, Min = 0, Max = 20, OnChange = v => m_fM_Lon = v },
            new Parameter { Name = "Lon n1", Value = m_fN1_Lon, Min = -10, Max = 20, OnChange = v => m_fN1_Lon = v },
            new Parameter { Name = "Lon n2", Value = m_fN2_Lon, Min = -10, Max = 20, OnChange = v => m_fN2_Lon = v },
            new Parameter { Name = "Lon n3", Value = m_fN3_Lon, Min = -10, Max = 20, OnChange = v => m_fN3_Lon = v },
        };
        
        protected override void OnConstruct(EngineeringContext ctx)
        {
            Library.Log("\n--- Starting Supershape Construction ---");

            // 1. Create a base sphere. This is our "rough stone".
            var oSphere = new BaseSphere(new LocalFrame(), m_fRadius);
            oSphere.SetAzimuthalSteps(200); // Increase resolution for detail
            oSphere.SetPolarSteps(200);

            // 2. Define the transformation function that applies the 3D Supershape formula.
            BaseShape.fnVertexTransformation fnShaper = (vecPt) =>
            {
                // Get the spherical coordinates of the original point on the sphere.
                // Phi is the angle in the XY plane (latitude).
                // Theta is the angle from the Z-axis (longitude).
                float fPhi      = VecOperations.fGetPhi(vecPt);
                float fTheta    = VecOperations.fGetTheta(vecPt);

                // ALGORITHM: Calculate the supershape radius for both latitudinal and longitudinal directions.
                // The ShapeKernel library provides this complex formula for us.
                float fRadius_Lat = Uf.fGetSuperShapeRadius(fPhi,   m_fM_Lat, m_fN1_Lat, m_fN2_Lat, m_fN3_Lat);
                float fRadius_Lon = Uf.fGetSuperShapeRadius(fTheta, m_fM_Lon, m_fN1_Lon, m_fN2_Lon, m_fN3_Lon);

                // The final 3D shape is created by multiplying the two results.
                float fFinalRadius = m_fRadius * fRadius_Lat * fRadius_Lon;

                // Move the point to its new position.
                return VecOperations.vecSetRadius(vecPt, fFinalRadius);
            };

            // 3. Apply the transformation to the sphere.
            oSphere.SetTransformation(fnShaper);

            // 4. Construct the final object.
            Library.Log("Constructing Supershape...");
            Mesh mShape = oSphere.mshConstruct();
            
            if (mShape.nTriangleCount() == 0)
            {
                Library.Log("!!! MESHING FAILED.");
                return;
            }

            Voxels vShape = new Voxels(mShape);
            ctx.Assembly.BoolAdd(vShape);
            Library.Log("--- Supershape Construction Complete ---");
        }
    }
}