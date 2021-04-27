using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace PlanBee.GH_Components
{
    public class Ghc_Convexity:GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GhcSimpleCells class.
        /// </summary>
        public Ghc_Convexity()
          : base("Convexity", "Convexity",
              "Simple measure of convexity.",
              "PlanBee", "Analysis")
        {
        }

        int IN_plane;
        int IN_perimCurve;
        int IN_coreCurves;
        int IN_resolution;

        int OUT_cells;
        int OUT_convexity;
        int OUT_poly;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IN_plane = pManager.AddPlaneParameter("Base Plane", "Plane", "The base plane for the floor plan under analysis", GH_ParamAccess.item, Plane.WorldXY);
            IN_perimCurve = pManager.AddCurveParameter("Perimeter Curve", "Perimeter", "The curve that describes the extents of the floor plan boundary", GH_ParamAccess.item);
            IN_coreCurves = pManager.AddCurveParameter("Core Curve(s)", "Core Curve(s)", "The curves that describe the extents of the core boundaries", GH_ParamAccess.list);
            IN_resolution = pManager.AddNumberParameter("Target resolution", "Resolution", "Resolution of smart plan within reasonable range. The component takes care of the subdivisions not being too high or too low.", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            OUT_cells = pManager.AddRectangleParameter("Plan Voxels", "Voxels", "Analysis unit size", GH_ParamAccess.list);
            OUT_convexity = pManager.AddNumberParameter("Convexity", "Convexity", "Convexity - Area shape / area convex hull of shape.", GH_ParamAccess.item);
            OUT_poly = pManager.AddCurveParameter("Convex Hull", "Convex Hull", "Convex Hull.", GH_ParamAccess.item);

        }


        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve perimeter = null;
            double _resolution = double.NaN;
            Plane plane = Plane.Unset;

            DA.GetData(IN_plane, ref plane);
            DA.GetData(IN_perimCurve, ref perimeter);

            List<Curve> coreCrvs = new List<Curve>();
            DA.GetDataList(IN_coreCurves, coreCrvs);
            DA.GetData(IN_resolution, ref _resolution);

            if (_resolution < 1.0)
                _resolution = 0.5;
            else if (_resolution == 2.0) //2.0 providing bugs..
                _resolution = 2.1;

            SmartPlan _plan;

            try
            {
              //  _plan = new SmartPlan(perimeter, coreCrvs, _resolution, plane);
                _plan = new SmartPlan(perimeter, coreCrvs, _resolution, plane);
                var _cells = _plan.getCells();

                Polyline convexHull;
                var convexity = _plan.GetConvexity(out convexHull);

                DA.SetDataList(OUT_cells, _cells);
                DA.SetData(OUT_convexity, convexity);
                DA.SetData(OUT_poly, convexHull);
            }
            catch (Exception e)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.ToString());
            }
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
            get { return new Guid("4c736ad3-03ec-49f7-9e08-c0ca3f2f8deb"); }
        }
    }
}
