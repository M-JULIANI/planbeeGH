using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace PlanBee
{
    public class Ghc_ExitAccessBetween : GH_Component
    {
        bool autoColor = false;
        SmartPlan _plan;
        List<Rectangle3d> rectangles = new List<Rectangle3d>();
        System.Drawing.Color[] gradientList;
        double[] _exitMetric;

        /// <summary>
        /// Initializes a new instance of the Ghc_ExitAccessBetweencs class.
        /// </summary>
        public Ghc_ExitAccessBetween()
          : base("Exit Paths Most Travelled", "Exit Paths Most Travelled",
              "The frequency with which each cell is used in the cumulative exiting of the entire analysis grid",
              "PlanBee", "Analysis")
        {
        }


        int IN_AutoColor;
        int IN_plane;
        int IN_rects;
        int IN_partitions;
        int IN_exitPts;

        int OUT_paths;
        int OUT_pathMetric;

        public override GH_Exposure Exposure => GH_Exposure.primary;

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

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("32e834ee-d792-49de-a342-67e370c2e848"); }
        }
    }
}