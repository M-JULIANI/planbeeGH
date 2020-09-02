using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace PlanBee
{
    public class PlanBeeCategoryIcon : GH_AssemblyPriority
    {
        public override GH_LoadingInstruction PriorityLoad()
        {
            Grasshopper.Instances.ComponentServer.AddCategoryIcon("PlanBee", PlanBee.Properties.Resources.PlanBee100_75_01_01);
            Grasshopper.Instances.ComponentServer.AddCategorySymbolName("PlanBee", 'P');
            return Grasshopper.Kernel.GH_LoadingInstruction.Proceed;
        }
    }
}