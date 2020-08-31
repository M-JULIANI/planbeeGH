using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Display;
using Rhino.DocObjects;

using System.IO;
using System.Linq;
using System.Data;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using Rhino.Collections;
using GH_IO;
using GH_IO.Serialization;
using System.Threading.Tasks;
using Planbee;

namespace PlanBee
{
    public class GhcNeighborhoodSize : GH_TaskCapableComponent<GhcNeighborhoodSize.SolveResults>
    {

        bool autoColor = false;
        SmartPlan _plan;
        List<Rectangle3d> rectangles = new List<Rectangle3d>();
        System.Drawing.Color[] gradientList;
        int[] rawNeighSize;
        double[] neighSize;

        /// <summary>
        /// Initializes a new instance of the GhcNeighborhoodSize class.
        /// </summary>
        public GhcNeighborhoodSize()
          : base("Neighborhood Size", "Neighborhood Size",
              "Neighborhood size equivalent to the area of the isovist polygon",
              "PlanBee", "Analysis")
        {
        }

        int IN_AutoColor;
        int IN_plane;
        int IN_perimCurve;
        int IN_coreCurves;
        int IN_rects;
        int IN_partitions;

        int OUT_neighSizeMetric;
        int OUT_rawNeighSizeMetric;
        int OUT_isovistPolys;

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IN_AutoColor = pManager.AddBooleanParameter("Auto preview Isovist Metric Visualization", "Autocolor Isovist", "A built-in analysis coloring of the voxels of the plan for the isovist metric. Make sure to have the component preview on in order to view.", GH_ParamAccess.item, false);
            IN_plane = pManager.AddPlaneParameter("Base Plane", "Plane", "The base plane for the floor plan under analysis", GH_ParamAccess.item, Plane.WorldXY);
            pManager[IN_plane].Optional = true;
            IN_rects = pManager.AddRectangleParameter("Plan Voxels", "Voxels", "The rectangular voxels representing the analysis units of the floor plan", GH_ParamAccess.list);
            IN_perimCurve = pManager.AddCurveParameter("Perimeter Curve", "Perimeter", "The curve that describes the extents of the floor plan boundary", GH_ParamAccess.item);
            IN_coreCurves = pManager.AddCurveParameter("Core Curves", "Cores", "The curves that describe the extent of the core boundaries", GH_ParamAccess.list);
            IN_partitions = pManager.AddCurveParameter("Partition Curves", "Partitions", "Polylines describing partitions", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            OUT_neighSizeMetric = pManager.AddNumberParameter("Normalized Neighborhood Size metric per voxel", "Normalized Neighborhood Size", "The neighborhood size metric of each cell, remapped from 0 to 1 using the bounds of the plan as remapping domain", GH_ParamAccess.list);
            OUT_rawNeighSizeMetric = pManager.AddNumberParameter("Raw Neighborhood Size metric per voxel", "Raw Neighborhood Size", "The raw neighborhood size metric of each cell", GH_ParamAccess.list);
            OUT_isovistPolys = pManager.AddCurveParameter("Isovist Polygons", "Iso Polygons", "Isovist polyline polygons describing range of vision from each plan voxel", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve perimeter = null;
            List<Curve> coreCrvs = new List<Curve>();
            List<Curve> interiorPartitions;
            Plane plane = Plane.Unset;

            try
            {
                if (InPreSolve)
                {
                    rectangles = new List<Rectangle3d>();
                    interiorPartitions = new List<Curve>();

                    // DA.GetData(IN_reset, ref iReset);
                    DA.GetData(IN_AutoColor, ref autoColor);
                    DA.GetData(IN_plane, ref plane);
                    DA.GetData(IN_perimCurve, ref perimeter);
                    DA.GetDataList(IN_coreCurves, coreCrvs);
                    DA.GetDataList(IN_rects, rectangles);
                    DA.GetDataList(IN_partitions, interiorPartitions);

                    _plan = new SmartPlan(perimeter, coreCrvs, rectangles, interiorPartitions, plane);


                    Task<SolveResults> task = Task.Run(() => ComputeNeighborhoodSize(_plan), CancelToken);
                    TaskList.Add(task);
                    return;
                }

                if (!GetSolveResults(DA, out SolveResults result))
                {
                    rectangles = new List<Rectangle3d>();
                    interiorPartitions = new List<Curve>();

                    DA.GetData(IN_AutoColor, ref autoColor);
                    DA.GetData(IN_plane, ref plane);
                    DA.GetData(IN_perimCurve, ref perimeter);
                    DA.GetData(IN_coreCurves, ref coreCrvs);
                    DA.GetDataList(IN_rects, rectangles);
                    DA.GetDataList(IN_partitions, interiorPartitions);

                    _plan = new SmartPlan(perimeter, coreCrvs, rectangles, interiorPartitions, plane);
                    result = ComputeNeighborhoodSize(_plan);
                    _plan = result.Value;

                }

                if (result != null)
                {
                    rawNeighSize = _plan.getNeighborhoodSizeRaw();
                    neighSize = _plan.getNeighborhoodSize();
                    DA.SetDataList(OUT_neighSizeMetric, neighSize);
                    DA.SetDataList(OUT_rawNeighSizeMetric, rawNeighSize);
                    DA.SetDataList(OUT_isovistPolys, _plan.isoNeighPolylines);
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

        public static SolveResults ComputeNeighborhoodSize(SmartPlan plan)
        {
            SolveResults result = new SolveResults();
            plan.ComputeNeighSize();
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
                return Properties.Resources.NeighborhoodSize_01;
            }
        }


        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {

            if (autoColor)
            {
                gradientList = new System.Drawing.Color[_plan.getCells().Count];
                for (int i = 0; i < gradientList.Length; i++)
                {
                    var multiplier = neighSize[i];

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
                    var multiplier = neighSize[i];

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
            get { return new Guid("2956704c-0f2b-4e1f-b3f6-c47e98d64eac"); }
        }
    }
}