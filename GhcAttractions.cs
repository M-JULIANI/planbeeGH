using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Display;
using System.Threading.Tasks;

namespace PlanBee
{
   

    public class GhcAttractions : GH_TaskCapableComponent<GhcAttractions.SolveResults>
    {
        bool autoColor = false;
        SmartPlan _plan;
        List<Rectangle3d> rectangles = new List<Rectangle3d>();
        System.Drawing.Color[] gradientList;
        double[] _attractor;

        /// <summary>
        /// Initializes a new instance of the GhcAttractions class.
        /// </summary>
        public GhcAttractions()
          : base("Attraction Visibility", "Attractions",
              "Computes the visibility to landmarks/attractions for each plan voxel",
              "PlanBee", "Analysis")
        {
        }

        int IN_AutoColor;
        int IN_plane;
        int IN_perimCurve;
        int IN_coreCurves;
        int IN_rects;
        int IN_partitions;
        int IN_attCrvs;
        int IN_obstCrvs;

        int OUT_attractorMetric;
        int OUT_attractorViewLines;

        public override GH_Exposure Exposure => GH_Exposure.primary;



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IN_AutoColor = pManager.AddBooleanParameter("Auto preview Attraction Metric Visualization", "Autocolor Attraction", "A build it analysis coloring of the voxels of the plan for the attraction metric. Make sure to have the component preview on in order to view.", GH_ParamAccess.item, false);
            pManager[IN_AutoColor].Optional = true;
            IN_plane = pManager.AddPlaneParameter("Base Plane", "Plane", "The base plane for the floor plan under analysis", GH_ParamAccess.item, Plane.WorldXY);
            pManager[IN_plane].Optional = true;
            IN_rects = pManager.AddRectangleParameter("Plan Voxels", "Voxels", "The rectangular voxels representing the analysis units of the floor plan", GH_ParamAccess.list);
            IN_perimCurve = pManager.AddCurveParameter("Perimeter Curve", "Perimeter", "The curve that describes the extents of the floor plan boundary", GH_ParamAccess.item);
            IN_coreCurves = pManager.AddCurveParameter("Core Curves", "Cores", "The curves that describes the extent of the core boundaries", GH_ParamAccess.list);
            pManager[IN_coreCurves].Optional = true;
            IN_partitions = pManager.AddCurveParameter("Partition Curves", "Partitions", "Polylines describing partitions", GH_ParamAccess.list);
            IN_attCrvs = pManager.AddCurveParameter("Attractor Curves", "Attractors", "Attractor curves describing the boundaries of building profiles that are of interest - attractors", GH_ParamAccess.list);
            IN_obstCrvs = pManager.AddCurveParameter("Obstacle Curves", "Obstacles", "Obstacles blocking the view of attractor buildings/profiles", GH_ParamAccess.list);
            pManager[IN_obstCrvs].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            OUT_attractorMetric = pManager.AddNumberParameter("Normalized attraction metric per voxel", "Normalized attraction", "The attraction metric of each cell, remapped from 0 to 1 using the bounds of the plan as remapping domain", GH_ParamAccess.list);
            OUT_attractorViewLines = pManager.AddCurveParameter("Attraction lines", "Attraction lines", "Attraction view lines from each plan voxel", GH_ParamAccess.tree);
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
            List<Curve> attractors;
            List<Curve> obstacles;
            Plane plane = Plane.Unset;

            try
            {
                if (InPreSolve)
                {
                    rectangles = new List<Rectangle3d>();
                    interiorPartitions = new List<Curve>();
                    attractors = new List<Curve>();
                    obstacles = new List<Curve>();

                    DA.GetData(IN_AutoColor, ref autoColor);
                    DA.GetData(IN_plane, ref plane);
                    DA.GetData(IN_perimCurve, ref perimeter);
                    DA.GetDataList(IN_coreCurves, coreCrvs);
                    DA.GetDataList(IN_rects, rectangles);
                    DA.GetDataList(IN_partitions, interiorPartitions);

                    DA.GetDataList(IN_attCrvs, attractors);
                    DA.GetDataList(IN_obstCrvs, obstacles);
               
                    _plan = new SmartPlan(perimeter, coreCrvs, rectangles, interiorPartitions, attractors, obstacles, plane);

                    Task<SolveResults> task = Task.Run(() => ComputeAttractions(_plan), CancelToken);
                    TaskList.Add(task);
                    return;
                }

                if (!GetSolveResults(DA, out SolveResults result))
                {
                    rectangles = new List<Rectangle3d>();
                    interiorPartitions = new List<Curve>();
                    attractors = new List<Curve>();
                    obstacles = new List<Curve>();

                    DA.GetData(IN_AutoColor, ref autoColor);
                    DA.GetData(IN_plane, ref plane);
                    DA.GetData(IN_perimCurve, ref perimeter);
                    DA.GetData(IN_coreCurves, ref coreCrvs);
                    DA.GetDataList(IN_rects, rectangles);
                    DA.GetDataList(IN_partitions, interiorPartitions);

                    DA.GetDataList(IN_attCrvs, attractors);
                    DA.GetDataList(IN_obstCrvs, obstacles);

                    _plan = new SmartPlan(perimeter, coreCrvs, rectangles, interiorPartitions, attractors, obstacles, plane);
                    result = ComputeAttractions(_plan);
                    _plan = result.Value;
                }

                if (result != null)
                {
                    _attractor = _plan.getAttractionMetric();
                    DA.SetDataList(OUT_attractorMetric, _attractor);
                    DA.SetDataTree(OUT_attractorViewLines, _plan.testLines);
                }
            }
            catch(Exception e)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.ToString());
            }
        }

        public class SolveResults
        {
            public SmartPlan Value { get; set; }
        }

        public static SolveResults ComputeAttractions(SmartPlan plan)
        {
            SolveResults result = new SolveResults();
            plan.ComputeAttractionViz();
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
                return PlanBee.Properties.Resources.Attractions_01;
            }
        }

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (autoColor)
            {
                gradientList = new System.Drawing.Color[_plan.getCells().Count];
                for (int i = 0; i < gradientList.Length; i++)
                {
                    var multiplier = _attractor[i];

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
                    var multiplier = _attractor[i];

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
            get { return new Guid("a6ca171a-2f9e-4fcf-9350-a261eb450438"); }
        }
    }
}