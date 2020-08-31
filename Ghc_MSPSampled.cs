using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Rhino.Display;
using Rhino.Geometry;
using Planbee;

namespace PlanBee
{
    public class Ghc_MSPSampled : GH_TaskCapableComponent<Ghc_MSPSampled.SolveResults>
    {


        bool autoColor = false;
        SmartPlan _plan;
        List<Rectangle3d> rectangles = new List<Rectangle3d>();
        System.Drawing.Color[] gradientList;
        double[] _mspMetric;
        double[] _rawMspMetric;

        /// <summary>
        /// Initializes a new instance of the Ghc_MSPQuickGraph class.
        /// </summary>
        public Ghc_MSPSampled()
          : base("Sampled Mean Shortest Path", "MSP Sampled",
              "Description",
              "PlanBee", "Analysis")
        {
        }

        int IN_AutoColor;
        int IN_plane;
        int IN_rects;
        int IN_partitions;

        int OUT_paths;
        int OUT_mspMetric;
        int OUT_mspRawMetric;

        public override GH_Exposure Exposure => GH_Exposure.secondary;


        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IN_AutoColor = pManager.AddBooleanParameter("Auto preview exit paths", "Autocolor paths", "Path to exit points for each voxel of the flor plan. Make sure to have the component preview on in order to view.", GH_ParamAccess.item, false);
            IN_plane = pManager.AddPlaneParameter("Base Plane", "Plane", "The base plane for the floor plan under analysis", GH_ParamAccess.item, Plane.WorldXY);
            pManager[IN_plane].Optional = true;
            IN_rects = pManager.AddRectangleParameter("Plan Voxels", "Voxels", "The rectangular voxels representing the analysis units of the floor plan", GH_ParamAccess.list);
            IN_partitions = pManager.AddCurveParameter("Partition Curves", "Partitions", "Polylines describing partitions", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            OUT_mspMetric = pManager.AddNumberParameter("Normalized MSP metric per voxel", "Normalized MSP", "The MSP of each cell, remapped from 0 to 1 using the bounds of the plan as remapping domain", GH_ParamAccess.list);
            OUT_mspRawMetric = pManager.AddNumberParameter("Raw Mean Shortest Path metric per voxel", "Raw MSP", "The raw Mean Shortest Path of each cell", GH_ParamAccess.list);
            OUT_paths = pManager.AddCurveParameter("Path curves", "Paths", "MSP paths from each plan voxel", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> interiorPartitions;
            Plane plane = Plane.Unset;

            try
            {
                if (InPreSolve)
                {
                    rectangles = new List<Rectangle3d>();
                    interiorPartitions = new List<Curve>();

                    if (!DA.GetData(IN_AutoColor, ref autoColor)) return;
                    if (!DA.GetData(IN_plane, ref plane)) return;
                    if (!DA.GetDataList(IN_rects, rectangles)) return;
                    DA.GetDataList(IN_partitions, interiorPartitions);


                    _plan = new SmartPlan(rectangles, interiorPartitions, plane);


                    Task<SolveResults> task = Task.Run(() => ComputeMSP(_plan), CancelToken);
                    TaskList.Add(task);
                    return;
                }

                if (!GetSolveResults(DA, out SolveResults result))
                {
                    rectangles = new List<Rectangle3d>();
                    interiorPartitions = new List<Curve>();

                    DA.GetData(IN_AutoColor, ref autoColor);
                    DA.GetData(IN_plane, ref plane);
                    DA.GetDataList(IN_rects, rectangles);
                    DA.GetDataList(IN_partitions, interiorPartitions);

                    _plan = new SmartPlan(rectangles, interiorPartitions, plane);
                    result = ComputeMSP(_plan);
                    _plan = result.Value;
                }


                if (result != null)
                {
                    _mspMetric = _plan.getMSP();
                    _rawMspMetric = _plan.getMSPRaw();
                    DA.SetDataList(OUT_mspMetric, _mspMetric);
                    DA.SetDataList(OUT_mspRawMetric, _rawMspMetric);
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

        public static SolveResults ComputeMSP(SmartPlan plan)
        {
            SolveResults result = new SolveResults();
            plan.ComputeSampleMeanShortestPath();
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
                return Properties.Resources.MeanShortestPath_Sampled_01;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("8f10c77e-2226-4f8a-8f86-dd2e4300c42e"); }
        }

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (autoColor)
            {
                gradientList = new System.Drawing.Color[_plan.getCells().Count];
                for (int i = 0; i < gradientList.Length; i++)
                {
                    var multiplier = _mspMetric[i];

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
                    var multiplier = _mspMetric[i];

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
    }
}