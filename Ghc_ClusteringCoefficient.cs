using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

using System.Threading.Tasks;
using Planbee;

namespace PlanBee
{
    public class Ghc_ClusteringCoefficient : GH_TaskCapableComponent<Ghc_ClusteringCoefficient.SolveResults>
    {

        bool autoColor = false;
        SmartPlan _plan;
        List<Rectangle3d> rectangles = new List<Rectangle3d>();
        System.Drawing.Color[] gradientList;
        double[] clusterCoeff;

        /// <summary>
        /// Initializes a new instance of the Ghc_ClusteringCoefficient class.
        /// </summary>
        public Ghc_ClusteringCoefficient()
          : base("Clustering Coefficient", "Clustering Coefficient",
              "The clustering coefficient of of all cells. 'The clustering coefficient gives a measure of the proportion of intervisible space within the visibility neighbourhood of a point.'" +
                "For more information refer to Alasdair Turners paper: 'From isovists to visibility graphs: a methodology for the analysis of architectural space'.",
              "PlanBee", "Analysis")
        {
        }

        int IN_AutoColor;
        int IN_plane;
        int IN_perimCurve;
        int IN_coreCurves;
        int IN_rects;
        int IN_partitions;

        int OUT_clusteringCoefficient;

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
            OUT_clusteringCoefficient = pManager.AddNumberParameter("Clustering coefficient metric per voxel", "Clustering Coefficient", "The clustering coefficient of each cell", GH_ParamAccess.list);
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
                    var neighborhoodSize = _plan.getNeighborhoodSizeRaw();
                    clusterCoeff = new double [neighborhoodSize.Length];
                    double[] remappedAreas = new double[neighborhoodSize.Length];

                    double min = 100000.0;
                    double max = -1.0;

                    for (int i = 0; i < neighborhoodSize.Length; i++)
                    {
                        if (neighborhoodSize[i] < min)
                            min = neighborhoodSize[i];
                        if (neighborhoodSize[i] > max)
                            max = neighborhoodSize[i];
                    }

                    for (int i = 0; i < remappedAreas.Length; i++)
                    {
                        //remapped neighborhood size values for quick factorial calc
                        remappedAreas[i] = PBUtilities.mapValue(neighborhoodSize[i], min, max, 0.0, 20.0);
                    }

                    for (int i = 0; i < clusterCoeff.Length; i++)
                        clusterCoeff[i] = remappedAreas[i] / PBUtilities.FactorialFor((int)Math.Round(remappedAreas[i])); // PBUtilities.FactorialR((int)Math.Round(remappedAreas[i]));

                    DA.SetDataList(OUT_clusteringCoefficient, clusterCoeff);
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
                return null;
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