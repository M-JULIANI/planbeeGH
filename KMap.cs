using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Data;
using Grasshopper;
using GH_IO;
using Rhino.Geometry;
using GH_IO.Serialization;
using System.Linq;
using Rhino.Display;
using System.Text.RegularExpressions;


namespace Planbee
{
    public class KMap
    {

        public int numNodes = 0;
        public Node[] nodes;
        public List<double> radius = new List<double>();
        public double learning;
        public List<double> radiusDecay = new List<double>();
        public double learningDecay;
        public double timeConstant;
        public double bmuMultiplier;
        public DataTree<double> progInputs = new DataTree<double>();
        public int maxIters = 0;
        public Input[] inputs;
        public DataTree<double> inputW;
        public int numWeights = 4;
        public double cRadius = double.NaN;

        public List<Point3d> nodePoints;
        public DataTree<double> nodeW;
        public int iter;
        public List<double> nodeWeights;
        public DataTree<double> _nodeData;

        public DataTree<Polyline> tree;
        public List<Polyline> drawingPolys;
        public List<string> m_pNames;
        public List<System.Drawing.Color> discreteCol;



        public KMap(DataTree<double> ProgramInputs, List<Point3d> NodePts, double resolution, DataTree<double> NodeData, int numW, double learn, List<double> radMultiplier, double BMUradMultiplier, int maxIterations) // BMU rad might want to be smaller than radMultiplier
        {
            m_pNames = new List<string>();
            this.numNodes = NodePts.Count;
            learning = learn;
            radius = radMultiplier;
            inputW = new DataTree<double>();
            progInputs = ProgramInputs;
            applyProgramInputs(radius.Count);

            bmuMultiplier = BMUradMultiplier;
            maxIters = maxIterations;
            
            cRadius = resolution / 2.0;
            
            nodeW = new DataTree<double>();
            nodeWeights = new List<double>();
            nodePoints = new List<Point3d>();
            nodePoints = NodePts;
            tree = new DataTree<Polyline>();
            drawingPolys = new List<Polyline>();
            _nodeData = new DataTree<double>();
            _nodeData = NodeData;
            discreteCol = new List<System.Drawing.Color>();
            iter = 0;

            

            nodes = new Node[numNodes];
            


            double average = 0.0;
            for (int i = 0; i < radius.Count; i++)
                average += radius[i];

            average /= radius.Count;

            for (int i = 0; i < numNodes; i++)
            {
                var pt = nodePoints[i];
                nodes[i] = new Node(pt, _nodeData.Branch(i));
            }
            timeConstant = maxIters / Math.Log(average);

            for (int i = 0; i < inputs.Length; i++)
                radiusDecay.Add(radius[i]);
            

        }

        public void applyProgramInputs(int numInputs)
        {
            inputs = new Input[numInputs];
            inputW.Clear();

            for (int i = 0; i < numInputs; i++)
            {
                inputs[i] = new Input(numWeights, i, progInputs);

                for (int j = 0; j < numWeights; j++)
                {
                    inputW.Add(inputs[i].w[j], new GH_Path(i));
                }
            }
        }

        //output func for visualization
        public void outputNodesXY()
        {
            nodePoints.Clear();

            for (int i = 0; i < numNodes; i++)
            {
                nodePoints.Add(new Point3d(nodes[i].x, nodes[i].y, 0));
            }
        }
        //output func for visualization
        public void outputNodeWeights()
        {
            nodeW.Clear();

            for (int i = 0; i < numNodes; i++)
            {
                var len = nodes[i].w.Length;
                for (int j = 0; j < len; j++)
                {
                    nodeW.Add(nodes[i].w[j], new GH_Path(i));
                }

            }
        }

        //finding best matching unit for each input ('archetypal' program)
        public int BMU(Input input)
        {
            //
            int win = -1;
            double minDist = cRadius * bmuMultiplier;

            for (int i = 0; i < nodes.Length; i++)
            {
                double d = distance(nodes[i].w, input.w);

                if (d < minDist)
                {
                    minDist = d;
                    win = i;
                }
            }

            return win;
        }

        public void updateNodeWeights(int index, Input input, int winner)
        {
            for (int i = 0; i < numNodes; i++)
            {
                for (int j = 0; j < numWeights; j++)
                {
                    nodes[i].w[j] += learningDecay * BMUInfluence(index, winner, i) * (input.w[j] - nodes[i].w[j]);
                }
            }
        }

        //win index BMU
        //index index of node
        public double BMUInfluence(int _index, int win, int index)
        {
            double delta = new Point3d(nodes[win].x, nodes[win].y, 0).DistanceTo(new Point3d(nodes[index].x, nodes[index].y, 0));
            double f = Math.Exp((-1 * Math.Pow(delta, 2)) / (2 * Math.Pow(radiusDecay[_index], 2)));
            return f;
        }

        public void trainNodes()
        {
            learningDecay = learning * Math.Exp(-1 * iter / timeConstant);
            //radiusDecay = radius * Math.Exp(-1 * iter / timeConstant);

            for (int i = 0; i < inputs.Length; i++)
            {
                radiusDecay[i] = radius[i] * Math.Exp(-1 * iter / timeConstant);
            }

            for (int i = 0; i < inputs.Length; i++)
            {
                int winner = BMU(inputs[i]);
                updateNodeWeights(i, inputs[i], winner);

            }
        }

        public double distance(double[] x, double[] y)
        {
            double dist = 0;

            for (int i = 0; i < x.Length; i++)
            {
                dist += Math.Pow(x[i] - y[i], 2);
            }
            dist = Math.Sqrt(dist);

            return dist;

        }
        public string RemoveNumbersSymbols(string nameString)
        {
            var filter1 = Regex.Replace(nameString, @"[\d-]", string.Empty);
            var filter2 = Regex.Replace(filter1, "[^a-zA-Z0-9]", string.Empty);

            return filter2;
        }

        public void drawCircles()
        {
     
            for (int i = 0; i < nodePoints.Count; i++)
            {
                Plane plane = new Plane(nodePoints[i], Vector3d.ZAxis);
                Interval inter = new Interval(-cRadius, cRadius);
                var rect = new Rectangle3d(plane, inter, inter);
                //var circ = new Circle(nodePoints[i], cRadius);
                var poly = rect.ToPolyline();
                drawingPolys.Add(poly);
            }
 
        }
    }



    //class containing the location of nodes and their corresponding weights (vector)
    public class Node
    {
        public double[] w;
        public double x;
        public double y;

        public Node(Point3d point, List<double> assignedWeights)
        {
            x = point.X;
            y = point.Y;
            w = new double[assignedWeights.Count];

            for (int i = 0; i < assignedWeights.Count; i++)
            {
                w[i] = assignedWeights[i];
            }
        }
    }

    //container class containing 'archetypical' features of a program
    public class Input
    {
        public double[] w;

        //    public Input(int num)
        //    {
        //      w = new double[num];
        //      for (int i = 0; i < num; i++)
        //        w[i] = rnd.NextDouble();
        //    }
        //extracting the program data (6 programs) and assigning weights per the data
        public Input(int num, int branch, DataTree<double> _pData)
        {
            w = new double[num];
            for (int j = 0; j < num; j++)
            {
                var b = _pData.Branch(branch);
                w[j] = b[j];
            }

        }
    }

    public class sInterval
    {
        public Interval interval;
        public double midInterval;
        public int index;
        public double cumulArea;

        public sInterval(double T0, double T1, int index)
        {
            this.interval = new Interval(T0, T1);
            this.index = index;
            midInterval = (T1 + T0) * 0.5;
        }
    }

    public class sNode
    {
        public Point3d pos;
        public double wVal;
        public int intervalIndex;
        public double multiplierStrength;
        public string name;
        public double area;
        public bool bmu;

        public sNode(Point3d pos, double wVal, double sideLength)
        {
            this.pos = pos;
            this.wVal = wVal;
            this.area = sideLength * sideLength;
            this.bmu = false;
        }
    }

    public class Remapper
    {
        public List<double> weightedVals; //value for each point
        public List<string> programNames; // 7 program names
        public List<double> programAreas;
        public List<Point3d> locations; //each point
        public int intervalCount;
        public sInterval[] smIntervals;
        public List<sNode> nodes = new List<sNode>();
        public List<ProgramAreaPairs> programPairs;
        public DataTree<sNode> programTree;
        public double _sideLength = double.NaN;
        public double[] progA;
        public double _area = double.NaN;
        public bool optimizeArea;

        public Remapper(List<double> weightedVals, List<Point3d> locations, List<string> programNames, List<double> programAreas, double sideLength, bool areaOpt)
        {
            optimizeArea = areaOpt;
            this._sideLength = sideLength;
            this._area = _sideLength * _sideLength;
            this.weightedVals = weightedVals;
            this.locations = locations;
            this.programNames = programNames;
            this.programAreas = programAreas;
            programTree = new DataTree<sNode>();
            intervalCount = programNames.Count;
            smIntervals = new sInterval[intervalCount];
            progA = new double[programAreas.Count];
            programPairs = new List<ProgramAreaPairs>();
            InitIntervals();
            InitProgramPairs();
            InstantiateNodes();
            MatchToProgram();
        }

        public void InitIntervals()
        {
            var first = weightedVals.OrderBy(v => v).Take(1).ToList();
            var last = weightedVals.OrderByDescending(v => v).Take(1).ToList();

            var divs = intervalCount;
            var range = last[0] - first[0];
            var increment = range / divs;

            for (int i = 0; i < smIntervals.Length; i++)
            {
                var bottom = first[0] + (increment * i);
                var top = first[0] + (increment * i) + increment;
                smIntervals[i] = new sInterval(bottom, top, i);
            }
        }

        public void InitProgramPairs()
        {
            for (int i = 0; i < programNames.Count; i++)
            {
                programPairs.Add(new ProgramAreaPairs(programNames[i], programAreas[i]));
                progA[i] = 0.0;

            }
        }

        public void InstantiateNodes()
        {
            for (int i = 0; i < weightedVals.Count; i++)
            {
                nodes.Add(new sNode(locations[i], weightedVals[i], _sideLength));
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                List<IntervalSorter> intSortList = new List<IntervalSorter>();

                for (int j = 0; j < smIntervals.Length; j++)
                {
                    var dist = Math.Abs(nodes[i].wVal - smIntervals[j].midInterval);
                    intSortList.Add(new IntervalSorter(j, dist));
                }

                var sorted = intSortList.OrderBy(n => n.dist).ToList();

                IntervalSorter best = null;

                if(optimizeArea)
                {
                    int location = 0;
                    bool found = false;

                    best = sorted[location];

                    while (found == false)
                    {
                        if (location == 6)
                        {
                            var tempList = new List<IntervalSorter>();
                            for (int f = 0; f < 7; f++)
                            {
                                var delta = programAreas[f] - progA[f];

                                if (delta > 0)
                                {
                                    var iSort = new IntervalSorter(f, delta);
                                    tempList.Add(iSort);
                                }
                            }

                            var programNeeded = tempList.OrderByDescending(v => v.dist).ToList();
                            var bProgram = programNeeded[0].p_interval;
                            best = sorted[bProgram];
                            progA[best.p_interval] += _area;
                            found = true;
                        }
                        else
                        {
                            if ((programAreas[best.p_interval] - _area) >= progA[best.p_interval])
                            {
                                best = sorted[location];
                                progA[best.p_interval] += _area;
                                found = true;
                            }
                            else
                            {
                                location++;
                                best = sorted[location];
                            }
                        }
                    }
                }
                else
                    best = sorted[0];

                nodes[i].intervalIndex = best.p_interval;
                nodes[i].multiplierStrength = 1.0 - best.dist * 10.0;
            }
        }

        public void MatchToProgram()
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = 0; j < smIntervals.Length; j++)
                {
                    if (nodes[i].intervalIndex == j)
                    {
                        smIntervals[j].cumulArea += nodes[i].area;
                    }

                }
            }

            var sortedIntervals = smIntervals.OrderBy(i => i.cumulArea).ToList();
            var sortedProgram = programPairs.OrderBy(p => p.area).ToList();

            for (int i = 0; i < nodes.Count; i++)
            {

                for (int j = 0; j < sortedIntervals.Count; j++)

                    if (nodes[i].intervalIndex == j)
                        nodes[i].name = sortedProgram[j].name;
            }

            var sortedNodes = nodes.OrderBy(n => n.multiplierStrength).ToList();

            for (int i = 0; i < sortedIntervals.Count; i++)
            {
                int count = 0;

                for (int j = 0; j < sortedNodes.Count; j++)
                {
                    if (i == sortedNodes[j].intervalIndex)
                    { //if(i == 0)
                      // sortedNodes[j].bmu = true;

                        sortedNodes[j].name += "_" + count;
                        count++;
                    }
                }

            }

            for (int i = 0; i < nodes.Count; i++)
            {

                for (int j = 0; j < sortedIntervals.Count; j++)
                {

                    if (nodes[i].intervalIndex == j)
                    {
                        programTree.Add(nodes[i], new GH_Path(j));
                    }
                }
            }
        }

    }

    public class IntervalSorter
    {
        public int p_interval;
        public double dist;

        public IntervalSorter(int p_interval, double dist)
        {
            this.p_interval = p_interval;
            this.dist = dist;
        }
    }

    public class ProgramAreaPairs
    {
        public string name;
        public double area;

        public ProgramAreaPairs(string name, double area)
        {
            this.name = name;
            this.area = area;
        }
    }

}