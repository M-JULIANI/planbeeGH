﻿using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Display;
using GrasshopperAsyncComponent;

namespace PlanBee
{
    public class Ghc_PerimeterDistance : GH_AsyncComponent
    {
        //bool autoColor;
        //SmartPlan _plan;
        List<Rectangle3d> rectangles = new List<Rectangle3d>();
        //System.Drawing.Color[] gradientList;
        //double[] perimeterDist;


        /// <summary>
        /// Initializes a new instance of the PerimeterDistance class.
        /// </summary>
        public Ghc_PerimeterDistance()
          : base("Perimeter Distance", "Perimeter Distance",
              "A temporary metric for each plan voxel to serve as a proxy for access to daylight. Has since been superseded with 'Daylight Access' component",
              "PlanBee", "Analysis")
        {
            BaseWorker = new PerimeterCalculator();
        }

        int IN_plane;
        int IN_AutoColor;
        int IN_perimCurve;
        int IN_rects;

        int OUT_perimeterMetric;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IN_AutoColor = pManager.AddBooleanParameter("Auto preview Perimeter Metric Visualization", "Autocolor Perimeter", "A build it analysis coloring of the voxels of the plan for the perimeter metric. Make sure to have the component preview on in order to view.", GH_ParamAccess.item, false);
            pManager[IN_AutoColor].Optional = true;
            IN_plane = pManager.AddPlaneParameter("Base Plane", "Plane", "The base plane for the floor plan under analysis", GH_ParamAccess.item);
            pManager[IN_plane].Optional = true;
            IN_rects = pManager.AddRectangleParameter("Plan Voxels", "Voxels", "The rectangular voxels representing the analysis units of the floor plan", GH_ParamAccess.list);
            IN_perimCurve = pManager.AddCurveParameter("Perimeter Curve", "Perimeter", "The curve that describes the extents of the floor plan boundary", GH_ParamAccess.item);
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
        //protected override void SolveInstance(IGH_DataAccess DA)
        //{

        //    Curve perimeter = null;
        //    Plane plane = Plane.Unset;

        //    rectangles = new List<Rectangle3d>();

        //    DA.GetData(IN_plane, ref plane);
        //    DA.GetData(IN_AutoColor, ref autoColor);
        //    if (!DA.GetData(IN_perimCurve, ref perimeter)) return;
        //    if (!DA.GetDataList(IN_rects, rectangles)) return;

        //    try
        //    {
        //        _plan = new SmartPlan(perimeter, rectangles, plane);
        //        _plan.ComputeDistToPerimeter();
        //        perimeterDist = _plan.getPerimDistances();
        //        DA.SetDataList(OUT_perimeterMetric, perimeterDist);
        //    }
        //    catch (Exception e)
        //    {
        //        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.ToString());
        //    }

        //}

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


        //public override void DrawViewportMeshes(IGH_PreviewArgs args)
        //{

        //    if (autoColor)
        //    {
        //        gradientList = new System.Drawing.Color[_plan.getCells().Count];
        //        for (int i = 0; i < gradientList.Length; i++)
        //        {
        //            var multiplier = perimeterDist[i];

        //            var gColor = new ColorHSL(multiplier, multiplier, 0, multiplier);
        //            var rgb = gColor.ToArgbColor();
        //            gradientList[i] = (rgb);
        //        }
        //        for (int i = 0; i < rectangles.Count; i++)
        //        {
        //            Rhino.Display.DisplayMaterial mat = new Rhino.Display.DisplayMaterial(gradientList[i]);
        //            mat.Shine = 0.25;
        //            {
        //                var curve = rectangles[i].ToNurbsCurve();
        //                var pts = rectangles[i].ToPolyline();
        //                args.Display.DrawPolyline(pts, gradientList[i], 1);
        //                var mesh = Mesh.CreateFromPlanarBoundary(curve, Rhino.Geometry.MeshingParameters.FastRenderMesh, 0.01);
        //                args.Display.DrawMeshShaded(mesh, mat);
        //            }
        //        }

        //    }
        //}

        //public override void DrawViewportWires(IGH_PreviewArgs args)
        //{

        //    if (autoColor)
        //    {
        //        gradientList = new System.Drawing.Color[_plan.getCells().Count];
        //        for (int i = 0; i < gradientList.Length; i++)
        //        {
        //            var multiplier = perimeterDist[i];

        //            var gColor = new ColorHSL(multiplier, multiplier, 0, multiplier);
        //            var rgb = gColor.ToArgbColor();
        //            gradientList[i] = (rgb);
        //        }
        //        for (int i = 0; i < rectangles.Count; i++)
        //        {
        //            Rhino.Display.DisplayMaterial mat = new Rhino.Display.DisplayMaterial(gradientList[i]);
        //            mat.Shine = 0.25;
        //            {
        //                var curve = rectangles[i].ToNurbsCurve();
        //                var pts = rectangles[i].ToPolyline();
        //                args.Display.DrawPolyline(pts, gradientList[i], 1);
        //                var mesh = Mesh.CreateFromPlanarBoundary(curve, Rhino.Geometry.MeshingParameters.FastRenderMesh, 0.01);
        //                args.Display.DrawMeshShaded(mesh, mat);
        //            }
        //        }

        //    }
        //}

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("09554004-529e-460a-8111-4e139cd6877b"); }
        }
    }


    public class PerimeterCalculator : WorkerInstance
    {
        //int TheNthPrime { get; set; } = 100;
        //long ThePrime { get; set; } = -1;

        public bool autoColor;
        public SmartPlan _plan;
        public List<Rectangle3d> rectangles = new List<Rectangle3d>();
        public System.Drawing.Color[] gradientList;
        public double[] perimeterDist;

        Curve perimeter = null;
        Plane plane = Plane.Unset;

        public override void DoWork(Action<string, double> ReportProgress, Action<string, GH_RuntimeMessageLevel> ReportError, Action Done)
        {
            // 👉 Checking for cancellation!
            if (CancellationToken.IsCancellationRequested) return;

            rectangles = new List<Rectangle3d>();

            _plan = new SmartPlan(perimeter, rectangles, plane); //constructor

            var min = 1000.0;
            var max = 1.0;
            var cells = _plan.Cells;
            int count = 0;
            foreach (KeyValuePair<Vector2dInt, SmartCell> cell in cells)
            {
                // 👉 Checking for cancellation!
                if (CancellationToken.IsCancellationRequested) return;

                var testPoint = new Point3d(cell.Value.location.X, cell.Value.location.Y, 0);
                double t;
                Point3d otherPt = new Point3d();
                if (perimeter.ClosestPoint(testPoint, out t))
                {
                    // 👉 Checking for cancellation!
                    if (CancellationToken.IsCancellationRequested) return;

                    otherPt = perimeter.PointAt(t);
                    double dist = testPoint.DistanceTo(otherPt);
                    cell.Value.metric2 = dist;
                    if (dist < min)
                        min = dist;
                    if (dist > max)
                        max = dist;
                }
                count++;


                ReportProgress(Id, ((double)count) / cells.Count);
            }

            foreach (KeyValuePair<Vector2dInt, SmartCell> cell in cells)
            {
                var holder = PBUtilities.mapValue(cell.Value.metric2, min, max, 0.0, 1.0);
                var final = 1.0 - holder;
                cell.Value.metric2 = final;
            }

            perimeterDist = _plan.getExitMetric();

            Done();
        }

        public override WorkerInstance Duplicate() => new PerimeterCalculator();

        public override void GetData(IGH_DataAccess DA, GH_ComponentParamServer Params)
        {
            DA.GetData(0, ref autoColor);
            DA.GetData(1, ref plane);
           
            DA.GetData(3, ref perimeter);

         
        }

        public override void GetDataList(IGH_DataAccess DA, GH_ComponentParamServer Params)
        {
            DA.GetDataList(2, rectangles);
        }

        public override void SetData(IGH_DataAccess DA)
        {
            //// 👉 Checking for cancellation!
            //if (CancellationToken.IsCancellationRequested) return;

            //DA.SetDataList(0, perimeterDist);
        }

        public override void SetDataList(IGH_DataAccess DA)
        {
            // 👉 Checking for cancellation!
            if (CancellationToken.IsCancellationRequested) return;

            DA.SetDataList(0, perimeterDist);
        }


    }
}


