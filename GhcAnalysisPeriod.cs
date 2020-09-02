using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Threading.Tasks;

namespace PlanBee
{
    public class GhcAnalysisPeriod : GH_TaskCapableComponent<GhcAnalysisPeriod.SolveResults>
    {
        /// <summary>
        /// Initializes a new instance of the GhcAnalysisPeriod class.
        /// </summary>
        public GhcAnalysisPeriod()
          : base("Daylight Analysis Period", "Analysis Period",
              "Uses Rhinocommon sun system to generate sun vectors for a specified place and period of time. Month (1-12), Time (0-24)",
              "PlanBee", "Inputs")
        {
        }

        int IN_Latitude;
        int IN_Longitude;
        int IN_MonthStart;
        int IN_MonthEnd;
        int IN_TimeStart;
        int IN_TimeEnd;

        int OUT_Vectors;

        PBSun _sun;
        List<Vector3d> sunVecs;


        public override GH_Exposure Exposure => GH_Exposure.secondary;
        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            IN_Latitude = pManager.AddNumberParameter("Location Latitude", "Latitude", "Latitude of the location of Earth which you want to analyze", GH_ParamAccess.item);
            IN_Longitude = pManager.AddNumberParameter("Location Longitude", "Longitude", "Longitude of the location of Earth which you want to analyze", GH_ParamAccess.item);
            IN_MonthStart = pManager.AddIntegerParameter("Start month", "Start month", "Start month of analysis period (1-12)", GH_ParamAccess.item);
            IN_MonthEnd = pManager.AddIntegerParameter("End month", "End month", "End month of analysis period inclusive of last month (1-12)", GH_ParamAccess.item);
            IN_TimeStart = pManager.AddIntegerParameter("Start time", "Start time", "Start time of analysis period (0-24). Start time must precede end time", GH_ParamAccess.item);
            IN_TimeEnd = pManager.AddIntegerParameter("End time", "End time", "End time of analysis period (0-24) inclusive of last hour. Start time must precede end time", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            OUT_Vectors = pManager.AddVectorParameter("Sun vectors", "Sun vectors", "Sun vectors corresponding to the analysis period specified", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double latitude = double.NaN;
            double longitude = double.NaN;
            int monthStart = 0;
            int monthEnd = 0;
            int timeStart = 0;
            int timeEnd = 0;

            if (InPreSolve)
            {
                DA.GetData(IN_Latitude, ref latitude);
                DA.GetData(IN_Longitude, ref longitude);
                DA.GetData(IN_MonthStart, ref monthStart);
                DA.GetData(IN_MonthEnd, ref monthEnd);
                DA.GetData(IN_TimeStart, ref timeStart);
                DA.GetData(IN_TimeEnd, ref timeEnd);

                _sun = new PBSun(latitude, longitude, monthStart, monthEnd, timeStart, timeEnd);
                Task<SolveResults> task = Task.Run(() => ComputeSunVecs(_sun), CancelToken);
                TaskList.Add(task);
                return;
            }

            if (!GetSolveResults(DA, out SolveResults result))
            {
                DA.GetData(IN_Latitude, ref latitude);
                DA.GetData(IN_Longitude, ref longitude);
                DA.GetData(IN_MonthStart, ref monthStart);
                DA.GetData(IN_MonthEnd, ref monthEnd);
                DA.GetData(IN_TimeStart, ref timeStart);
                DA.GetData(IN_TimeEnd, ref timeEnd);
                _sun = new PBSun(latitude, longitude, monthStart, monthEnd, timeStart, timeEnd);
                result = ComputeSunVecs(_sun);
                _sun = result.Value;
            }

            if (result != null)
            {
                sunVecs = _sun.SunVectors;
                DA.SetDataList(OUT_Vectors, sunVecs);
            }


            }

        public class SolveResults
        {
            public PBSun Value { get; set; }
        }

        public static SolveResults ComputeSunVecs(PBSun sun)
        {
            SolveResults result = new SolveResults();
            sun.ComputeVectors();
            result.Value = sun;
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
                return PlanBee.Properties.Resources.Analysis_Period_01;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("8dfa1423-7532-4541-9a33-4c1d5c39c46b"); }
        }
    }
}