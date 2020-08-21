using System;
using System.Collections.Generic;
using Rhino.Display;

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
    public static class PBUtilities
    {
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
    }

}