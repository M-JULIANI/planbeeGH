﻿using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace PlanBee
{
    public class Ghc_SimpleCells : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the GhcSimpleCells class.
        /// </summary>
        public Ghc_SimpleCells()
          : base("Simple Plan Cells", "Simple Cells",
              "Plan voxels/ 'analysis grid' serving as the basic unit for which different analysis metrics are computed." +
                "This component takes a perimeter curve and some core/stair/hole curves. This is for when no program information is " +
                "required.",
              "PlanBee", "Inputs")
        {
        }

        int IN_plane;
        int IN_perimCurve;
        int IN_coreCurves;
        int IN_resolution;

        int OUT_cells;
        int OUT_resolution;

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
                _plan = new SmartPlan(perimeter, coreCrvs, _resolution, plane);
                var _cells = _plan.getCells();
                int totalNeighbors = 0;
                foreach(var c in _plan.Cells)
                {
                    var neighCount =_plan.GetNeighbors(c.Value).Count();
                    totalNeighbors += neighCount;
                }

                DA.SetDataList(OUT_cells, _cells);
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
                return PlanBee.Properties.Resources.SimplifiedCells_01;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("6917b987-c63f-4365-bcdd-dd0cc5ce20cb"); }
        }
    }
}