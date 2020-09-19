using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Threading.Tasks;
using Rhino.Display;

namespace PlanBee
{
    public class Ghc_DaylightAccess : GH_TaskCapableComponent<Ghc_DaylightAccess.SolveResults>
    {

        bool autoColor = false;
        SmartPlan _plan;
        List<Rectangle3d> rectangles = new List<Rectangle3d>();
        System.Drawing.Color[] gradientList;
        double[] sunAccess;

        /// <summary>
        /// Initializes a new instance of the GhcDaylightAccess class.
        /// </summary>
        public Ghc_DaylightAccess()
          : base("Daylight Access", "Daylight Access",
              "Computes a daylight access metric for each plan voxel for a specified period of time",
              "PlanBee", "Analysis")
        {
        }

        int IN_AutoColor;
        int IN_plane;
        int IN_vectors;
        int IN_partitionC;
        int IN_ftoCeil;
        int IN_Obstacles;
        int IN_rects;
        int IN_perimCurve;

        int OUT_SolarAccessMetric;
        int OUT_obs;


        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IN_AutoColor = pManager.AddBooleanParameter("Auto preview Daylight Access Metric Visualization", "Autocolor Daylight", "A built-in analysis coloring of the voxels of the plan for the daylight access metric. Make sure to have the component preview on in order to view.", GH_ParamAccess.item, false);
            pManager[IN_AutoColor].Optional = true;
            IN_plane = pManager.AddPlaneParameter("Base Plane", "Plane", "The base plane for the floor plan under analysis", GH_ParamAccess.item, Plane.WorldXY);
            pManager[IN_plane].Optional = true;
            IN_rects = pManager.AddRectangleParameter("Plan Voxels", "Voxels", "The rectangular voxels representing the analysis units of the floor plan", GH_ParamAccess.list);
            IN_perimCurve = pManager.AddCurveParameter("Perimeter Curve", "Perimeter", "The curve that describes the extents of the floor plan boundary", GH_ParamAccess.item);
            IN_vectors = pManager.AddVectorParameter("Sun Vectors", "Sun Vectors", "The sun vectors pertaining to spcified analysis period", GH_ParamAccess.list);
            IN_partitionC = pManager.AddCurveParameter("Partition curves", "Partitions", "Partition curves describing plan internal walls", GH_ParamAccess.list);
            IN_ftoCeil = pManager.AddNumberParameter("Floor to ceiling height", "Floor to ceiling", "Floor to ceiling height for internal partition sizing", GH_ParamAccess.item);
            IN_Obstacles = pManager.AddBrepParameter("Solar Obstacle Breps", "Obstacle Breps", "External geometry (including facade elements/ roof/ ceiling/ other building geometry) that may block access to daylight. Exclude internal partitions and ceiling as that is being taken into account", GH_ParamAccess.list);
            pManager[IN_Obstacles].Optional = true;
        }


        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            OUT_SolarAccessMetric = pManager.AddNumberParameter("Daylight Access Metric", "Normalized Daylight Access", "The daylight metric of each cell, remapped from 0 to 1 using the bounds of the plan as remapping domain", GH_ParamAccess.list);
            OUT_obs = pManager.AddMeshParameter("Obstacle mesh", "Obstacle mesh", "Obstacle mesh for user verification that correct obstacles are being used", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve perimeter = null;
            List<Brep> _obstacles = new List<Brep>();
            List<Brep> ACTUAL_obstacles = new List<Brep>();
            List<Vector3d> SunVectors = new List<Vector3d>();
            List<Curve> partitionCurves = new List<Curve>();
            Plane plane = Plane.Unset;
            double floorToCeiling = double.NaN;

            Brep extrBrep = new Brep();

            try
            {
                if (InPreSolve)
                {
                    rectangles = new List<Rectangle3d>();
                    _obstacles = new List<Brep>();
                    ACTUAL_obstacles = new List<Brep>();
                    SunVectors = new List<Vector3d>();
                    partitionCurves = new List<Curve>();

                    DA.GetData(IN_AutoColor, ref autoColor);
                    DA.GetData(IN_plane, ref plane);
                    DA.GetDataList(IN_vectors, SunVectors);
                    DA.GetData(IN_perimCurve, ref perimeter);
                    DA.GetDataList(IN_partitionC, partitionCurves);
                    DA.GetData(IN_ftoCeil, ref floorToCeiling);
                    bool obstaclesPresent = DA.GetDataList(IN_Obstacles, _obstacles);
                    if (obstaclesPresent)
                        ACTUAL_obstacles.AddRange(_obstacles);
                    DA.GetDataList(IN_rects, rectangles);

                    Transform project = Transform.PlanarProjection(plane);
                    for (int i = 0; i < partitionCurves.Count; i++)
                        partitionCurves[i].Transform(project);

                    perimeter.Transform(project);

                    Vector3d extrusionVec = new Vector3d(0, 0, 1) * floorToCeiling;
                    for (int i = 0; i < partitionCurves.Count; i++)
                    {
                        var extr = Surface.CreateExtrusion(partitionCurves[i], extrusionVec);
                        var brep = extr.ToBrep();
                        ACTUAL_obstacles.Add(brep);
                    }
                    perimeter.Translate(extrusionVec);

                    extrBrep = Rhino.Geometry.Brep.CreatePlanarBreps(perimeter, 0.0001)[0];
                    ACTUAL_obstacles.Add(extrBrep);


                    _plan = new SmartPlan(rectangles, SunVectors, ACTUAL_obstacles, plane);

                    Task<SolveResults> task = Task.Run(() => ComputeSolar(_plan), CancelToken);
                    TaskList.Add(task);
                    return;
                }
                if (!GetSolveResults(DA, out SolveResults result))
                {
                    rectangles = new List<Rectangle3d>();
                    ACTUAL_obstacles = new List<Brep>();
                    _obstacles = new List<Brep>();
                    SunVectors = new List<Vector3d>();
                    partitionCurves = new List<Curve>();

                    DA.GetData(IN_AutoColor, ref autoColor);
                    DA.GetData(IN_plane, ref plane);
                    DA.GetDataList(IN_vectors, SunVectors);
                    DA.GetData(IN_perimCurve, ref perimeter);
                    DA.GetDataList(IN_partitionC, partitionCurves);
                    DA.GetData(IN_ftoCeil, ref floorToCeiling);
                    bool obstaclesPresent = DA.GetDataList(IN_Obstacles, _obstacles);
                    if (obstaclesPresent)
                        ACTUAL_obstacles.AddRange(_obstacles);
                    DA.GetDataList(IN_rects, rectangles);


                    Transform project = Transform.PlanarProjection(plane);
                    for (int i = 0; i < partitionCurves.Count; i++)
                        partitionCurves[i].Transform(project);

                    perimeter.Transform(project);

                    Vector3d extrusionVec = new Vector3d(0, 0, 1) * floorToCeiling;
                    for (int i = 0; i < partitionCurves.Count; i++)
                    {
                        var extr = Surface.CreateExtrusion(partitionCurves[i], extrusionVec);
                        var brep = extr.ToBrep();
                        ACTUAL_obstacles.Add(brep);
                    }
                    perimeter.Translate(extrusionVec);

                    extrBrep = Rhino.Geometry.Brep.CreatePlanarBreps(perimeter, 0.0001)[0];
                    ACTUAL_obstacles.Add(extrBrep);

                    _plan = new SmartPlan(rectangles, SunVectors, ACTUAL_obstacles, plane);
                    result = ComputeSolar(_plan);
                    _plan = result.Value;
                }

                if (result != null)
                {
                    sunAccess = _plan.getSolarMetric();
                    DA.SetDataList(OUT_SolarAccessMetric, sunAccess);
                    DA.SetData(OUT_obs, _plan.obstacleMeshJoined);
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

        public static SolveResults ComputeSolar(SmartPlan plan)
        {
            SolveResults result = new SolveResults();
            plan.ComputeSolarAccess();
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
                return PlanBee.Properties.Resources.Daylight_Hours_01;
            }
        }

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {

            if (autoColor)
            {
                gradientList = new System.Drawing.Color[_plan.getCells().Count];
                for (int i = 0; i < gradientList.Length; i++)
                {
                    var multiplier = sunAccess[i];

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
                    var multiplier = sunAccess[i];

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
            get { return new Guid("48719de2-ede7-4bd7-a33c-ca87e3d7368a"); }
        }
    }
}