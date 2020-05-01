using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace PlanBee
{
    public class PlanBeeInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "PlanBee";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return PlanBee.Properties.Resources.PlanBee100_75_01_01;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "A collection of functions used to compute various properties of floor plans with discrete units.Using the different computed properties of each " +
                    "floor plan voxel as well as the desired properties ascribed to each building program," +
                    "this plug-in allows users to create feature maps (using Kohonen Nets) which negotatiates the two. " +
                    "The purpose of the tool is to visualize plan/ programmatic features and to quickly generate diagrammatic floor plan layouts that expresses that feature information.";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("a4a6b4ce-eafd-44d8-b222-982070f4c76c");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "Marco Juliani";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "marcotjuliani@gmail.com";
            }
        }
    }
}
