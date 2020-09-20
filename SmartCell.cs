using System;
using Rhino.Geometry;


namespace PlanBee
{

    public struct Vector2dInt: IEquatable<Vector2dInt>
    { 
        private int x { get; set; }
        public int X { get { return x; }} 
        private int y { get; set; }
        public int Y { get { return y; } }

        public Vector2dInt(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public static Vector2dInt operator -(Vector2dInt vec1, Vector2dInt vec2)
        {
            Vector2dInt outVec = new Vector2dInt(vec1.X - vec2.X, vec1.Y - vec2.Y);
            return outVec;
        }

        public Vector2d ToVector2d()
        {
            Vector2d output = new Vector2d(this.X, this.Y);
            return output;
        }

        public bool Equals(Vector2dInt other)
        {
            if (other == null)
                return false;

            if (this.X == other.X && this.Y == other.Y)
                return true;
            else
                return false;
        }

        public override bool Equals(Object obj)
        {
            if (obj == null)
                return false;

            if (obj.GetType().ToString() != "Vector2dInt")
                return false;
            else
                return Equals((Vector2dInt)obj);
        }

        public override int GetHashCode()
        {
            return Tuple.Create(x, y).GetHashCode();
        }

        public static bool operator ==(Vector2dInt vec1, Vector2dInt vec2)
        {
            if (((object)vec1) == null || ((object)vec2) == null)
                return Object.Equals(vec1, vec2);

            return vec1.Equals(vec2);
        }

        public static bool operator !=(Vector2dInt vec1, Vector2dInt vec2)
        {
            if (((object)vec1) == null || ((object)vec2) == null)
                return !Object.Equals(vec1, vec2);

            return !(vec1.Equals(vec2));
        }

    }
    public class SmartCell
    {
        public double metric1; // isovist
        public double metric2; // distance to perimeter curve/ daylight
        public double metric3; //distance to attractions
        public double metric4; //distance to closest exit
        public double metric5; // meanshortestpath
        public double mspRaw; // meanshortestpath
        public int covidMetric;
        public double neighSizeRaw;
        public double neighSize;
        public double tempMetric;
        public Vector2d location;
        public Vector2dInt index;
        public double _resolution;
        public Rectangle3d rect;


        Grid2d _grid;

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
            int roundedX = (int)(location.X);
            roundedX *= 5;
            int roundedY = (int)(location.Y);
            roundedY *= 5;

            //index = new Vector2d(Math.Round(roundedX / this._resolution * 5.0), Math.Round(roundedX / this._resolution * 5.0));
           index = new Vector2dInt((int)Math.Round(location.X / this._resolution), (int)Math.Round(location.Y / this._resolution));

  
            this.metric1 = metric1;
            this.metric2 = metric2;
            this.metric3 = metric3;
            this.metric4 = metric4;
            this.tempMetric = 0.0;
            this.mspRaw = 0.0;
            this.neighSize = 0.0;
            this.neighSizeRaw = 0.0;
            this.covidMetric = 0;
            Interval interval = new Interval(-this._resolution / 2.0, this._resolution / 2.0);
            Plane plane = new Plane(new Point3d(location.X, location.Y, 0), Vector3d.ZAxis);
            rect = new Rectangle3d(plane, interval, interval);
            isActive = true;
        }

        public SmartCell(Vector2d location, double _resolution)
        {
            this._resolution = _resolution;
            this.location = location;
            int roundedX = (int)location.X;
            roundedX *= 5;
            int roundedY = (int)(location.Y);
            roundedY *= 5;

            //index = new Vector2d(Math.Round(roundedX / this._resolution * 5.0), Math.Round(roundedX / this._resolution * 5.0));

            index = new Vector2dInt((int)Math.Round(location.X / this._resolution), (int)Math.Round(location.Y / this._resolution));
           
            this.metric1 = 0.0;
            this.metric2 = 0.0;
            this.metric3 = 0.0;
            this.metric4 = 0.0;
            this.metric5 = 0.0;
            this.mspRaw = 0.0;
            this.tempMetric = 0.0;
            this.neighSize = 0.0;
            this.neighSizeRaw = 0.0;
            this.covidMetric = 0;
            Interval interval = new Interval(-this._resolution / 2.0, this._resolution / 2.0);
            Plane plane = new Plane(new Point3d(location.X, location.Y, 0), Vector3d.ZAxis);
            rect = new Rectangle3d(plane, interval, interval);
            isActive = true;

        }

        public SmartCell(Vector2d location, double _resolution, Grid2d grid)
        {
            _grid = grid;
            this._resolution = _resolution;
            this.location = location;
            int roundedX = (int)location.X;
            roundedX *= 5;
            int roundedY = (int)(location.Y);
            roundedY *= 5;

            //index = new Vector2d(Math.Round(roundedX / this._resolution * 5.0), Math.Round(roundedX / this._resolution * 5.0));

            index = new Vector2dInt((int)Math.Round(location.X / this._resolution), (int)Math.Round(location.Y / this._resolution));

            this.metric1 = 0.0;
            this.metric2 = 0.0;
            this.metric3 = 0.0;
            this.metric4 = 0.0;
            this.metric5 = 0.0;
            this.mspRaw = 0.0;
            this.tempMetric = 0.0;
            this.neighSize = 0.0;
            this.neighSizeRaw = 0.0;
            this.covidMetric = 0;
            Interval interval = new Interval(-this._resolution / 2.0, this._resolution / 2.0);
            Plane plane = new Plane(new Point3d(location.X, location.Y, 0), Vector3d.ZAxis);
            rect = new Rectangle3d(plane, interval, interval);
            isActive = true;

        }

        public Face[] Faces
        {
            get
            {
                int x = index.X;
                int y = index.Y;

                return new[]
                {
                  _grid.Faces[new AxisVector2dInt(PBUtilities.Axis.X, x - 1, y)],
                  _grid.Faces[new AxisVector2dInt(PBUtilities.Axis.X, x + 1, y)],
                  _grid.Faces[new AxisVector2dInt(PBUtilities.Axis.Y, x, y - 1)],
                  _grid.Faces[new AxisVector2dInt(PBUtilities.Axis.Y, x, y + 1)],
                };
            }
        }

    }

}