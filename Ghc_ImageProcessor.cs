using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlanBee
{
    public class Ghc_ImageProcessor: GH_Component
    {

        ImageProcessor imageProcessor;

        public Ghc_ImageProcessor()
         : base("Image Processor", "Image Processor",
             "Derive primeter, core, and interior partition polygons from a .jpg image by drawing them in 3 separate RGB colors and specifying those colors as inputs to this component",
             "PlanBee", "Inputs")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;


        int IN_imagePath;
        int IN_colList;

        int OUT_polyTree;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IN_imagePath = pManager.AddTextParameter("Image Path", "Path", "The path to a local .jpg", GH_ParamAccess.item);
            IN_colList = pManager.AddTextParameter("Color List As String", "Color List", "Color list (of RGB strings).", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            OUT_polyTree = pManager.AddCurveParameter("Polylines", "Polylines", "Polyline tree describing the different colored polygons by branch.", GH_ParamAccess.item);
           
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {

            List<string> colList = new List<string>();
            string path = "";

            DA.GetData(IN_imagePath, ref path);
            DA.GetDataList(IN_colList, colList);

            imageProcessor = new ImageProcessor(path, colList);

            var areaPts = imageProcessor._areaPoints;
            var programCentroids = imageProcessor._progCentroids;
            var perimPts = imageProcessor._ProcessedPts;

            BoundaryExtractor engine = new BoundaryExtractor(perimPts, programCentroids);

            var ptTree = engine.outTree;

            //temp polylineTree
            DataTree<Polyline> treeInterim = new DataTree<Polyline>();
            for (int i = 0; i < ptTree.BranchCount; i++)
            {
                Polyline poly = new Polyline();
                poly.AddRange(ptTree.Branch(i));
                treeInterim.Add(poly, new GH_Path(ptTree.Path(i).Indices[0]));
            }

            //final output tree
            DataTree<Polyline> treeOut = new DataTree<Polyline>();
            for (int i = 0; i < treeInterim.BranchCount; i++)
            {
                if (treeInterim.Branch(i).Count == 2)
                    treeOut.Add(treeInterim.Branch(i).OrderByDescending(l => l.Length).ToArray()[0], new GH_Path(treeInterim.Path(i)));
                else
                    treeOut.AddRange(treeInterim.Branch(i), new GH_Path(treeInterim.Path(i)));
            }

            DA.SetDataTree(OUT_polyTree, treeOut);
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
                return PlanBee.Properties.Resources.ImageProcessor_01;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("a45590d9-d0a4-4f2e-a002-872c5d8ae57e"); }
        }
    }
}
