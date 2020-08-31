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


namespace Planbee
{
    public class GhcIsovist : GH_TaskCapableComponent<GhcIsovist.SolveResults>
    {
        bool autoColor = false;
        //bool reset_Active = true;
        SmartPlan _plan;
        List<Rectangle3d> rectangles = new List<Rectangle3d>();
        System.Drawing.Color[] gradientList;
        double[] iso;

        GH_Document doc;

        /// <summary>
        /// Initializes a new instance of the GhcIsovist class.
        /// </summary>
        public GhcIsovist()
          : base("Isovist", "Isovist",
              "Computes an isovist metric for each plan voxel",
              "PlanBee", "Analysis")
        {
        }

        //int IN_reset;
        int IN_AutoColor;
        int IN_plane;
        int IN_perimCurve;
        int IN_coreCurves;
        int IN_rects;
        int IN_partitions;
        int OUT_isovistMetric;
        int OUT_isovistPolys;

        public override GH_Exposure Exposure => GH_Exposure.primary;

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
            pManager[IN_coreCurves].Optional = true;
            IN_partitions = pManager.AddCurveParameter("Partition Curves", "Partitions", "Polylines describing partitions", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            OUT_isovistMetric = pManager.AddNumberParameter("Normalized Isovist metric per voxel", "Normalized isovist", "The isovist metric of each cell, remapped from 0 to 1 using the bounds of the plan as remapping domain", GH_ParamAccess.list);
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
                    bool coreReceived = DA.GetDataList(IN_coreCurves, coreCrvs);
                    DA.GetDataList(IN_rects, rectangles);
                    DA.GetDataList(IN_partitions, interiorPartitions);

                    if (coreReceived)
                        _plan = new SmartPlan(perimeter, coreCrvs, rectangles, interiorPartitions, plane);
                    else
                        _plan = new SmartPlan(perimeter, rectangles, interiorPartitions, plane);


                    Task<SolveResults> task = Task.Run(() => ComputeIso(_plan), CancelToken);
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
                    bool coreReceived = DA.GetData(IN_coreCurves, ref coreCrvs);
                    DA.GetDataList(IN_rects, rectangles);
                    DA.GetDataList(IN_partitions, interiorPartitions);

                    if (coreReceived)
                        _plan = new SmartPlan(perimeter, coreCrvs, rectangles, interiorPartitions, plane);
                    else
                        _plan = new SmartPlan(perimeter, rectangles, interiorPartitions, plane);

                    result = ComputeIso(_plan);
                    _plan = result.Value;

                }

                if (result != null)
                {
                    iso = _plan.getIsovist();
                    DA.SetDataList(OUT_isovistMetric, iso);
                    DA.SetDataList(OUT_isovistPolys, _plan.isoPolylines);
                }
            }
            catch (Exception e)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.ToString());
            }

           // Params.ParameterChanged -= ObjectEventHandler;
           // Params.ParameterChanged += ObjectEventHandler;

        }

        public class SolveResults
        {
            public SmartPlan Value { get; set; }
        }

        public static SolveResults ComputeIso(SmartPlan plan)
        {
            SolveResults result = new SolveResults();
            plan.ComputeIsovist();
            result.Value = plan;
            return result;
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return PlanBee.Properties.Resources.Isovist_01;
            }
        }

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {

            if (autoColor)
            {
                gradientList = new System.Drawing.Color[_plan.getCells().Count];
                for (int i = 0; i < gradientList.Length; i++)
                {
                    var multiplier = iso[i];

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
                    var multiplier = iso[i];

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
            get { return new Guid("a3eaedb0-7217-4034-b936-d39427de8772"); }
        }

        void ObjectEventHandler(object sender, EventArgs e)
        {
                //if (e.Objects.Where(o => o is IGH_ActiveObject).Count() > 0)
                {
                   // reset_Active = true;
                    // lastChecked = DateTime.Now;
                    this.ExpireSolution(true);
                }

        }
    }
}