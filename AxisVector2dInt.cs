using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using PlanBee;

namespace PlanBee
{
    //not currently using this class 

    public struct AxisVector2dInt : IEquatable<AxisVector2dInt>
    { 
            public PBUtilities.Axis Axis;
            public Vector2dInt Index;

            public AxisVector2dInt(PBUtilities.Axis axis, int x, int y)
            {
                Index = new Vector2dInt(x, y);
                Axis = axis;
            }

            public bool Equals(AxisVector2dInt other)
            {
                return Index == other.Index && Axis == other.Axis;
            }

            public override int GetHashCode()
            {
                return Index.GetHashCode() ^ (Axis.GetHashCode() << 2);
            }
        
    }
}