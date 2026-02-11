// FILE: LSystemPlant.cs

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using PicoGK;
using Leap71.ShapeKernel;
using MyFirstApp.Core;
using MyFirstApp.Core.Engine;

namespace MyFirstApp.Algorithms.Playground
{
    // ALGORITHM 2: L-Systems (Lindenmayer Systems)
    // This is the definitive, corrected version with a fully functional 3D turtle
    // that moves and rotates correctly in its own local coordinate system.
    public class LSystem : EngineeringComponent
    {
        protected int   m_nIterations   = 4;
        protected float m_fAngle        = 25f;
        protected float m_fStepSize     = 20f;
        protected float m_fThickness    = 2f;

        public LSystem() { Name = "ALGORITHM: L-System Plant"; }

        protected override List<Parameter> GetComponentParameters() => new List<Parameter>
        {
            new Parameter { Name = "Iterations", Value = m_nIterations, Min = 1, Max = 6, OnChange = v => m_nIterations = (int)v },
            new Parameter { Name = "Angle (deg)", Value = m_fAngle, Min = 10, Max = 90, OnChange = v => m_fAngle = v },
            new Parameter { Name = "Step Size (mm)", Value = m_fStepSize, Min = 5, Max = 100, OnChange = v => m_fStepSize = v },
            new Parameter { Name = "Thickness (mm)", Value = m_fThickness, Min = 1, Max = 15, OnChange = v => m_fThickness = v },
        };

        private string strGenerateLSystemString(string strAxiom, Dictionary<char, string> aRules, int nIterations)
        {
            string strCurrent = strAxiom;
            var oStringBuilder = new StringBuilder();
            for (int i = 0; i < nIterations; i++)
            {
                oStringBuilder.Clear();
                foreach (char c in strCurrent)
                {
                    if (aRules.ContainsKey(c)) { oStringBuilder.Append(aRules[c]); }
                    else { oStringBuilder.Append(c); }
                }
                strCurrent = oStringBuilder.ToString();
            }
            return strCurrent;
        }
        
        protected override void OnConstruct(EngineeringContext ctx)
        {
            Library.Log("\n--- Starting L-System Construction ---");

            // 1. DEFINE THE L-SYSTEM RULES
            string strAxiom = "X";
            var aRules = new Dictionary<char, string>
            {
                { 'X', "F-[[X]+X]+F[+FX]-X" },
                { 'F', "FF" }
            };
            
            // 2. GENERATE THE INSTRUCTION STRING
            string strInstructions = strGenerateLSystemString(strAxiom, aRules, m_nIterations);
            Library.Log($"String generated with length: {strInstructions.Length}");

            // 3. INTERPRET THE STRING WITH A CORRECT 3D TURTLE
            var oLattice        = new Lattice();
            var oTurtleStates   = new Stack<LocalFrame>();
            // Start turtle at origin, pointing UP along the WORLD Y-axis.
            // Local Z (Forward) = World Y
            // Local X (Right)   = World X
            // Local Y (Up)      = World Z
            var oCurrentFrame   = new LocalFrame(Vector3.Zero, Vector3.UnitY, Vector3.UnitX);
            float fAngleRad     = DegreesToRadians(m_fAngle);
            float fCurrentThickness = m_fThickness * MathF.Pow(1.5f, m_nIterations);

            foreach (char c in strInstructions)
            {
                switch (c)
                {
                    case 'F':
                        // ** THE CRITICAL FIX IS HERE **
                        // To move forward, we get the turtle's current position and its local "forward" vector.
                        Vector3 vecStart    = oCurrentFrame.vecGetPosition();
                        Vector3 vecForward  = oCurrentFrame.vecGetLocalZ();
                        Vector3 vecNewPos   = vecStart + vecForward * m_fStepSize;
                        
                        // Create a new frame at the new position but with the same orientation.
                        // This correctly "walks" the turtle forward instead of teleporting it.
                        oCurrentFrame = new LocalFrame(vecNewPos, oCurrentFrame.vecGetLocalZ(), oCurrentFrame.vecGetLocalX());
                        
                        Vector3 vecEnd = oCurrentFrame.vecGetPosition();
                        oLattice.AddBeam(vecStart, fCurrentThickness, vecEnd, fCurrentThickness * 0.7f, true);
                        break;
                    
                    // Rotations are performed around the turtle's OWN local axes.
                    case '+': // Turn Right (YAW): Rotate around the turtle's UP vector (Local Y)
                        oCurrentFrame = oCurrentFrame.oRotate(fAngleRad, oCurrentFrame.vecGetLocalY());
                        break;

                    case '-': // Turn Left (YAW): Rotate around the turtle's UP vector (Local Y)
                        oCurrentFrame = oCurrentFrame.oRotate(-fAngleRad, oCurrentFrame.vecGetLocalY());
                        break;
                        
                    case '&': // Pitch Down: Rotate around the turtle's RIGHT vector (Local X)
                        oCurrentFrame = oCurrentFrame.oRotate(fAngleRad, oCurrentFrame.vecGetLocalX());
                        break;

                    case '^': // Pitch Up: Rotate around the turtle's RIGHT vector (Local X)
                        oCurrentFrame = oCurrentFrame.oRotate(-fAngleRad, oCurrentFrame.vecGetLocalX());
                        break;
                    
                    case '\\': // Roll Right: Rotate around the turtle's FORWARD vector (Local Z)
                        oCurrentFrame = oCurrentFrame.oRotate(fAngleRad, oCurrentFrame.vecGetLocalZ());
                        break;
                    
                    case '/': // Roll Left: Rotate around the turtle's FORWARD vector (Local Z)
                        oCurrentFrame = oCurrentFrame.oRotate(-fAngleRad, oCurrentFrame.vecGetLocalZ());
                        break;

                    case '[': // Save current state
                        oTurtleStates.Push(oCurrentFrame);
                        fCurrentThickness *= 0.75f;
                        break;

                    case ']': // Restore last saved state
                        if (oTurtleStates.Count > 0)
                        {
                            oCurrentFrame = oTurtleStates.Pop();
                            fCurrentThickness /= 0.75f;
                        }
                        break;
                }
            }
            Library.Log("Turtle interpretation complete.");

            // 4. CONSTRUCT THE FINAL GEOMETRY
            Voxels vPlant = new Voxels(oLattice);
            vPlant.Smoothen(m_fThickness * 0.5f);
            ctx.Assembly.BoolAdd(vPlant);
            Library.Log("--- L-System Construction Complete ---");
        }

        private float DegreesToRadians(float degrees) => degrees * (MathF.PI / 180f);
    }
}