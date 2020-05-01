﻿using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Display;
using System.Threading.Tasks;

namespace Planbee
{
    public class GhcExitAccess : GH_TaskCapableComponent<GhcExitAccess.SolveResults>
    {

        bool autoColor = false;
        SmartPlan _plan;
        List<Rectangle3d> rectangles = new List<Rectangle3d>();
        System.Drawing.Color[] gradientList;
        double[] _exitMetric;

        /// <summary>
        /// Initializes a new instance of the GhcExitAccess class.
        /// </summary>
        public GhcExitAccess()
          : base("Exit Paths", "Paths",
              "Computes an exit access metric for each plan voxel. The suggested use case is to compute access to exits but could be to other attraction locations on the floor plan",
              "PlanBee", "Analysis")
        {
        }

        int IN_reset;
        int IN_AutoColor;
        int IN_plane;
        int IN_rects;
        int IN_partitions;
        int IN_exitPts;

        int OUT_paths;
        int OUT_pathMetric;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IN_reset = pManager.AddBooleanParameter("Reset", "Reset", "Reset all vals", GH_ParamAccess.item, false);
            IN_AutoColor = pManager.AddBooleanParameter("Auto preview exit paths", "Autocolor paths", "Path to exit points for each voxel of the flor plan. Make sure to have the component preview on in order to view.", GH_ParamAccess.item, false);
            IN_plane = pManager.AddPlaneParameter("Base Plane", "Plane", "The base plane for the floor plan under analysis", GH_ParamAccess.item, Plane.WorldXY);
            pManager[2].Optional = true;
            IN_rects = pManager.AddRectangleParameter("Plan Voxels", "Voxels", "The rectangular voxels representing the analysis units of the floor plan", GH_ParamAccess.list);
            IN_partitions = pManager.AddCurveParameter("Partition Curves", "Partitions", "Polylines describing partitions", GH_ParamAccess.list);
            IN_exitPts = pManager.AddPointParameter("Exit Points", "Exits", "Points used as exit locations from floor plan", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            OUT_pathMetric = pManager.AddNumberParameter("Normalized exit path metric per voxel", "Normalized exit paths", "The exit path metric of each cell, remapped from 0 to 1 using the bounds of the plan as remapping domain", GH_ParamAccess.list);
            OUT_paths = pManager.AddCurveParameter("Path curves", "Paths", "Paths to exits from each plan voxel", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            
            List<Curve> interiorPartitions = new List<Curve>();
            List<Point3d> exitPts = new List<Point3d>();
            Plane plane = Plane.Unset;
            bool iReset = false;

            try
            {
                if (InPreSolve)
                {
                    rectangles = new List<Rectangle3d>();
                    interiorPartitions = new List<Curve>();


                    DA.GetData(IN_reset, ref iReset);
                    if (!DA.GetData(IN_AutoColor, ref autoColor)) return;
                    if (!DA.GetData(IN_plane, ref plane)) return;
                    if (!DA.GetDataList(IN_rects, rectangles)) return;
                    if (!DA.GetDataList(IN_partitions, interiorPartitions)) return;
                    if (!DA.GetDataList(IN_exitPts, exitPts)) return;

                    if(iReset)
                        _plan = new SmartPlan(rectangles, interiorPartitions, exitPts, plane);
                    else if (_plan ==null)
                        _plan = new SmartPlan(rectangles, interiorPartitions, exitPts, plane);

                    Task<SolveResults> task = Task.Run(() => ComputeExit(_plan), CancelToken);
                    TaskList.Add(task);
                    return;
                }

                if (!GetSolveResults(DA, out SolveResults result))
                {
                    rectangles = new List<Rectangle3d>();
                    interiorPartitions = new List<Curve>();

                    DA.GetData(IN_reset, ref iReset);
                    DA.GetData(IN_AutoColor, ref autoColor);
                    DA.GetData(IN_plane, ref plane);
                    DA.GetDataList(IN_rects, rectangles);
                    DA.GetDataList(IN_partitions, interiorPartitions);
                    DA.GetDataList(IN_exitPts, exitPts);

                    _plan = new SmartPlan(rectangles, interiorPartitions, exitPts, plane);
                    result = ComputeExit(_plan);
                    _plan = result.Value;
                }


                if (result != null)
                {
                    _exitMetric = _plan.getExitMetric();
                    DA.SetDataList(OUT_pathMetric, _exitMetric);
                    DA.SetDataTree(OUT_paths, _plan.pathCurves);
                }
            }
            catch (Exception e)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.ToString());
            }

        }

        public class SolveResults
        {
            public SmartPlan Value { get; set; }
        }

        public static SolveResults ComputeExit(SmartPlan plan)
        {
            SolveResults result = new SolveResults();
            plan.ComputeExitAccess();
            result.Value = plan;
            return result;
        }


        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        /// 

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return PlanBee.Properties.Resources.Paths;
            }
        }

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (autoColor)
            {
                gradientList = new System.Drawing.Color[_plan.getCells().Count];
                for (int i = 0; i < gradientList.Length; i++)
                {
                    var multiplier = _exitMetric[i];

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
                    var multiplier = _exitMetric[i];

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
            get { return new Guid("e46fdb52-163d-43b7-ba36-6f88a3431aca"); }
        }
    }
}