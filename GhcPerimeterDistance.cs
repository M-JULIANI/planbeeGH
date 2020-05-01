﻿using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Display;

namespace Planbee
{
    public class GhcPerimeterDistance : GH_Component
    {
        bool autoColor;
        SmartPlan _plan;
        List<Rectangle3d> rectangles;
        System.Drawing.Color[] gradientList;
        double[] perimeterDist;


        /// <summary>
        /// Initializes a new instance of the PerimeterDistance class.
        /// </summary>
        public GhcPerimeterDistance()
          : base("Perimeter Distance", "Perimeter Distance",
              "A temporary metric for each plan voxel to serve as a proxy for access to daylight. Has since been superseded with 'Daylight Access' component",
              "PlanBee", "Analysis")
        {

        }

        int IN_reset;
        int IN_plane;
        int IN_AutoColor;
        int IN_perimCurve;
        int IN_coreCurve;
        int IN_rects;


        int OUT_perimeterMetric;

        //void m_SolutionExpired(IGH_DocumentObject sender, GH_SolutionExpiredEventArgs e)
        //{
        //    // Checking if the sender is welcome in our house ;)
        //    // meaning: not connected to the input parameter
        //    _change = false;

        //    for (int i = 0; i < 5; i++)
        //    {
        //        if (this.Params.Input[i].Sources[0].Equals(sender) != true)
        //        {
        //            _change = true;
        //            break;
        //        }
        //    }

        //    if (_change)
        //    {
        //        // unregistering from this floating slider
        //        sender.SolutionExpired -= m_SolutionExpired;
        //        return;
        //    }

        //}
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IN_reset = pManager.AddBooleanParameter("Reset", "Reset", "Reset all vals", GH_ParamAccess.item, false);
            IN_AutoColor = pManager.AddBooleanParameter("Auto preview Perimeter Metric Visualization", "Autocolor Perimeter", "A build it analysis coloring of the voxels of the plan for the perimeter metric. Make sure to have the component preview on in order to view.", GH_ParamAccess.item, false);
            IN_plane = pManager.AddPlaneParameter("Base Plane", "Plane", "The base plane for the floor plan under analysis", GH_ParamAccess.item);
            pManager[2].Optional = true;
            IN_rects = pManager.AddRectangleParameter("Plan Voxels", "Voxels", "The rectangular voxels representing the analysis units of the floor plan", GH_ParamAccess.list);
            IN_perimCurve = pManager.AddCurveParameter("Perimeter Curve", "Perimeter", "The curve that describes the extents of the floor plan boundary", GH_ParamAccess.item);
            IN_coreCurve = pManager.AddCurveParameter("Core Curve", "Core", "The curve that describes the extentds of the core boundary", GH_ParamAccess.item);
          
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            OUT_perimeterMetric = pManager.AddNumberParameter("Normalized Perimeter Distance metric per voxel", "Normalized distance to perimeter", "The distance to perimeter metric of each cell, remapped from 0 to 1 using the bounds of the plan as remapping domain", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            Curve perimeter = null;
            Curve core = null;
            Plane plane = Plane.Unset;
            bool iReset = false;


            DA.GetData(IN_reset, ref iReset);
            rectangles = new List<Rectangle3d>();

            if (!DA.GetData(IN_plane, ref plane)) return;
            if (!DA.GetData(IN_AutoColor, ref autoColor)) return;
            if (!DA.GetData(IN_perimCurve, ref perimeter)) return;
            if (!DA.GetData(IN_coreCurve, ref core)) return;
            if (!DA.GetDataList(IN_rects, rectangles)) return;

            try
            {
                if (_plan == null)
                {
                    _plan = new SmartPlan(perimeter, core, rectangles, plane);
                    _plan.ComputeDistToPerimeter();
                }
            

                if (iReset)
                {

                    _plan = new SmartPlan(perimeter, core, rectangles, plane);
                    _plan.ComputeDistToPerimeter();

                }

                perimeterDist = _plan.getPerimDistances();

                DA.SetDataList(OUT_perimeterMetric, perimeterDist);
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
                return PlanBee.Properties.Resources.Perimeter;
            }
        }


        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {

            if (autoColor)
            {
                gradientList = new System.Drawing.Color[_plan.getCells().Count];
                for (int i = 0; i < gradientList.Length; i++)
                {
                    var multiplier = perimeterDist[i];

                    var gColor = new ColorHSL(multiplier, multiplier, 0, multiplier);
                    var rgb = gColor.ToArgbColor();
                    gradientList[i] = (rgb);
                }
                for (int i = 0; i < rectangles.Count; i++)
                {
                    Rhino.Display.DisplayMaterial mat = new Rhino.Display.DisplayMaterial(gradientList[i]);
                    mat.Shine = 0.25;
                    {
                        var curve = rectangles[i].ToNurbsCurve();
                        var pts = rectangles[i].ToPolyline();
                        args.Display.DrawPolyline(pts, gradientList[i], 1);
                        var mesh = Mesh.CreateFromPlanarBoundary(curve, Rhino.Geometry.MeshingParameters.FastRenderMesh, 0.01);
                        args.Display.DrawMeshShaded(mesh, mat);
                    }
                }

            }
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {

            if (autoColor)
            {
                gradientList = new System.Drawing.Color[_plan.getCells().Count];
                for (int i = 0; i < gradientList.Length; i++)
                {
                    var multiplier = perimeterDist[i];

                    var gColor = new ColorHSL(multiplier, multiplier, 0, multiplier);
                    var rgb = gColor.ToArgbColor();
                    gradientList[i] = (rgb);
                }
                for (int i = 0; i < rectangles.Count; i++)
                {
                    Rhino.Display.DisplayMaterial mat = new Rhino.Display.DisplayMaterial(gradientList[i]);
                    mat.Shine = 0.25;
                    {
                        var curve = rectangles[i].ToNurbsCurve();
                        var pts = rectangles[i].ToPolyline();
                        args.Display.DrawPolyline(pts, gradientList[i], 1);
                        var mesh = Mesh.CreateFromPlanarBoundary(curve, Rhino.Geometry.MeshingParameters.FastRenderMesh, 0.01);
                        args.Display.DrawMeshShaded(mesh, mat);
                    }
                }

            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("09554004-529e-460a-8111-4e139cd6877b"); }
        }
    }
}