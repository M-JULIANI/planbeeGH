using System;
using System.Collections.Generic;
using System.Linq;

using System.Drawing;

using Rhino.Geometry;
using Grasshopper;
using Grasshopper.Kernel.Data;

namespace PlanBee
{

    #region ImageProcessing Classes
    public class ImageProcessor
    {
            public double ptSpacing;
            public Bitmap imageBit;
            public List<string> clrList;
            public DataTree<Point3d> _areaPoints;
            public DataTree<Point3d> _ProcessedPts;
            public DataTree<Point3d> _ProcessedPts2;
            public Point3d[,] _vizPts;
            public Point3d[] _progCentroids;

            public string[] names;
            public System.Drawing.Color[,] colors;
            public ColorVal[] sortedMatrixValue;
            public string[,] finalS;
            public SmartCell[] SmartCellMat;
            public List<string> sss;

            public SortedDictionary<Vector2d, Point3d> initPointDict;

            public ImageProcessor(string bitmapString, List<string> colorList)
            {
                imageBit = new Bitmap(bitmapString);
                clrList = colorList;
                _areaPoints = new DataTree<Point3d>();
                _vizPts = new Point3d[clrList.Count, clrList.Count];
                _progCentroids = new Point3d[clrList.Count]; //derived program centroid from image
                SmartCellMat = new SmartCell[(clrList.Count) * (clrList.Count)];
                CollectPtsFromImage();
                //CalculateMatrices();
                List<string> s;
                GetPointBoundaries(out s);
                sss = s;
            }

            public List<Point3d> ExtractPerimeterPts(List<Point3d> points)
            {
                initPointDict = new SortedDictionary<Vector2d, Point3d>();
                SortedDictionary<Vector2d, Point3d> boundaryDict = new SortedDictionary<Vector2d, Point3d>();

                SortedDictionary<Vector2d, Point3d> finalboundaryDict = new SortedDictionary<Vector2d, Point3d>();

                for (int i = 0; i < points.Count; i++)
                {
                    Vector2d index = new Vector2d(points[i].X, points[i].Y);
                    initPointDict.Add(index, points[i]);
                }

                var initKeys = initPointDict.Keys.ToList();

                for (int i = 0; i < initKeys.Count; i++)
                {
                    if (LegalPt1(initKeys[i], initPointDict))
                    {
                        Point3d pt;
                        if (initPointDict.TryGetValue(initKeys[i], out pt))
                            boundaryDict.Add(initKeys[i], pt);
                    }
                }

                var secondKeys = boundaryDict.Keys.ToList();

                for (int i = 0; i < secondKeys.Count; i++)
                {
                    if (LegalPt2(secondKeys[i], boundaryDict))
                    {
                        Point3d pt;
                        if (boundaryDict.TryGetValue(secondKeys[i], out pt))
                            finalboundaryDict.Add(secondKeys[i], pt);
                    }
                }

                List<Point3d> allPts = boundaryDict.Values.ToList();

                return allPts;
            }

            public bool LegalPt1(Vector2d index, SortedDictionary<Vector2d, Point3d> dict)
            {
                bool isLegal = false;

                var neighbors = new List<Point3d>();

                for (int yi = -1; yi <= 1; yi++)
                {
                    int y = (int)(yi + index.Y);

                    for (int xi = -1; xi <= 1; xi++)
                    {
                        int x = (int)(xi + index.X);

                        var i = new Vector2d(x, y);

                        if (index == i) continue;

                        Point3d point;

                        if (dict.TryGetValue(i, out point))
                        {
                            if (Math.Abs(xi) + Math.Abs(yi) == 1)

                                neighbors.Add(point);
                        }
                    }
                }

                if (neighbors.Count > 1 && neighbors.Count < 4)
                    isLegal = true;

                return isLegal;

            }

            public bool LegalPt2(Vector2d index, SortedDictionary<Vector2d, Point3d> dict)
            {
                bool isLegal = false;

                var neighbors = new List<Point3d>();

                for (int yi = -1; yi <= 1; yi++)
                {
                    int y = (int)(yi + index.Y);

                    for (int xi = -1; xi <= 1; xi++)
                    {
                        int x = (int)(xi + index.X);

                        var i = new Vector2d(x, y);

                        if (index == i) continue;

                        Point3d point;

                        if (dict.TryGetValue(i, out point))
                        {
                            if (xi * yi == 0)

                                neighbors.Add(point);
                        }
                    }
                }

                if (neighbors.Count > 1 && neighbors.Count < 4)
                    isLegal = true;

                return isLegal;

            }



            public void GetPointBoundaries(out List<string> length)
            {
                _ProcessedPts = new DataTree<Point3d>();

                length = new List<string>();

                for (int i = 0; i < _areaPoints.BranchCount; i++)
                {
                    var outPts = ExtractPerimeterPts(_areaPoints.Branch(i));
                    _ProcessedPts.AddRange(outPts, new GH_Path(i));
                    length.Add(outPts.Count.ToString());
                }
            }

        public void CollectPtsFromImage()
            {

                var width = imageBit.Width;
                var height = imageBit.Height;


                int[,] indices = new int[width, height];
                for (int j = 0; j < width; j++)
                    for (int k = 0; k < height; k++)
                    {
                        int index = -1;
                        Color p = imageBit.GetPixel(j, k);
                        int actR = p.R;
                        int actG = p.G;
                        int actB = p.B;

                        {
                            double min = 55;
                            //double threshold = 10.0;

                            for (int i = 0; i < clrList.Count; i++)
                            {
                                string[] colString = clrList[i].Split(',');
                                int R = Int32.Parse(colString[0]);
                                int G = Int32.Parse(colString[1]);
                                int B = Int32.Parse(colString[2]);

                                var diff = Math.Pow(Math.Abs(R - actR), 1) + Math.Pow(Math.Abs(G - actG), 1) + Math.Pow(Math.Abs(B - actB), 1);

                                var diffR = Math.Abs(R - actR);
                                var diffG = Math.Abs(G - actG);
                                var diffB = Math.Abs(B - actB);

                                //if(diffR<=threshold && diffG<=threshold && diffB<=threshold)

                                if (diff <= min)
                                {
                                    index = i;
                                    indices[j, k] = index;
                                    min = diff;
                                }
                                else
                                    indices[j, k] = index;

                                //if(i == clrList.Count - 1)
                                // index = -1;
                            }
                        }


                    }
                /////////////////////////

                List<Point3d>[] areaPts = new List<Point3d>[clrList.Count]; //pixels corresponding to a program
                for (int i = 0; i < clrList.Count; i++)
                {
                    areaPts[i] = new List<Point3d>();
                    for (int j = 0; j < width; j++)
                        for (int k = 0; k < height; k++)
                        {
                            var index = indices[j, k];

                            if (index == i)
                            { ///if pixelColor == program color
                                areaPts[i].Add(new Point3d(j, -k + height, 0));
                                // Print(index.ToString());
                            }
                            else continue;
                        }
                    var m = PBUtilities.Mean(areaPts[i]);
                    _progCentroids[i] = m;
                }

                //////////////////////////

                for (int i = 0; i < clrList.Count; i++)
                    _areaPoints.AddRange(areaPts[i], new GH_Path(i));


                for (int j = 0; j < _progCentroids.Length; j++)//7 pts
                {
                    for (int k = 0; k < _progCentroids.Length; k++)
                    {
                        _vizPts[j, k] = new Point3d(j, -k, 0);
                    }
                }
            }

        }
    

    public class ColorVal
    {
        public int red;
        public int green;
        public int blue;
        public double val;
        //public string label;

        public ColorVal(double red, double green, double blue, double val)
        {
            this.red = (int)red;
            this.green = (int)green;
            this.blue = (int)blue;
            this.val = val;
        }
    }

    public class SmartPixel
    {
        public double r;
        public double g;
        public double b;
        public double sum = 0;

        public SmartPixel(double metric1, double metric2, double metric3)
        {
            this.r = metric1;
            this.g = metric2;
            this.b = metric3;
            sum = this.r + this.g + this.b;
        }

    }

    #endregion



    #region Boundary Extrracting Classes
    public class BoundaryExtractor
    {
        public DataTree<Point3d> pointTree;
        //public List<Point3d> centroids;

        public List<int> tempPt = new List<int>();
        public List<int> origPt = new List<int>();
        public DataTree<Point3d> outTree;
        public SortedDictionary<Vector2d, smPoint> mainDict;

        public BoundaryExtractor(DataTree<Point3d> pTree, List<Point3d> centList)
        {
            pointTree = pTree;
            outTree = new DataTree<Point3d>();
            // centroids = new List<Point3d>();
            for (int i = 0; i < pointTree.BranchCount; i++)
            {
                BranchWork(pointTree.Branch(i), i, centList[i]);
            }
        }

        public void BranchWork(List<Point3d> pointList, int branchIndex, Point3d centroid)
        {
            mainDict = new SortedDictionary<Vector2d, smPoint>();

            Vector2d indexTemp;
            for (int i = 0; i < pointList.Count; i++)
            {
                indexTemp = new Vector2d(pointList[i].X, pointList[i].Y);
                smPoint pt;
                if (mainDict.TryGetValue(indexTemp, out pt))
                    continue;
                else
                    mainDict.Add(indexTemp, new smPoint(pointList[i]));
            }

            var cent = centroid;
            //centroids.Add(cent);
            var holdPts = mainDict.Values.OrderBy(s => s.pt.DistanceTo(cent)).Select(s => s.pt).ToList();

            List<Point3d> ptStore = new List<Point3d>();
            ptStore.AddRange(holdPts);

            origPt.Add(ptStore.Count);

            int sCount = 0;

            List<Point3d> branchPts;
            while (ptStore.Count > 0)
            {
                branchPts = TraverseDict(ptStore[0], mainDict);

                if (branchPts != null && branchPts.Count > 0)
                {
                    if (branchPts.Count > 200)
                        outTree.AddRange(branchPts, new GH_Path(branchIndex, sCount));
                    for (int j = 0; j < branchPts.Count; j++)
                        ptStore.Remove(branchPts[j]);

                    tempPt.Add(ptStore.Count);

                    sCount++;
                }
                else
                    ptStore.Remove(ptStore[0]);
            }
        }

        public List<Point3d> TraverseDict(Point3d startPt, SortedDictionary<Vector2d, smPoint> dict)
        {
            var outPts = new List<Point3d>();

            Vector2d activeOne = new Vector2d(startPt.X, startPt.Y);

            bool stillGoin = true;

            while (stillGoin)
            {
                Vector2d next = Vector2d.Unset;

                int degree = 1;
                bool hasN = false;
                while (degree < 3)
                {
                    Vector2d holder = Vector2d.Unset;
                    hasN = GetActiveNeighbors(degree, activeOne, dict, out holder);
                    if (hasN)
                    {
                        next = holder;
                        goto nextSec;
                    }
                    degree++;
                }

            nextSec:
                if (hasN && next != Vector2d.Unset)
                {
                    smPoint sp;
                    if (dict.TryGetValue(next, out sp))
                    {
                        sp.isActive = false;
                        outPts.Add(sp.pt);
                        activeOne = next;
                    }
                }
                else
                    break;
            }

            return outPts;
        }

        public bool GetActiveNeighbors(int degree, Vector2d index, SortedDictionary<Vector2d, smPoint> dict, out Vector2d selected)
        {
            selected = Vector2d.Unset;

            var neighbors = new List<Point3d>();

            for (int yi = -degree; yi <= degree; yi++)
            {
                int y = (int)(yi + index.Y);

                for (int xi = -degree; xi <= degree; xi++)
                {
                    int x = (int)(xi + index.X);

                    var i = new Vector2d(x, y);

                    if (index == i) continue;

                    smPoint point;

                    if (dict.TryGetValue(i, out point))
                    {
                        if (point.isActive)
                        {
                            selected = i;
                            return true;
                        }
                    }
                }
            }

            return false;

        }

    }

    public class smPoint
    {
        public Point3d pt;
        public Vector2d index;
        public bool isActive { get; set; }

        public smPoint(Point3d pt)
        {
            this.pt = pt;
            index = new Vector2d(pt.X, pt.Y);
            isActive = true;
        }
    }

}
#endregion