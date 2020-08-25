using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Planbee;
using System.Linq;

namespace PlanBee
{
    //not currently using this class
    public class Grid2d
    {
        public Dictionary<Vector2dInt, SmartCell> Voxels = new Dictionary<Vector2dInt, SmartCell>();
        public Dictionary<AxisVector2dInt, Face> Faces = new Dictionary<AxisVector2dInt, Face>();

        public double VoxelSize;
        
        public Grid2d(double voxelSize = 2.0)
        {
            VoxelSize = voxelSize;
        }

        public IEnumerable<SmartCell> GetVoxels()
        {
            return Voxels.Select(v => v.Value);
        }

        public IEnumerable<Face> GetFaces()
        {
            return Faces.Select(v => v.Value);
        }

        public bool AddVoxel(Vector2d position, Vector2dInt index, double resolution)
        {
            if (Voxels.TryGetValue(index, out _)) return false;

            var voxel = new SmartCell(position, resolution, this);
            Voxels.Add(index, voxel);

            int x = index.X;
            int y = index.Y;
            var indices = new[]
                {
                  new AxisVector2dInt(PBUtilities.Axis.X, x - 1, y),
                  new AxisVector2dInt(PBUtilities.Axis.X, x + 1, y),
                  new AxisVector2dInt(PBUtilities.Axis.Y, x, y - 1),
                  new AxisVector2dInt(PBUtilities.Axis.Y, x, y + 1),
              
                };

            foreach (var i in indices)
            {
                if (Faces.TryGetValue(i, out _)) continue;

                var face = new Face(i.Index.X, i.Index.Y, i.Axis, this);
                Faces.Add(i, face);
            }

            return true;
        }
    }
}