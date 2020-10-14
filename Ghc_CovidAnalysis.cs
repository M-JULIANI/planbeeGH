using System;
using System.Drawing;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Display;

namespace PlanBee
{
    public class Ghc_CovidAnalysis : GH_Component
    {

        bool autoColor = false;
        SmartPlan _plan;
        List<Rectangle3d> rectangles;
        List<Curve> obstacleCrvs;
        List<Curve> coreCrvs;
        System.Drawing.Color[] gradientList;
        int[] compromisedMetric;

        /// <summary>
        /// Initializes a new instance of the GhcCovidAnalysis class.
        /// </summary>
        public Ghc_CovidAnalysis()
          : base("Covid Analysis", "Covid",
              "Compute whether a given cell in the analysis grid is compromised for Covid. This is a proxy metric that is measured by shooting rays 2m in length in various directions." +
                "If two directions which are 45 degrees apart collide against obstacle objects, the cell is considered 'compromised'.",
              "PlanBee", "Analysis")
        {
        }

        int IN_autoColor;
        int IN_plane;
        int IN_perimCrv;
        int IN_coreCrvs;
        int IN_voxelRects;
        int IN_obstacleCrvs;

        int OUT_lines;
        int OUT_hit;

        public override GH_Exposure Exposure => GH_Exposure.primary;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IN_autoColor = pManager.AddBooleanParameter("Preview Covid compromised cells", "Autocolor", "Built in preview showing which cells are Covid compromised", GH_ParamAccess.item, false);
            pManager[IN_autoColor].Optional = true;
            IN_plane = pManager.AddPlaneParameter("Plane", "Plane", "Plane describing the current floor plan", GH_ParamAccess.item, Plane.WorldXY);
            pManager[IN_plane].Optional = true;
            IN_perimCrv = pManager.AddCurveParameter("Perimeter Curve", "Perimeter Curve", "Perimeter Curve", GH_ParamAccess.item);
            IN_coreCrvs = pManager.AddCurveParameter("Core Curve(s)", "Core Curve(s)", "Core Curve(s)", GH_ParamAccess.list);
            pManager[IN_coreCrvs].Optional = true;
            IN_voxelRects = pManager.AddRectangleParameter("Voxels", "Voxels", "Voxels for analysis", GH_ParamAccess.list);
            IN_obstacleCrvs = pManager.AddCurveParameter("Obstacle Curves/ Interior Partitions", "Interior Partitions", "The interior partitions/obstacle curves used to do Covid analysis", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            OUT_hit = pManager.AddIntegerParameter("Collisions", "Collisions", " 0 = no collision, 1 = collision", GH_ParamAccess.list);
            OUT_lines = pManager.AddLineParameter("Collision lines", "Collision lines", "Collision lines used to determine whether a cell is compromised", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Plane plane = Plane.WorldXY;
            Curve perimCrv = null;
            coreCrvs = new List<Curve>();
            rectangles = new List<Rectangle3d>();
            obstacleCrvs = new List<Curve>();
            DA.GetData(IN_autoColor, ref autoColor);

            DA.GetData(IN_plane, ref plane);
            DA.GetData(IN_perimCrv, ref perimCrv);
            DA.GetDataList(IN_voxelRects, rectangles);
            DA.GetDataList(IN_obstacleCrvs, obstacleCrvs);
            bool coreReceived = DA.GetDataList(IN_coreCrvs, coreCrvs);

                _plan = new SmartPlan(perimCrv, coreCrvs, rectangles, obstacleCrvs, plane);

            Rhino.RhinoDoc doc = Rhino.RhinoDoc.ActiveDoc;
            Rhino.UnitSystem system = doc.ModelUnitSystem;
            _plan.projectUnits = system.ToString() == "Meters" ? 0 : 1;

            _plan.ComputeCovid();

            compromisedMetric = _plan.GetCovidMetric();

            DA.SetDataList(OUT_hit, compromisedMetric);
            DA.SetDataTree(OUT_lines, _plan.covidLines);

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
                return Properties.Resources.CovidComp_01;
            }
        }
        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {

            if (autoColor)
            {
                gradientList = new System.Drawing.Color[_plan.getCells().Count];
                for (int i = 0; i < gradientList.Length; i++)
                {
                    var multiplier = compromisedMetric[i];

                    Color cellCol = multiplier == 1 ? Color.Red : Color.White;
                    var gColor = new ColorHSL(cellCol);
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
                    var multiplier = compromisedMetric[i];
                    Color cellCol = multiplier == 1 ? Color.Red : Color.White;
                    var gColor = new ColorHSL(cellCol);
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
            get { return new Guid("9d93d22b-00e6-4521-b7a9-fb4d9429746c"); }
        }

    }
}