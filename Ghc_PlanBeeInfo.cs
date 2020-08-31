using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace PlanBee
{
    public class Ghc_PlanBeeInfo : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the Ghc_PlanBeeInfo class.
        /// </summary>
        public Ghc_PlanBeeInfo()
          : base("PlanBee Description", "PlanBee Description",
              "PlanBee Description",
              "PlanBee", "About")
        {
        }

        int OUT_string;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            OUT_string = pManager.AddTextParameter("Description", "Description", "Describes what PlanBee does", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string outText = "A collection of functions used to compute various properties of floor plans" +
                    "The purpose of the tool is to visualize plan/ programmatic features and to quickly generate diagrammatic floor plan layouts that expresses that feature information." +
                    "" +
                    "For more information please visit https://www.food4rhino.com/app/planbee.";

            DA.SetData(OUT_string, outText);
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
                return Properties.Resources.PlanBee_Component_01;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("0275e657-013c-4e0d-9e81-1aeebb6522cb"); }
        }
    }
}