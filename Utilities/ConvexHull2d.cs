using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlanBee
{
    public class ConvexHull2d
    {
        public bool done;
        public List<Point3d> proxy = new List<Point3d>();
        public List<Point3d> convexC = new List<Point3d>();
        public int count;
        public Point3d initial;
        public List<Point3d> start = new List<Point3d>();
        public Polyline poly = new Polyline();


        public ConvexHull2d(List<Point3d> pts)
        {
            proxy = pts;
        }

        public void Init()
        {
            start = proxy.OrderBy(v => v.Y).ToList();
            initial = start[0];
            count = 0;
            convexC.Clear();
            done = false;
        }

        public string status()
        {
            return "done";
        }

        public void Calculate()
        {
            while (done == false)
            {
                Vector3d currentVec;
                Vector3d nextVec;

                Point3d current;
                List<SPoint> dotList = new List<SPoint>();


                if (count == 0)
                {
                    current = initial;
                    var dummyPoint = new Point3d(current.X + 5, current.Y, current.Z);
                    currentVec = dummyPoint - current;
                }

                else if (count == 1)
                {
                    current = convexC[count - 1];
                    currentVec = initial - current;
                }
                else
                {
                    current = convexC[count - 1];
                    currentVec = convexC[count - 2] - current;
                }

                for (int i = 0; i < proxy.Count; i++)
                {
                    {
                        if (proxy[i] == current) continue;
                        else
                        {
                            nextVec = proxy[i] - current;
                            var dot = Vector3d.Multiply(currentVec, nextVec);
                            var angle = Math.Acos(dot / (currentVec.Length * nextVec.Length));
                            var dist = current.DistanceTo(proxy[i]);
                            dotList.Add(new SPoint(proxy[i], angle));

                        }
                    }
                }

                var tList = dotList.OrderByDescending(v => v.val).ToList();
                current = tList[0].point;
                convexC.Add(current);
                count++;

                if (current == initial)
                {
                    done = true;
                }
                else
                {
                    done = false;
                }
            }
            convexC.Add(convexC[0]);

            status();
            poly.AddRange(convexC);
        }
    }

    //custom point/double class to sort points based on a dot product
    public class SPoint
    {
        public Point3d point;
        public double val;

        public SPoint(Point3d point, double val)
        {
            this.point = point;
            this.val = val;
        }
    }
}
