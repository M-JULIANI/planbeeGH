using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Display;
using System.Threading.Tasks;
using Planbee;

namespace PlanBee
{
    public class GhcMeanShortestPaths : GH_TaskCapableComponent<GhcMeanShortestPaths.SolveResults>
    {

        SmartPlan _plan;
        /// <summary>
        /// Initializes a new instance of the Ghc_MeanShortestPaths class.
        /// </summary>
        public GhcMeanShortestPaths()
          : base("Mean Shortest Paths", "MSP",
              "The shortest path from each cell to every other cell",
              "PlanBee", "Analysis")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
        }

        public class SolveResults
        {
            public SmartPlan Value { get; set; }
        }

        public static SolveResults ComputeMSP(SmartPlan plan)
        {
            SolveResults result = new SolveResults();
            plan.ComputeMeanShortestPaths();
            result.Value = plan;
            return result;
        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Properties.Resources.MeanShortestPath_01;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("77512942-8ce7-47cc-9118-eebdd8448467"); }
        }
    }
}