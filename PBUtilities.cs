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

        //public static long FactorialR(int inputVal)
        //{
        //    if (inputVal == 1)
        //        return inputVal;
        //    else
        //        return inputVal * FactorialR(inputVal - 1);
        //}

        //public static int FactorialFor(int inputVal)
        //{
        //    int factorial = inputVal;
        //    for (int i = inputVal - 1; i >= 1; i--)
        //    {
        //        factorial = factorial * i;
        //    }

        //    return factorial;
        //}

        public static long factorial(int n)
        {
            if (n == 0)
                return 1;

            return n * factorial(n - 1);
        }
    }

}