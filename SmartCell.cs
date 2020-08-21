using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using System.IO;
using System.Linq;
using System.Data;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

using Rhino.DocObjects;
using Rhino.Collections;
using GH_IO;
using GH_IO.Serialization;

using Grasshopper;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;

namespace Planbee
{
    public class SmartCell
    {
        public double metric1; // isovist
        public double metric2; // distance to perimeter curve/ daylight
        public double metric3; //distance to attractions
        public double metric4; //distance to closest exit
        public double metric5; // meanshortestpath
        public double mspRaw; // meanshortestpath
        public double neighSizeRaw;
        public double neighSize;
        public double tempMetric;
        public Vector2d location;
        public Vector2d index;
        public double _resolution;
        public Rectangle3d rect;

        public int gCost;
        public int hCost;

        public int FCost { get { return gCost + hCost; } }
        public bool isActive { get; set; }

        public SmartCell Parent;

        //public Grid2d _grid;
        // public bool IsClimbable => IsActive && Faces.Any(f => f.IsClimbable);

        public SmartCell(Vector2d location, double _resolution, double metric1, double metric2, double metric3, double metric4)
        {
            this._resolution = _resolution;
            this.location = location;
            int roundedX = (int)Math.Round(location.X);
            roundedX *= 5;
            int roundedY = (int)Math.Round(location.Y);
            roundedY *= 5;

            //index = new Vector2d(Math.Round(roundedX / this._resolution * 5.0), Math.Round(roundedX / this._resolution * 5.0));
           index = new Vector2d(Math.Round(location.X / this._resolution), Math.Round(location.Y / this._resolution));

            this.metric1 = metric1;
            this.metric2 = metric2;
            this.metric3 = metric3;
            this.metric4 = metric4;
            this.tempMetric = 0.0;
            this.mspRaw = 0.0;
            this.neighSize = 0.0;
            this.neighSizeRaw = 0.0;
            Interval interval = new Interval(-this._resolution / 2.0, this._resolution / 2.0);
            Plane plane = new Plane(new Point3d(location.X, location.Y, 0), Vector3d.ZAxis);
            rect = new Rectangle3d(plane, interval, interval);
            isActive = true;
        }

        public SmartCell(Vector2d location, double _resolution)
        {
            this._resolution = _resolution;
            this.location = location;
            int roundedX = (int)Math.Round(location.X);
            roundedX *= 5;
            int roundedY = (int)Math.Round(location.Y);
            roundedY *= 5;

            //index = new Vector2d(Math.Round(roundedX / this._resolution * 5.0), Math.Round(roundedX / this._resolution * 5.0));

            index = new Vector2d(Math.Round(location.X / this._resolution), Math.Round(location.Y / this._resolution));
           
            this.metric1 = 0.0;
            this.metric2 = 0.0;
            this.metric3 = 0.0;
            this.metric4 = 0.0;
            this.metric5 = 0.0;
            this.mspRaw = 0.0;
            this.tempMetric = 0.0;
            this.neighSize = 0.0;
            this.neighSizeRaw = 0.0;
            Interval interval = new Interval(-this._resolution / 2.0, this._resolution / 2.0);
            Plane plane = new Plane(new Point3d(location.X, location.Y, 0), Vector3d.ZAxis);
            rect = new Rectangle3d(plane, interval, interval);
            isActive = true;

        }

    }

}