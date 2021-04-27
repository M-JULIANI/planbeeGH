using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Linq;


namespace PlanBee
{
    public static class PBUtilities
    {

        public enum Axis { X, Y, Z };
        public enum BoundaryType { Inside = 0, Left = -1, Right = 1, Outside = 2 }

        public static Point3d[] getDiscontinuities(Curve curve)
        {
            var t0 = curve.Domain.T0;
            var t1 = curve.Domain.T1;

            bool done = false;
            var ts = new List<double>();

            ts.Add(0.0);
            while (done == false)
            {
                double t;
                if (curve.GetNextDiscontinuity(Continuity.G1_continuous, t0, t1, out t))
                {
                    if (ts.Contains(t) == false)
                    {
                        ts.Add(t);
                        t0 = t;
                    }
                }
                else
                    done = true;
            }

            var pts = new Point3d[ts.Count];
            for (int i = 0; i < pts.Length; i++)
            {
                pts[i] = curve.PointAt(ts[i]);
            }
            return pts;
        }

        public static double mapValue(double mainValue, double inValueMin, double inValueMax, double outValueMin, double outValueMax)
        {
            return (mainValue - inValueMin) * (outValueMax - outValueMin) / (inValueMax - inValueMin) + outValueMin;
        }

        public static double RemapValue(List<double> inputList, double inputVal)
        {
            var sort1 = inputList.OrderBy(i => i).ToList();
            var T0 = sort1[0];
            var T1 = sort1[sort1.Count - 1];
            var outputVal = mapValue(inputVal, T0, T1, 0.0, 1.0);

            return outputVal;
        }

        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> items,
                                                          int maxItems)
        {
            return items.Select((item, inx) => new { item, inx })
                        .GroupBy(x => x.inx / maxItems)
                        .Select(g => g.Select(x => x.item));
        }

        // Or IsNanOrInfinity
        public static bool HasValue(this double value)
        {
            return !Double.IsNaN(value) && !Double.IsInfinity(value);
        }

        public static long factorial(int n)
        {
            if (n == 0)
                return 1;

            return n * factorial(n - 1);
        }


        /// <summary>
        /// Used for colors in image processing
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static Point3d Mean(List<Point3d> points)
        {
            var nLength = points.Count;
            Point3d totalPre = new Point3d(0, 0, 0);
            double X = 0.0;
            double Y = 0.0;
            double Z = 0.0;
            for (int i = 0; i < nLength; i++)
            {
                X += points[i].X;
                Y += points[i].Y;
                Z += points[i].Z;
            }
            X /= nLength;
            Y /= nLength;
            Z /= nLength;
            Point3d mean = new Point3d(X, Y, Z);
            return mean;
        }

    }

}