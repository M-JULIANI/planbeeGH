using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;
using Grasshopper;
using Rhino.Geometry;
using Rhino.Display;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;

namespace PlanBee
{
    public class Ghc_KohonenMap : GH_TaskCapableComponent<Ghc_KohonenMap.SolveResults>
    {
        bool isRunning = false;
        bool run = false;
        bool reset;
        bool showSOM;
        bool displayText;
        int display;
        double radiusMultiplier;
        double _resolution;
        List<System.Drawing.Color> colors;
        List<System.Drawing.Color> gradientList;
        List<string> pNames;
        List<double> pAreas;
        int maximumIterations = 150;
        List<double> radii;
        List<Polyline> circles = new List<Polyline>();
        KMap SOM = null;

        /// <summary>
        /// Initializes a new instance of the GhcKohonenMap class.
        /// </summary>
        public Ghc_KohonenMap()
          : base("Kohonen Self Organizing Feature Map", "Kohonen SOM",
              "Produced a self-organizing feature map of the voxels/ analysis grid corresponding to the floor plan",
              "PlanBee", "Solver")
        {
        }

        int IN_reset;
        int IN_run;
        int IN_mode;
        int IN_programNames;
        int IN_programAreas;
        int IN_programData;
        int IN_programColors;
        int IN_pRects;
        int IN_numMetrics;
        int IN_pWeights;
        int IN_resolution;
        int IN_radMultiplier;
        int IN_showSOM;
        int IN_SOMDisplay;
        int IN_DisplayLabels;

        int OUT_programTree;
        int OUT_nodeWeights;
        int OUT_RunFeedback;

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IN_reset = pManager.AddBooleanParameter("Reset", "Reset", "Reset Kohonen SOM", GH_ParamAccess.item, false);
            IN_run = pManager.AddBooleanParameter("Run", "Run", "Run Kohonen SOM", GH_ParamAccess.item, false);
            IN_mode = pManager.AddBooleanParameter("Area Pseudo Optimized or Features Raw", "'Optimized'?", "Either True: areas are pesudo optimized or False: they are left raw. To run in either mode hit 'reset' button followed by 'run'", GH_ParamAccess.item, false);
            IN_programNames = pManager.AddTextParameter("Program Names", "Program Names", "The list of program names your are using in list order", GH_ParamAccess.list);
            IN_programAreas = pManager.AddNumberParameter("Program Areas", "Program Areas", "The list of program areas your are using in list order", GH_ParamAccess.list);
            IN_programData = pManager.AddNumberParameter("Program Data", "Program Data", "Program vectors used to describe the features of each program (coming from 'Parse Program Data' component", GH_ParamAccess.tree);
            IN_programColors = pManager.AddColourParameter("Program Colors", "Program Colors", "An ordered list of the corresponding colors representing each program", GH_ParamAccess.list);
            IN_pRects = pManager.AddRectangleParameter("Voxels", "Voxels", "Voxels used for Kohonen SOM", GH_ParamAccess.list);
            IN_numMetrics = pManager.AddIntegerParameter("Number of Metrics", "Number of Metrics", "Number of Metrics used for KSOM. This should match the length of each branch in of 'voxel weights'.", GH_ParamAccess.item);
            IN_pWeights = pManager.AddNumberParameter("Voxel Weights", "Voxel Weights", "Voxel weights used as a multidimensional vector describing each voxel's features", GH_ParamAccess.tree);
            IN_resolution = pManager.AddNumberParameter("Voxel Resolution", "Voxel Resolution", "The resolution describing the size of the voxels", GH_ParamAccess.item);
            IN_radMultiplier = pManager.AddNumberParameter("Initial radius multiplier", "Radius multiplier", "Initial radius multiplier used for Kohonen SOM", GH_ParamAccess.item);
            IN_showSOM = pManager.AddBooleanParameter("Display Kohonen SOM", "Display SOM", "Auto preview Kohonen Feature Map. Set to 'True' only once running is complete and once 'run' is also manually set to False by you", GH_ParamAccess.item, false);
            IN_SOMDisplay = pManager.AddIntegerParameter("Kohonen SOM Display Type", "Display Type", "Enabled user to toggle between different view modes of the feature map. One corresponds to The discrete building program, the other is how strong each" +
                "voxel is to its best matching unit, and the last mode provides a visualization of raw node weights. 0: Discretized SOM, 1: BMU Multiploer, 2: Weight viz", GH_ParamAccess.item);
            IN_DisplayLabels = pManager.AddBooleanParameter("Display Voxel Labels", "Display Labels", "Displays the programmatic label of each voxel as well as its relative strength (expressed as index: lower = stronger)", GH_ParamAccess.item, false);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            OUT_RunFeedback = pManager.AddTextParameter("Run Status", "Run Status", "This output tells the user what run status this component is under", GH_ParamAccess.item);
            OUT_programTree = pManager.AddCurveParameter("Program Voxels", "Program Voxels", "Voxels assigned to their corresponding program tree", GH_ParamAccess.tree);
            OUT_nodeWeights = pManager.AddNumberParameter("Final node weights", "Node Weights", "The final node weights after the Kohonen SOM is done running", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            colors = new List<System.Drawing.Color>();
            pNames = new List<string>();
            pAreas = new List<double>();
            List<Rectangle3d> rects = new List<Rectangle3d>();
            GH_Structure<GH_Number> pData;
            GH_Structure<GH_Number> pWeights;
            List<Point3d> points = new List<Point3d>();

            DataTree<double> mod_pData;
            DataTree<double> mod_pWeights;
            string message = "nothing";
            bool optimized = false;
            int numMetrics = -1;


            DA.GetData(IN_numMetrics, ref numMetrics);
            DA.GetData(IN_reset, ref reset);
            DA.GetData(IN_run, ref run);
            DA.GetData(IN_mode, ref optimized);
            DA.GetDataList(IN_programNames, pNames);
            DA.GetDataList(IN_programAreas, pAreas);
            DA.GetDataTree(IN_programData, out pData);
            DA.GetDataList(IN_programColors, colors);
            DA.GetDataList(IN_pRects, rects);
            DA.GetDataTree(IN_pWeights, out pWeights);
            DA.GetData(IN_resolution, ref _resolution);
            DA.GetData(IN_radMultiplier, ref radiusMultiplier);
            DA.GetData(IN_showSOM, ref showSOM);
            DA.GetData(IN_SOMDisplay, ref display);
            DA.GetData(IN_DisplayLabels, ref displayText);

            if (run)
                isRunning = true;
            else
                isRunning = false;

            if (isRunning)
                ExpireSolution(true);


            try
            {

                if (reset || SOM == null)
                {
                    circles = new List<Polyline>();
                    points = new List<Point3d>();

                    radii = new List<double>();
                    gradientList = new List<System.Drawing.Color>();

                    for (int i = 0; i < rects.Count; i++)
                        points.Add(rects[i].Center);

                    for (int i = 0; i < pAreas.Count; i++)
                    {
                        var rad = Math.Sqrt(pAreas[i] / Math.PI);
                        radii.Add(rad * radiusMultiplier);
                    }

                    mod_pData = new DataTree<double>();
                    mod_pWeights = new DataTree<double>();


                    for (int i = 0; i < pData.Paths.Count; i++)
                    {
                        var path = pData.get_Branch(i);
                        for (int j = 0; j < path.Count; j++)
                        {
                            var p = new GH_Path(i);
                            var prog = pData.get_DataItem(p, j).Value;
                            mod_pData.Add(prog, new GH_Path(i));
                        }
                    }
                    for (int i = 0; i < pWeights.Paths.Count; i++)
                    {
                        var path = pWeights.get_Branch(i);
                        for (int j = 0; j < path.Count; j++)
                        {
                            var p = new GH_Path(i);
                            var weight = pWeights.get_DataItem(p, j).Value;
                            mod_pWeights.Add(weight, new GH_Path(i));
                        }
                    }

                    //DA.SetDataTree(OUT_nodeWeights,mod_pWeights);
                    SOM = new KMap(mod_pData, points, _resolution, mod_pWeights, numMetrics, 0.12, radii, 1.0, maximumIterations);

                    SOM.applyProgramInputs(pNames.Count);
                    SOM.outputNodesXY();
                    reset = false;
                }


                if (SOM.iter < maximumIterations)
                {
                    if (InPreSolve)
                    {
                        Task<SolveResults> task = Task.Run(() => ComputeSOM(SOM), CancelToken);
                        TaskList.Add(task);
                        return;
                    }

                    if (!GetSolveResults(DA, out SolveResults result))
                    {
                        result = ComputeSOM(SOM);
                        SOM = result.Value;
                    }

                    message = string.Format("Runnning...{0}/{1} iterations", SOM.iter, maximumIterations);
                }
                else
                {
                    message = "Done.";
                    isRunning = false;
                    run = false;
                    ClearData();
                }

                if (showSOM)
                {
                    var nL = new List<double>();
                    SOM.nodeWeights.Clear();
                    for (int i = 0; i < SOM.nodeW.BranchCount; i++)
                    {
                        GH_Path path = SOM.nodeW.Path(i);
                        int elementCount = SOM.nodeW.Branch(i).Count;
                        double average = 0;

                        for (int j = 0; j < elementCount; j++)
                            average += SOM.nodeW[path, j];

                        average /= elementCount;

                        nL.Add(average); //average value of all dims of a node
                    }

                    SOM.nodeWeights = nL;

                    Remapper re = new Remapper(SOM.nodeWeights, SOM.nodePoints, pNames, pAreas, _resolution, optimized);


                    var n = re.nodes;
                    var _tree = re.programTree;

                    //'tree' below is the sorted nodes corresponding to each program based on their BMU
                    var tempTree = new DataTree<sNode>();

                    for (int i = 0; i < _tree.BranchCount; i++)
                    {
                        int localBCount = _tree.Branch(i).Count;

                        var orderedBranch = _tree.Branch(i).OrderBy(c => c.multiplierStrength).ToList();
                        tempTree.AddRange(orderedBranch, new GH_Path(i));
                    }

                    SOM.tree.Clear();
                    for (int i = 0; i < tempTree.BranchCount; i++)
                    {
                        var fPath = tempTree.Path(i);
                        int localBCount = tempTree.Branch(i).Count;

                        for (int j = 0; j < localBCount; j++)
                        {

                            Plane plane = new Plane(tempTree.Branch(i)[j].pos, Vector3d.ZAxis);
                            Interval inter = new Interval(-_resolution * 0.5, _resolution * 0.5);
                            var rect = new Rectangle3d(plane, inter, inter);
                            var poly = rect.ToPolyline();
                            //circles.Add(poly);

                            SOM.tree.Add(poly, new GH_Path(fPath));
                        }
                    }

                    //data for visualization

                    var _points = new List<Point3d>();
                    SOM.m_pNames = new List<string>();
                    for (int i = 0; i < n.Count; i++)
                    {
                        var p = n[i].pos;
                        var name = n[i].name;
                        var multiplier = n[i].multiplierStrength;
                        _points.Add(p);
                        SOM.m_pNames.Add(name);
                    }


                    //color by gradient colors
                    // for (int i = 0; i < nodeWeights.Count; i++)

                    if (display == 1)
                    {
                        gradientList = new List<System.Drawing.Color>();
                        for (int i = 0; i < n.Count; i++)
                        {
                            var multiplier = n[i].multiplierStrength;
                            var gColor = new ColorHSL(multiplier, 0, multiplier);

                            var rgb = gColor.ToArgbColor();
                            gradientList.Add(rgb);
                        }
                    }

                    else if (display == 2)
                    {
                        gradientList = new List<System.Drawing.Color>();
                        for (int i = 0; i < n.Count; i++)
                        {
                            var gColor = new ColorHSL(SOM.nodeWeights[i], SOM.nodeWeights[i], SOM.nodeWeights[i], SOM.nodeWeights[i]);

                            var rgb = gColor.ToArgbColor();
                            gradientList.Add(rgb);
                        }
                    }


                    for (int i = 0; i < SOM.m_pNames.Count; i++)
                    {
                        for (int j = 0; j < pNames.Count; j++)
                        {
                            var strippedName = SOM.RemoveNumbersSymbols(SOM.m_pNames[i]);
                            if (strippedName == pNames[j])
                            {
                                SOM.discreteCol.Add(colors[j]);
                            }
                        }
                    }
                    SOM.drawingPolys.Clear();
                    SOM.drawCircles();

                    circles = SOM.drawingPolys;
                }
            }
            catch (Exception e)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.ToString());
            }

            DA.SetData(OUT_RunFeedback, message);
            DA.SetDataList(OUT_nodeWeights, SOM.nodeWeights);
            DA.SetDataTree(OUT_programTree, SOM.tree);
        }

        public class SolveResults
        {
            public KMap Value { get; set; }
        }

        public static SolveResults ComputeSOM(KMap _som)
        {
            SolveResults result = new SolveResults();
            _som.trainNodes();
            _som.outputNodeWeights();
            _som.iter++;
            result.Value = _som;
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
                return PlanBee.Properties.Resources.SOM_01;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("23b3d062-cd08-40f5-a554-e65844d62a7b"); }
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (showSOM)
            {
                if (display == 0)
                {
                    for (int i = 0; i < circles.Count; i++)
                    {
                        Rhino.Display.DisplayMaterial mat = new Rhino.Display.DisplayMaterial(SOM.discreteCol[i]);
                        mat.Shine = 0.25;
                        //if(srMode == 0)
                        {
                            var curve = circles[i].ToNurbsCurve();
                            args.Display.DrawPolyline(circles[i], Color.Black, (int)(SOM.nodeWeights[i] * 3.0));

                            var mesh = Mesh.CreateFromPlanarBoundary(curve, Rhino.Geometry.MeshingParameters.FastRenderMesh, 0.01);
                            args.Display.DrawMeshShaded(mesh, mat);
                        }
                    }
                }

                else
                {
                    for (int i = 0; i < circles.Count; i++)
                    {
                        Rhino.Display.DisplayMaterial mat = new Rhino.Display.DisplayMaterial(gradientList[i]);
                        mat.Shine = 0.25;
                        //if(srMode == 0)
                        {
                            var curve = circles[i].ToNurbsCurve();
                            args.Display.DrawPolyline(circles[i], Color.Black, (int)(SOM.nodeWeights[i] * 3.0));

                            var mesh = Mesh.CreateFromPlanarBoundary(curve, Rhino.Geometry.MeshingParameters.FastRenderMesh, 0.01);
                            args.Display.DrawMeshShaded(mesh, mat);
                        }
                    }
                }


                if (displayText)
                {
                    //program names
                    for (int i = 0; i < SOM.nodePoints.Count; i++)
                    {
                        var pos = SOM.nodePoints[i];
                        args.Display.Draw2dText(SOM.m_pNames[i], Color.Black, pos, true, (int)15.0);
                    }
                }

            }
        }
    }
}