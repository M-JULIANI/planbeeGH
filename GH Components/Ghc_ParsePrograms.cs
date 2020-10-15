using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using System.IO;

using Grasshopper;
using Grasshopper.Kernel.Data;


namespace PlanBee
{
    public class Ghc_ParsePrograms : GH_Component
    {

        string areaPath = "None yet";
        string featurePath = "None yet";

        /// <summary>
        /// Initializes a new instance of the GhcParsePrograms class.
        /// </summary>
        public Ghc_ParsePrograms()
          : base("Parse Program Data", "Parse",
              "Parses program data (.csv files)",
              "PlanBee", "Inputs")
        {
        }

        public override GH_Exposure Exposure => GH_Exposure.secondary;

        int IN_programAreas;
        int IN_programFeatures;
        int OUT_programNames;
        int OUT_programAreas;
        int OUT_programFeatures;

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IN_programAreas = pManager.AddTextParameter("Program Areas", "Areas", "The path to a CSV file descriing the areas of the floorplate", GH_ParamAccess.item, areaPath);
            pManager[IN_programAreas].Optional = true;
            IN_programFeatures = pManager.AddTextParameter("Program Features", "Features", "The path to a CSV file descriing the features of each program", GH_ParamAccess.item, featurePath);
            pManager[IN_programFeatures].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            OUT_programNames =  pManager.AddTextParameter("Names", "Names", "A list of program names to be used downstream", GH_ParamAccess.list);
            OUT_programAreas = pManager.AddNumberParameter("Areas", "Areas", "A list of program areas to be used downstream", GH_ParamAccess.list);
            OUT_programFeatures = pManager.AddNumberParameter("Features", "Features", "A tree of program features to be used downstream", GH_ParamAccess.tree);
        }

        public void GetPaths()
        {
            string filePath = this.OnPingDocument().FilePath;
            var pathParts = filePath.Split('\\');
            string directory = "";
            for (int i = 0; i < pathParts.Length - 1; i++)
            {
                directory += pathParts[i];
                if (i != pathParts.Length - 1)
                    directory += "/";
            }
            //var directory = pathParts[0] + pathParts[1];
            try
            {
                featurePath = Directory.GetFiles(directory, "Program Features.csv")[0];
                areaPath = Directory.GetFiles(directory, "Program Areas.csv")[0];
                // A = directory;
            }
            catch (Exception)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Files not named properly: 'Program Areas.csv'/ 'Program Features.csv'. See sample files.");
            }
        }
        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
  
            bool success1 = DA.GetData(0, ref areaPath);
            bool success2 = DA.GetData(1, ref featurePath);

            if (success1 == false || success2 == false)
            {
                GetPaths();
            }

            else 
            {

                /////Basic Program Data///////
                string[] areas = File.ReadAllLines(areaPath);
                var programs = new List<string>(); // program names
                var sizes = new List<double>(); // program areas/sizes

                for (int i = 1; i < areas.Length; i++)
                {
                    string[] cells = areas[i].Split(',');
                    programs.Add(cells[0]);
                    sizes.Add(double.Parse(cells[1]));
                }

                ////Program outputs
                DA.SetDataList(OUT_programNames, programs); //names
                DA.SetDataList(OUT_programAreas, sizes); //areas


                ////////kohonenData//////////
                string[] kData = File.ReadAllLines(featurePath);
                DataTree<double> progFeatures = new DataTree<double>(); // features

                int count = 0;
                for (int i = 1; i < kData.Length; i++)
                {
                    string[] cells = kData[i].Split(',');
                    int numFeatures = cells.Length - 1;
                    for (int j = 1; j < numFeatures + 1; j++)
                        progFeatures.Add(double.Parse(cells[j]), new GH_Path(count));

                    count++;
                }
                //////Kohonen Outputs
                DA.SetDataTree(OUT_programFeatures, progFeatures); //features
            }
            //else
            //    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Feed valid paths to CSV files. Make sure to leave row and column headers per sample files");

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
                return PlanBee.Properties.Resources.ParseProgramIcon_01;
                //return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("60bc7af8-62ca-44de-856b-410530b1220d"); }
        }
    }
}