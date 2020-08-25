using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Planbee;

namespace PlanBee
{
    //not currently using this class
    public class Face
    {
        public SmartCell[] Voxels => GetVoxels();
        public Vector2dInt Index;
        public Vector2d Center;
        public PBUtilities.Axis Direction;

        Grid2d _grid;

        public bool IsActive => Voxels.Count(v => v != null && v.isActive) == 2;

        public PBUtilities.BoundaryType Boundary
        {
            get
            {
                bool left = Voxels[0]?.isActive == true;
                bool right = Voxels[1]?.isActive == true;

                if (!left && right) return PBUtilities.BoundaryType.Left;
                if (left && !right) return PBUtilities.BoundaryType.Right;
                if (left && right) return PBUtilities.BoundaryType.Inside;
                return PBUtilities.BoundaryType.Outside;
            }
        }

        public Vector3d Normal
        {
            get
            {
                int f = (int)Boundary;
                if (Boundary == PBUtilities.BoundaryType.Outside) f = 0;

                if (Index.Y == 0 && Direction == PBUtilities.Axis.Y)
                {
                    f = Boundary == PBUtilities.BoundaryType.Outside ? 1 : 0;
                }

                switch (Direction)
                {
                    case PBUtilities.Axis.X:
                        return Vector3d.XAxis * f;
                    case PBUtilities.Axis.Y:
                        return Vector3d.YAxis * f;
                    default:
                        throw new Exception("Wrong direction.");
                }
            }
        }

        public bool IsClimbable
        {
            get
            {
                if (Index.Y == 0 && Direction == PBUtilities.Axis.Y)
                {
                    return Boundary == PBUtilities.BoundaryType.Outside;
                }

                return Boundary == PBUtilities.BoundaryType.Left || Boundary == PBUtilities.BoundaryType.Right;
            }
        }

        public Face(int x, int y, PBUtilities.Axis direction, Grid2d grid)
        {
            _grid = grid;
            Index = new Vector2dInt(x, y);
            Direction = direction;
            Center = GetCenter();
        }

        Vector2d GetCenter()
        {
            int x = Index.X;
            int y = Index.Y;

            switch (Direction)
            {
                case PBUtilities.Axis.X:
                    return new Vector2d(x, y + 0.5) * _grid.VoxelSize;
                case PBUtilities.Axis.Y:
                    return new Vector2d(x + 0.5, y) * _grid.VoxelSize;
                default:
                    throw new Exception("Wrong direction.");
            }
        }

        SmartCell[] GetVoxels()
        {
            int x = Index.X;
            int y = Index.Y;

            switch (Direction)
            {
                case PBUtilities.Axis.X:
                    {
                        return new[]
                        {
                            _grid.Voxels.TryGetValue(new Vector2dInt(x - 1, y), out var left) ? left : null,
                            _grid.Voxels.TryGetValue(new Vector2dInt(x, y), out var right) ? right : null,
                        };
                    }
                case PBUtilities.Axis.Y:
                    {
                        return new[]
                        {
                            _grid.Voxels.TryGetValue(new Vector2dInt(x, y - 1), out var left) ? left : null,
                             _grid.Voxels.TryGetValue(new Vector2dInt(x, y), out var right) ? right : null,
                        };
                    }
                default:
                    throw new Exception("Wrong direction.");
            }
        }

    }
}