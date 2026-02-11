using System;
using System.Numerics;
using PicoGK;
using Leap71.ShapeKernel;

namespace MyFirstApp.Core.Generative
{
    // 1. Basic Cylinder Step (Using ShapeKernel BaseCylinder)
    public class Step_Cylinder : IBldStep
    {
        float R, H;
        public Step_Cylinder(float r, float h) { R = r; H = h; }
        
        public void Execute(Voxels v) 
        {
            // FIX: Using BaseCylinder avoids the "float to int" error in Utils
            LocalFrame frame = new LocalFrame(new Vector3(0,0,0));
            BaseCylinder cyl = new BaseCylinder(frame, H, R);
            
            Voxels voxCyl = cyl.voxConstruct();
            v.BoolAdd(voxCyl);
        }
    }

    // 2. Hollow Shell Step (Using Voxel Offset)
    public class Step_HollowShell : IBldStep
    {
        float Wall;
        public Step_HollowShell(float w) { Wall = w; }
        
        public void Execute(Voxels v) 
        {
            Voxels inner = new Voxels(v);
            inner.Offset(-Wall); // Shrink
            v.BoolSubtract(inner); // Cut
        }
    }

    // 3. Open Top Step (Using BaseBox)
    public class Step_OpenTop : IBldStep
    {
        float Height;
        public Step_OpenTop(float h) { Height = h; }
        
        public void Execute(Voxels v) 
        {
            // Create a big box at the top to slice it open
            LocalFrame frame = new LocalFrame(new Vector3(0, 0, Height));
            BaseBox cutter = new BaseBox(frame, 100f, 500f, 500f); // Length=100 (up), Width/Depth=500
            
            v.BoolSubtract(cutter.voxConstruct());
        }
    }

    // 4. Gyroid Step (Simplified)
    public class Step_GyroidInfill : IBldStep
    {
        float Unit, Thick;
        public Step_GyroidInfill(float unit, float thick) { Unit = unit; Thick = thick; }

        public void Execute(Voxels v)
        {
            // Logic: Create a lattice grid inside the volume
            // (Simplified version to ensure compilation without external Gyroid class)
            Voxels mask = new Voxels(v);
            mask.Offset(-2.0f); // Infill area

            Lattice lat = new Lattice();
            BBox3 b = mask.mshAsMesh().oBoundingBox();
            
            // Simple X/Y Grid Lattice (Placeholder for Gyroid)
            for(float x = b.vecMin.X; x < b.vecMax.X; x+=Unit)
            {
                for(float y = b.vecMin.Y; y < b.vecMax.Y; y+=Unit)
                {
                    lat.AddBeam(new Vector3(x, y, b.vecMin.Z), new Vector3(x, y, b.vecMax.Z), Thick, Thick, true);
                }
            }
            
            Voxels voxLat = new Voxels(lat);
            voxLat.BoolIntersect(mask); // Trim to shape
            v.BoolAdd(voxLat);
        }
    }
}