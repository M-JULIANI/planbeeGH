using Grasshopper.Kernel;
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
         : base("Plan Cells", "Cells",
             "Plan voxels/ 'analysis grid' serving as the basic unit for which different analysis metrics are computed",
             "PlanBee", "Inputs")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.primary;


        int IN_imagePath;
        int IN_colList;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IN_imagePath = pManager.AddTextParameter("Image Path", "Path", "The path to a local .jpg", GH_ParamAccess.item);
            IN_colList = pManager.AddTextParameter("Color List As String", "Color Liss", "Color list (of RGB strings).", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            OUT_areaFeedback = pManager.AddTextParameter("Area feedback", "Area feedback", "Provides feedback as to whether desired areas are being met with current configuration", GH_ParamAccess.item);
            OUT_cells = pManager.AddRectangleParameter("Plan Voxels", "Voxels", "Analysis unit size", GH_ParamAccess.list);
            OUT_perimeter = pManager.AddCurveParameter("Perimeter Curve", "Perimeter", "The curve that describes the extents of the floor plan boundary", GH_ParamAccess.item);
            OUT_core = pManager.AddCurveParameter("Core Curve", "Core", "The curve that describes the extents of the core boundaries", GH_ParamAccess.list);
            OUT_resolution = pManager.AddNumberParameter("Resolution", "Resolution", "This final resolution may differ from the input resolution as this component takes care of ensuring the resolution isn't too fine or coarse.", GH_ParamAccess.item);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {

            List<string> colList = new List<string>();
            string path = "";

            DA.GetData(IN_imagePath, ref path);
            DA.GetDataList(IN_colList, colList);

            imageProcessor = new ImageProcessor(path, colList);

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
                return PlanBee.Properties.Resources.Cells;
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
