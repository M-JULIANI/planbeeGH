using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace PlanBee
{
    public class Ghc_PlanCells : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the TestSPlan class.
        /// </summary>
        public Ghc_PlanCells()
          : base("Plan Cells", "Cells",
              "Plan voxels/ 'analysis grid' serving as the basic unit for which different analysis metrics are computed",
              "PlanBee", "Inputs")
        {
        }

        int IN_plane;
        int IN_coreMode;
        int IN_perimCurve;
        int IN_coreCurves;
        int IN_resolution;
        int IN_leaseSpan;
        int IN_areas;

        int OUT_cells;
        int OUT_core;
        int OUT_perimeter;
        int OUT_resolution;
        int OUT_areaFeedback;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IN_areas = pManager.AddNumberParameter("Program Areas", "Areas", "List of total program areas to ensure compliance with. If no error is thrown, areas are compliant", GH_ParamAccess.list);
            IN_plane = pManager.AddPlaneParameter("Base Plane", "Plane", "The base plane for the floor plan under analysis", GH_ParamAccess.item, Plane.WorldXY);
            pManager[1].Optional = true;
            IN_coreMode = pManager.AddIntegerParameter("Core Mode", "Mode", "0: Provide core curve , 1: Offset core using lease span. If no mode is provided a standard offset is used", GH_ParamAccess.item, 1);
            IN_perimCurve = pManager.AddCurveParameter("Perimeter Curve", "Perimeter", "The curve that describes the extents of the floor plan boundary", GH_ParamAccess.item);
            IN_coreCurves = pManager.AddCurveParameter("Core Curve(s)", "Core Curve(s)", "The curves that describe the extents of the core boundaries", GH_ParamAccess.list);
            IN_resolution = pManager.AddNumberParameter("Target resolution", "Resolution", "Resolution of smart plan within reasonable range. The component takes care of the subdivisions not being too high or too low.", GH_ParamAccess.item);
            IN_leaseSpan = pManager.AddNumberParameter("Target lease span", "Lease span", "If a core curve is not provided, a target lease span kicks in to define a core boundary", GH_ParamAccess.item, 25);

            
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            OUT_areaFeedback = pManager.AddTextParameter("Area feedback", "Area feedback", "Provides feedback as to whether desired areas are being met with current configuration", GH_ParamAccess.item);
            OUT_cells = pManager.AddRectangleParameter("Plan Voxels", "Voxels", "Analysis unit size", GH_ParamAccess.list);
            OUT_perimeter = pManager.AddCurveParameter("Perimeter Curve", "Perimeter", "The curve that describes the extents of the floor plan boundary", GH_ParamAccess.item);
            OUT_core = pManager.AddCurveParameter("Core Curve", "Core", "The curve that describes the extents of the core boundaries", GH_ParamAccess.list);
            OUT_resolution = pManager.AddNumberParameter("Resolution", "Resolution", "This final resolution may differ from the input resolution as this component takes care of ensuring the resolution isn't too fine or coarse.", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            Curve perimeter = null;
            
            double _resolution = double.NaN;
            List<Point3d> exitPts = new List<Point3d>();
            double leaseSpan = double.NaN;
            int coreMode = 1;
            Plane plane = Plane.Unset;
            List<double> areas = new List<double>();
            double totalArea = 0.0;

            DA.GetDataList(IN_areas, areas);

            for (int i = 0; i < areas.Count; i++)
                totalArea += areas[i];

            DA.GetData(IN_plane, ref plane);
            bool cMode = DA.GetData(IN_coreMode, ref coreMode);
            DA.GetData(IN_perimCurve, ref perimeter);

            //List<int> myStuff = new List<int>();
            //if (!DA.GetDataList<int>(0, myStuff)) { return; }

            List<Curve> coreCrvs = new List<Curve>();
            DA.GetDataList(IN_coreCurves, coreCrvs);
            DA.GetData(IN_resolution, ref _resolution);

            if (_resolution < 1.0)
                _resolution = 0.5;
            else if (_resolution == 2.0) //2.0 providing bugs..
                _resolution = 2.1;

            bool successLease = DA.GetData(IN_leaseSpan, ref leaseSpan);

            SmartPlan _plan;

            try
            {
                if (cMode == false) //if no mode specified
                    coreMode = 1;

                if (coreMode == 0)
                    _plan = new SmartPlan(perimeter, coreCrvs, _resolution, plane);
                else
                    _plan = new SmartPlan(perimeter, leaseSpan, _resolution, plane);


                var _cells = _plan.getCells();

                var calcArea = _cells.Count * _plan._resolution * _plan._resolution;
                int diff = 0;
                string message = "None";

                if (calcArea < totalArea)
                {
                    diff = (int)Math.Round(totalArea - calcArea);
                    message = String.Format("Plan areas don't meet those specified in the .csv by {0}, either reduce required areas or make plan footprint larger", diff);
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, message);
                    DA.SetData(OUT_areaFeedback, message);
                }
                else
                {
                    diff = (int)Math.Abs(Math.Round(totalArea - calcArea));
                    message = String.Format("Plan areas satisfied and over by {0}", diff);
                    DA.SetData(OUT_areaFeedback, message);
                }

                
                DA.SetDataList(OUT_cells, _cells);
                DA.SetDataList(OUT_core, _plan._coreCurves);
                DA.SetData(OUT_perimeter, _plan.perimCurve);
                DA.SetData(OUT_resolution, _plan._resolution);
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
                return PlanBee.Properties.Resources.Cells;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("bbc8d682-38b9-4038-b039-eaa5046d2cd5"); }
        }
    }
}