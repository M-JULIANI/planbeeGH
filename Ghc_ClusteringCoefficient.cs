using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Threading.Tasks;
using Rhino.Display;

namespace PlanBee
{
    public class Ghc_ClusteringCoefficient : GH_TaskCapableComponent<Ghc_ClusteringCoefficient.SolveResults>
    {

        bool autoColor = false;
        SmartPlan _plan;
        List<Rectangle3d> rectangles = new List<Rectangle3d>();
        System.Drawing.Color[] gradientList;
        double[] clusterCoeff;
        double[] clusterCoeffRaw;

        /// <summary>
        /// Initializes a new instance of the Ghc_ClusteringCoefficient class.
        /// </summary>
        public Ghc_ClusteringCoefficient()
          : base("Clustering Coefficient", "Clustering Coefficient",
              "The clustering coefficient of of all cells. 'The clustering coefficient gives a measure of the proportion of intervisible space within the visibility neighbourhood of a point.'" +
                "Refer to Alasdair Turner's paper:'From isovists to visibility graphs: a methodology for the analysis of architectural space' for a full definition" +
                "of clustering coefficient.",
              "PlanBee", "Analysis")
        {
        }

        int IN_AutoColor;
        int IN_plane;
        int IN_perimCurve;
        int IN_coreCurves;
        int IN_rects;
        int IN_partitions;

        int OUT_clusteringCoefficientRemap;
        int OUT_clusteringCoefficientRaw;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IN_AutoColor = pManager.AddBooleanParameter("Auto preview Isovist Metric Visualization", "Autocolor Isovist", "A built-in analysis coloring of the voxels of the plan for the isovist metric. Make sure to have the component preview on in order to view.", GH_ParamAccess.item, false);
            pManager[IN_AutoColor].Optional = true;
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
            OUT_clusteringCoefficientRemap = pManager.AddNumberParameter("Normalized Clustering coefficient metric per voxel", "Clustering Coefficient", "The clustering coefficient of each cell normalized.", GH_ParamAccess.list);
            OUT_clusteringCoefficientRaw = pManager.AddNumberParameter("Clustering coefficient metric per voxel", "Clustering Coefficient Raw", "The clustering coefficient of each cell", GH_ParamAccess.list);
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

                    DA.GetData(IN_AutoColor, ref autoColor);
                    DA.GetData(IN_plane, ref plane);
                    DA.GetData(IN_perimCurve, ref perimeter);
                    bool coreReceived =  DA.GetDataList(IN_coreCurves, coreCrvs);
                    DA.GetDataList(IN_rects, rectangles);
                    DA.GetDataList(IN_partitions, interiorPartitions);

                    if (coreReceived)
                        _plan = new SmartPlan(perimeter, coreCrvs, rectangles, interiorPartitions, plane);
                    else
                        _plan = new SmartPlan(perimeter, rectangles, interiorPartitions, plane);
                    Rhino.RhinoApp.WriteLine(_plan.ToString());

                    Task<SolveResults> task = Task.Run(() => ComputeIsovistClustering(_plan), CancelToken);
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

                    Rhino.RhinoApp.WriteLine(_plan.ToString());

                    result = ComputeIsovistClustering(_plan);
                    _plan = result.Value;

                }

                if (result != null)
                {
                    clusterCoeff = _plan.getClusterCoeff();
                    clusterCoeffRaw = _plan.getClusterCoeffRaw();
                    DA.SetDataList(OUT_clusteringCoefficientRaw, clusterCoeffRaw);
                    DA.SetDataList(OUT_clusteringCoefficientRemap, clusterCoeff);
                    
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

        public static SolveResults ComputeIsovistClustering(SmartPlan plan)
        {
            SolveResults result = new SolveResults();
            plan.ComputeIsoCluteringCoeff();
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
                return null;
            }
        }

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {

            if (autoColor)
            {
                gradientList = new System.Drawing.Color[_plan.getCells().Count];
                for (int i = 0; i < gradientList.Length; i++)
                {
                    var multiplier = clusterCoeff[i];

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
                    var multiplier = clusterCoeff[i];

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
            get { return new Guid("d50365c5-ce44-473e-b6b6-c5ec4565cad2"); }
        }
    }
}