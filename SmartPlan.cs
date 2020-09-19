using Grasshopper;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using QuickGraph;
using QuickGraph.Algorithms;

namespace PlanBee
{
    public class SmartPlan
    {
        public double _resolution;
        public Point3d minPoint;
        public Curve perimCurve;
        public Curve[] _coreCurves;
        public Mesh coreMesh;
        public Point3d[] pts;
        Dictionary<Vector2dInt, SmartCell> cells;
        public int divsX;
        public int divsY;
        public List<string> tShoot;
        public List<string> tShoot2;
        public Mesh attractorMeshJoined;
        public Mesh obstacleMeshJoined;
        public DataTree<Line> testLines;
        public SmartCell[] exitCells;
        public DataTree<Polyline> pathCurves;
        public Polyline[] isoPolylines;
        public Polyline[] isoNeighPolylines;
        public Vector3d[] isovistDirections;
        Curve[] _partCurves;

        public int projectUnits; //0 == metric, 1 == imperial
        Grid2d _grid;
        public UndirectedGraph<SmartCell, TaggedEdge<SmartCell, Face>> _graph;
        public IEnumerable<TaggedEdge<SmartCell, Face>> graphEdges;
        public IEnumerable<double> edgeLengths;

        public Curve[] attrCrvs;
        public Curve[] obstCrvs;
        Vector3d[] _SunVecs;

        public Mesh interiorPartitionMesh;

        Mesh meshCore;
        Mesh meshOutline;

        public Brep[] SolarObstacles;

        Plane _plane;
        Transform project;


        public SmartPlan(Curve perimCurve, double leaseSpan, double _resolution, List<Point3d> ExitPts, Plane plane)
        {
            _plane = new Plane(plane.Origin, Vector3d.ZAxis);
            project = Transform.PlanarProjection(_plane);

            this._resolution = _resolution;
            exitCells = new SmartCell[ExitPts.Count];

            this.perimCurve = perimCurve;
            _coreCurves = new Curve[1];
            _coreCurves[0] = perimCurve.Offset(Plane.WorldXY, -leaseSpan, 0.01, CurveOffsetCornerStyle.Chamfer)[0];

            cells = new Dictionary<Vector2dInt, SmartCell>();
            PopulateCells();

        }

        //base constructor leaseSpan
        public SmartPlan(Curve perimCurve, double leaseSpan, double _resolution, Plane plane)
        {
            _plane = new Plane(plane.Origin, Vector3d.ZAxis);
            project = Transform.PlanarProjection(_plane);

            this._resolution = _resolution;
            exitCells = new SmartCell[0];

            this.perimCurve = perimCurve;

            _coreCurves = new Curve[1];
            _coreCurves[0] = perimCurve.Offset(Plane.WorldXY, -leaseSpan, 0.01, CurveOffsetCornerStyle.Chamfer)[0];

            cells = new Dictionary<Vector2dInt, SmartCell>();
            PopulateCells();
        }

        //exit paths
        public SmartPlan(Curve perimCurve, List<Curve> coreCurves, double _resolution, List<Point3d> ExitPts, Plane plane)
        {
            _plane = new Plane(plane.Origin, Vector3d.ZAxis);
            project = Transform.PlanarProjection(_plane);

            this._resolution = _resolution;
            exitCells = new SmartCell[ExitPts.Count];

            this.perimCurve = perimCurve;
            _coreCurves = new Curve[coreCurves.Count];
            for (int i = 0; i < _coreCurves.Length; i++)
                _coreCurves[i] = coreCurves[i];

            cells = new Dictionary<Vector2dInt, SmartCell>();
            PopulateCells();

        }


        //Single Space Analysis Grid
        public SmartPlan(Curve perimCurve, double _resolution, Plane plane)
        {
            _plane = new Plane(plane.Origin, Vector3d.ZAxis);
            project = Transform.PlanarProjection(_plane);

            this._resolution = _resolution;
            exitCells = new SmartCell[0];

            this.perimCurve = perimCurve;

            cells = new Dictionary<Vector2dInt, SmartCell>();
            PopulateCells();
        }

        //base constructor CoreCurve
        public SmartPlan(Curve perimCurve, List<Curve> coreCurves, double _resolution, Plane plane)
        {
            _plane = new Plane(plane.Origin, Vector3d.ZAxis);
            project = Transform.PlanarProjection(_plane);

            this._resolution = _resolution;
            exitCells = new SmartCell[0];

            this.perimCurve = perimCurve;
            _coreCurves = new Curve[coreCurves.Count];
            for (int i = 0; i < _coreCurves.Length; i++)
                _coreCurves[i] = coreCurves[i];

            cells = new Dictionary<Vector2dInt, SmartCell>();
            PopulateCells();
        }

        //isovist constructor
        public SmartPlan(Curve perimCurve, List<Curve> coreCurves, List<Rectangle3d> rectangles, List<Curve> _interiorPartitions, Plane plane)
        {
            _plane = new Plane(plane.Origin, Vector3d.ZAxis);
            project = Transform.PlanarProjection(_plane);

            this._resolution = Math.Sqrt(rectangles[0].Area);
            exitCells = new SmartCell[0];
            _partCurves = new Curve[_interiorPartitions.Count];
            for (int i = 0; i < _partCurves.Length; i++)
            {
                _interiorPartitions[i].Transform(project);
                _partCurves[i] = _interiorPartitions[i];
            }

            this.perimCurve = perimCurve;

            _coreCurves = new Curve[coreCurves.Count];
            for (int i = 0; i < _coreCurves.Length; i++)
                _coreCurves[i] = coreCurves[i];

            cells = new Dictionary<Vector2dInt, SmartCell>();

            for (int i = 0; i < rectangles.Count; i++)
            {
                var loc = PlaceLocation(rectangles[i]);
                var _cell = new SmartCell(loc, this._resolution);

                SmartCell cellExisting;
                if (cells.TryGetValue(_cell.index, out cellExisting))
                    continue;
                else
                    cells.Add(_cell.index, _cell);
            }

            int size = 75;
            isovistDirections = new Vector3d[size];

            for (int i = 0; i < isovistDirections.Length; i++)
            {
                var modSin = i * (Math.PI * 2.0) / size;
                var vec = new Vector3d(Math.Sin(modSin), Math.Cos(modSin), 0);
                isovistDirections[i] = vec;
            }

        }

        //c0re-less isovist constructor
        public SmartPlan(Curve perimCurve, List<Rectangle3d> rectangles, List<Curve> _interiorPartitions, Plane plane)
        {
            _plane = new Plane(plane.Origin, Vector3d.ZAxis);
            project = Transform.PlanarProjection(_plane);

            this._resolution = Math.Sqrt(rectangles[0].Area);
            exitCells = new SmartCell[0];
            _partCurves = new Curve[_interiorPartitions.Count];
            for (int i = 0; i < _partCurves.Length; i++)
            {
                _interiorPartitions[i].Transform(project);
                _partCurves[i] = _interiorPartitions[i];
            }

            this.perimCurve = perimCurve;

            cells = new Dictionary<Vector2dInt, SmartCell>();

            for (int i = 0; i < rectangles.Count; i++)
            {
                var loc = PlaceLocation(rectangles[i]);
                var _cell = new SmartCell(loc, this._resolution);

                SmartCell cellExisting;
                if (cells.TryGetValue(_cell.index, out cellExisting))
                    continue;
                else
                    cells.Add(_cell.index, _cell);
            }

            int size = 75;
            isovistDirections = new Vector3d[size];

            for (int i = 0; i < isovistDirections.Length; i++)
            {
                var modSin = i * (Math.PI * 2.0) / size;
                var vec = new Vector3d(Math.Sin(modSin), Math.Cos(modSin), 0);
                isovistDirections[i] = vec;
            }

        }

        //solar access constructor
        public SmartPlan(List<Rectangle3d> rectangles, List<Vector3d> SunVectors, List<Brep> obstacles, Plane plane)
        {
            _plane = new Plane(plane.Origin, Vector3d.ZAxis);
            project = Transform.PlanarProjection(_plane);

            this._resolution = Math.Sqrt(rectangles[0].Area);
            exitCells = new SmartCell[0];

            cells = new Dictionary<Vector2dInt, SmartCell>();

            for (int i = 0; i < rectangles.Count; i++)
            {
                var loc = PlaceLocation(rectangles[i]);
                var _cell = new SmartCell(loc, this._resolution);

                SmartCell cellExisting;
                if (cells.TryGetValue(_cell.index, out cellExisting))
                    continue;
                else
                    cells.Add(_cell.index, _cell);
            }

            _SunVecs = new Vector3d[SunVectors.Count];

            for (int i = 0; i < _SunVecs.Length; i++)
                _SunVecs[i] = SunVectors[i];

            SolarObstacles = new Brep[obstacles.Count];
            for (int i = 0; i < SolarObstacles.Length; i++)
                SolarObstacles[i] = obstacles[i];

        }

        //distance to perimeter constructor
        public SmartPlan(Curve perimCurve, List<Rectangle3d> rectangles, Plane plane)
        {
            _plane = new Plane(plane.Origin, Vector3d.ZAxis);
            project = Transform.PlanarProjection(_plane);

            this._resolution = Math.Sqrt(rectangles[0].Area);
            this.perimCurve = perimCurve;

            cells = new Dictionary<Vector2dInt, SmartCell>();

            for (int i = 0; i < rectangles.Count; i++)
            {
                var loc = PlaceLocation(rectangles[i]);
                var _cell = new SmartCell(loc, this._resolution);
                SmartCell cellExisting;
                if (cells.TryGetValue(_cell.index, out cellExisting))
                    continue;
                else
                    cells.Add(_cell.index, _cell);
            }

        }


        //natural light
        //views to attractors
        //exit access
        //isovist

        //MSP constructor quickgraph

        public SmartPlan(List<Rectangle3d> rectangles, List<Curve> partitions, Plane plane, string name)
        {
            _plane = new Plane(plane.Origin, Vector3d.ZAxis);
            project = Transform.PlanarProjection(_plane);

            this._resolution = Math.Sqrt(rectangles[0].Area);
            interiorPartitionMesh = new Mesh();

            Transform mov = Transform.Translation(-0.5 * Vector3d.ZAxis);

            for (int i = 0; i < partitions.Count; i++)
            {
                var extrLocal = Extrusion.CreateExtrusion(partitions[i], Vector3d.ZAxis);
                extrLocal.Transform(mov);
                var meshLocal = Mesh.CreateFromSurface(extrLocal);
                interiorPartitionMesh.Append(meshLocal);
            }

            cells = new Dictionary<Vector2dInt, SmartCell>();

            _grid = new Grid2d(_resolution);

            for (int i = 0; i < rectangles.Count; i++)
            {
                var loc = PlaceLocation(rectangles[i]);
                var _cell = new SmartCell(loc, this._resolution);

                SmartCell cellExisting;
                if (cells.TryGetValue(_cell.index, out cellExisting))
                    continue;
                else
                {
                    var pt = new Point3d(_cell.location.X, _cell.location.Y, 0);
                    var meshPt = interiorPartitionMesh.ClosestMeshPoint(pt, 1000000.0);
                    var output = pt.DistanceTo(meshPt.Point).ToString();
                    if (pt.DistanceTo(meshPt.Point) > _resolution * 0.75)
                    {
                        cells.Add(_cell.index, _cell);
                        if (_grid.Voxels.TryGetValue(_cell.index, out var end))
                            continue;
                        else
                            _grid.AddVoxel(loc, _cell.index, this._resolution);
                    }
                }
            }

            //AssignInactiveCells(_grid);

            InitGraph();
        }

        //MSP constructor
        public SmartPlan(List<Rectangle3d> rectangles, List<Curve> partitions, Plane plane)
        {
            _plane = new Plane(plane.Origin, Vector3d.ZAxis);
            project = Transform.PlanarProjection(_plane);

            this._resolution = Math.Sqrt(rectangles[0].Area);
            interiorPartitionMesh = new Mesh();

            Transform mov = Transform.Translation(-0.5 * Vector3d.ZAxis);

            for (int i = 0; i < partitions.Count; i++)
            {
                var extrLocal = Extrusion.CreateExtrusion(partitions[i], Vector3d.ZAxis);
                extrLocal.Transform(mov);
                var meshLocal = Mesh.CreateFromSurface(extrLocal);
                interiorPartitionMesh.Append(meshLocal);
            }

            cells = new Dictionary<Vector2dInt, SmartCell>();

            for (int i = 0; i < rectangles.Count; i++)
            {
                var loc = PlaceLocation(rectangles[i]);
                var _cell = new SmartCell(loc, this._resolution);
                SmartCell cellExisting;
                if (cells.TryGetValue(_cell.index, out cellExisting))
                    continue;
                else
                    cells.Add(_cell.index, _cell);
            }

            AssignInactiveCells();
        }

        //simple shortest path
        public SmartPlan(List<Rectangle3d> rectangles, List<Point3d> exitPoints, Plane plane)
        {
            _plane = new Plane(plane.Origin, Vector3d.ZAxis);
            project = Transform.PlanarProjection(_plane);

            this._resolution = Math.Sqrt(rectangles[0].Area);
            exitCells = new SmartCell[exitPoints.Count];

            cells = new Dictionary<Vector2dInt, SmartCell>();

            for (int i = 0; i < rectangles.Count; i++)
            {
                var loc = PlaceLocation(rectangles[i]);
                var _cell = new SmartCell(loc, this._resolution);
                SmartCell cellExisting;
                if (cells.TryGetValue(_cell.index, out cellExisting))
                    continue;
                else
                    cells.Add(_cell.index, _cell);
            }

            AssignExitCells(exitPoints);
            // AssignInactiveCells();
        }

        //shortest path constructor
        public SmartPlan(List<Rectangle3d> rectangles, List<Curve> partitions, List<Point3d> exitPoints, Plane plane)
        {
            _plane = new Plane(plane.Origin, Vector3d.ZAxis);
            project = Transform.PlanarProjection(_plane);

            this._resolution = Math.Sqrt(rectangles[0].Area);
            exitCells = new SmartCell[exitPoints.Count];

            interiorPartitionMesh = new Mesh();

            Transform mov = Transform.Translation(-0.5 * Vector3d.ZAxis);

            for (int i = 0; i < partitions.Count; i++)
            {
                var extrLocal = Extrusion.CreateExtrusion(partitions[i], Vector3d.ZAxis);
                extrLocal.Transform(mov);
                var meshLocal = Mesh.CreateFromSurface(extrLocal);
                interiorPartitionMesh.Append(meshLocal);
            }

            cells = new Dictionary<Vector2dInt, SmartCell>();

            for (int i = 0; i < rectangles.Count; i++)
            {
                var loc = PlaceLocation(rectangles[i]);
                var _cell = new SmartCell(loc, this._resolution);
                SmartCell cellExisting;
                if (cells.TryGetValue(_cell.index, out cellExisting))
                    continue;
                else
                    cells.Add(_cell.index, _cell);
            }

            AssignExitCells(exitPoints);
            AssignInactiveCells();
        }



        //attractor viz constructor
        public SmartPlan(Curve perimCurve, List<Curve> coreCurves, List<Rectangle3d> rectangles, List<Curve> interiorPartitions, List<Curve> attractorCrvs, List<Curve> obstacleCrvs, Plane plane)
        {
            _plane = new Plane(plane.Origin, Vector3d.ZAxis);
            project = Transform.PlanarProjection(_plane);

            this._resolution = Math.Sqrt(rectangles[0].Area);
            exitCells = new SmartCell[0];
            attrCrvs = new Curve[attractorCrvs.Count];
            var obstacles = new List<Curve>();
            obstacles.AddRange(interiorPartitions);
            obstacles.AddRange(obstacleCrvs);
            obstCrvs = new Curve[obstacles.Count];


            for (int i = 0; i < attrCrvs.Length; i++)
            {
                attractorCrvs[i].Transform(project);
                attrCrvs[i] = attractorCrvs[i];
            }

            for (int i = 0; i < obstCrvs.Length; i++)
            {
                obstacles[i].Transform(project);
                obstCrvs[i] = obstacles[i];
            }

            this.perimCurve = perimCurve;

            if (coreCurves.Count > 0)
            {
                _coreCurves = new Curve[coreCurves.Count];
                for (int i = 0; i < _coreCurves.Length; i++)
                    _coreCurves[i] = coreCurves[i];
            }

            cells = new Dictionary<Vector2dInt, SmartCell>();

            for (int i = 0; i < rectangles.Count; i++)
            {
                var loc = PlaceLocation(rectangles[i]);
                var _cell = new SmartCell(loc, this._resolution);
                SmartCell cellExisting;
                if (cells.TryGetValue(_cell.index, out cellExisting))
                    continue;
                else
                    cells.Add(_cell.index, _cell);
            }

            int size = 75;
            isovistDirections = new Vector3d[size];

            for (int i = 0; i < isovistDirections.Length; i++)
            {
                var modSin = i * (Math.PI * 2.0) / size;
                var vec = new Vector3d(Math.Sin(modSin), Math.Cos(modSin), 0);
                isovistDirections[i] = vec;
            }
        }

        public List<Rectangle3d> getCells()
        {
            var rects = new List<Rectangle3d>();

            foreach (KeyValuePair<Vector2dInt, SmartCell> _cell in cells)
            {
                rects.Add(_cell.Value.rect);
            }
            return rects;

        }

        public double[] getExitMetric()
        {
            {
                var att = new double[cells.Count];
                int count = 0;
                foreach (KeyValuePair<Vector2dInt, SmartCell> _cell in cells)
                {
                    att[count] = _cell.Value.metric4;
                    count++;
                }
                return att;
            }
        }

        public double[] getMSP()
        {
            var att = new double[cells.Count];
            int count = 0;
            foreach (KeyValuePair<Vector2dInt, SmartCell> _cell in cells)
            {
                att[count] = _cell.Value.metric5;
                count++;
            }
            return att;
        }

        public int[] getNeighborhoodSizeRaw()
        {
            var att = new int[cells.Count];
            int count = 0;
            foreach (KeyValuePair<Vector2dInt, SmartCell> _cell in cells)
            {
                att[count] = (int)Math.Round(_cell.Value.neighSizeRaw);
                count++;
            }
            return att;
        }
        public double[] getNeighborhoodSize()
        {
            var att = new double[cells.Count];
            int count = 0;
            foreach (KeyValuePair<Vector2dInt, SmartCell> _cell in cells)
            {
                att[count] = _cell.Value.neighSize;
                count++;
            }
            return att;
        }

        public double[] getMSPRaw()
        {
            var att = new double[cells.Count];
            int count = 0;
            foreach (KeyValuePair<Vector2dInt, SmartCell> _cell in cells)
            {
                att[count] = _cell.Value.mspRaw;
                count++;
            }
            return att;
        }


        public double[] getIsovist()
        {
            double[] isos = new double[cells.Count];
            int count = 0;
            foreach (KeyValuePair<Vector2dInt, SmartCell> _cell in cells)
            {
                isos[count] = _cell.Value.metric1;
                count++;
            }
            return isos;
        }

        public double[] getPerimDistances()
        {
            var perim = new double[cells.Count];
            int count = 0;
            foreach (KeyValuePair<Vector2dInt, SmartCell> _cell in cells)
            {
                perim[count] = _cell.Value.metric2;
                count++;
            }

            return perim;
        }

        public double[] getAttractionMetric()
        {
            var att = new double[cells.Count];
            int count = 0;
            foreach (KeyValuePair<Vector2dInt, SmartCell> _cell in cells)
            {
                att[count] = _cell.Value.metric3;
                count++;
            }
            return att;
        }

        public double[] getSolarMetric()
        {
            var solar = new double[cells.Count];
            int count = 0;
            foreach (KeyValuePair<Vector2dInt, SmartCell> _cell in cells)
            {
                solar[count] = _cell.Value.metric2;
                count++;
            }
            return solar;
        }

        public void PopulateCells()
        {
            pts = PBUtilities.getDiscontinuities(this.perimCurve);
            GridGen();

        }

        public void GridGen()
        {
            tShoot = new List<string>();
            tShoot2 = new List<string>();
            int max = 80;
            int min = 5;
            BoundingBox bb = new BoundingBox(pts);
            var rangeX = bb.Max.X - bb.Min.X;
            var rangeY = bb.Max.Y - bb.Min.Y;

            minPoint = bb.Min;

            divsX = (int)(rangeX / this._resolution) + 1;
            divsY = (int)(rangeY / this._resolution) + 1;

            if (divsX > max)
            {
                divsX = max;
                this._resolution = rangeX / divsX * 1f;
            }

            if (divsY > max)
            {
                divsY = max;
                this._resolution = rangeY / divsY * 1f;
            }

            if (divsX < min)
            {
                divsX = min;
                this._resolution = rangeX / divsX * 1f;
            }

            if (divsY < min)
            {
                divsY = min;
                this._resolution = rangeY / divsY * 1f;
            }

            divsX = (int)(rangeX / this._resolution) + 1;
            divsY = (int)(rangeY / this._resolution) + 1;

            Point3d[,] initStorage = new Point3d[divsX, divsY];
            //var measuring = this.perimCurve.Offset(_plane, this._resolution*0.25, 0.01, CurveOffsetCornerStyle.Chamfer);
            //var measuring = this.perimCurve.Duplicate();

            for (int i = 0; i < divsX; i++)
                for (int j = 0; j < divsY; j++)
                {
                    var samplePt = new Point3d(i * this._resolution + minPoint.X, j * this._resolution + minPoint.Y, 0);
                    var roundedPt = new Vector2d((int)Math.Round(samplePt.X, 0, MidpointRounding.AwayFromZero),
                      (int)Math.Round(samplePt.Y, 0, MidpointRounding.AwayFromZero));
                    var loc = new Vector2d((samplePt.X), (samplePt.Y));
                    var index = new Vector2dInt((int)Math.Round(roundedPt.X / this._resolution), (int)Math.Round(roundedPt.Y / this._resolution));

                    if (this.perimCurve.Contains(samplePt, _plane, 0.00001) != Rhino.Geometry.PointContainment.Inside)
                        continue;

                    else
                    {
                        if (_coreCurves == null)
                        {
                            var _cell = new SmartCell(loc, this._resolution);
                            SmartCell cellExisting;
                            if (cells.TryGetValue(_cell.index, out cellExisting))
                                continue;
                            else
                                cells.Add(_cell.index, _cell);
                        }
                        else
                        {
                            if (InsideCrvsGroup(samplePt, _plane, _coreCurves) == true)
                                continue;
                            else
                            {
                                for (int c = 0; c < _coreCurves.Length; c++)
                                {
                                    if (_coreCurves[c].Contains(samplePt, _plane, 0.01) != Rhino.Geometry.PointContainment.Inside)
                                    {
                                        var _cell = new SmartCell(loc, this._resolution);
                                        SmartCell cellExisting;
                                        if (cells.TryGetValue(_cell.index, out cellExisting))
                                            continue;
                                        else
                                            cells.Add(_cell.index, _cell);
                                    }
                                }
                            }
                        }
                    }
                }
        }

        public bool InsideCrvsGroup(Point3d point, Plane plane, Curve[] curveArray)
        {
            bool invalid = false;
            for (int i = 0; i < curveArray.Length; i++)
                if (curveArray[i].Contains(point, _plane, 0.01) == Rhino.Geometry.PointContainment.Inside)
                    return true;
            return invalid;
        }

        public void ComputeCovid()
        {

        }

        public Vector3d[] InitCovidVecs()
        {
            Vector3d[] vecs = new Vector3d[8];
        }

        public void ComputeSolarAccess()
        {
            // Mesh combinedObs = new Mesh();
            obstacleMeshJoined = new Mesh();

            for (int i = 0; i < SolarObstacles.Length; i++)
            {
                MeshingParameters m = new MeshingParameters(0.1, 1);
                var localMesh = Mesh.CreateFromBrep(SolarObstacles[i], m);
                //combinedObs.Append(localMesh);
                obstacleMeshJoined.Append(localMesh);
            }

            var min = 100000.0;
            var max = -1.0;

            Ray3d ray;

            foreach (KeyValuePair<Vector2dInt, SmartCell> cell in cells)
            {
                int hitMiss = 0;
                Point3d dummyPt = new Point3d(cell.Value.location.X, cell.Value.location.Y, 0);

                for (int i = 0; i < _SunVecs.Length; i++)
                {

                    ray = new Ray3d(dummyPt, _SunVecs[i]);

                    if (Rhino.Geometry.Intersect.Intersection.MeshRay(obstacleMeshJoined, ray) < 0.0)
                        hitMiss++;

                }

                cell.Value.metric2 = hitMiss;
                if (hitMiss < min)
                    min = hitMiss;
                if (hitMiss > max)
                    max = hitMiss;

            }

            foreach (KeyValuePair<Vector2dInt, SmartCell> cell in cells)
            {
                var holder = PBUtilities.mapValue(cell.Value.metric2, min, max, 0.00, 1.00);
                cell.Value.metric2 = holder;
            }
        }


        public void ComputeNeighSize()
        {
            Transform mov = Transform.Translation(-0.5 * Vector3d.ZAxis);
            isoNeighPolylines = new Polyline[cells.Count];

            interiorPartitionMesh = new Mesh();
            meshCore = new Mesh();

            Extrusion[] extrusionCores;
            if (_coreCurves != null)
            {
                extrusionCores = new Extrusion[_coreCurves.Length];
                for (int i = 0; i < _coreCurves.Length; i++)
                {
                    var extr = Extrusion.CreateExtrusion(_coreCurves[i], Vector3d.ZAxis);// core extrusion
                    extr.Transform(mov);
                    meshCore.Append(Mesh.CreateFromSurface(extr));
                }
            }

            var curveOff = this.perimCurve.Offset(_plane, -this._resolution / 2.0, 0.0001, CurveOffsetCornerStyle.Sharp);
            var extrPerimeter = Extrusion.CreateExtrusion(this.perimCurve, Vector3d.ZAxis); // perimeter extrusion

            for (int i = 0; i < _partCurves.Length; i++)
            {
                var extrLocal = Extrusion.CreateExtrusion(_partCurves[i], Vector3d.ZAxis);
                extrLocal.Transform(mov);
                var meshLocal = Mesh.CreateFromSurface(extrLocal);
                interiorPartitionMesh.Append(meshLocal);
            }


            meshOutline = Mesh.CreateFromSurface(extrPerimeter);
            var min = 1000000.0;
            var max = -1.0;

            int count = 0;

            foreach (KeyValuePair<Vector2dInt, SmartCell> cell in cells)
            {
                var interSum = 0.0;
                Polyline poly = new Polyline();
                Point3d memPt = Point3d.Unset;
                Point3d dummyPt = new Point3d(cell.Value.location.X, cell.Value.location.Y, 0);

                if (this.perimCurve.Contains(dummyPt, _plane, 0.00001) == Rhino.Geometry.PointContainment.Outside)
                {
                    double t;
                    if (curveOff[0].ClosestPoint(dummyPt, out t))
                        dummyPt = curveOff[0].PointAt(t);
                }

                bool addPt;
                Ray3d ray;
                Point3d pt;
                double partialLength;

                for (int i = 0; i < isovistDirections.Length; i++)
                {
                    addPt = false;
                    ray = new Ray3d(dummyPt, isovistDirections[i]);

                    pt = Point3d.Unset;

                    var pointforComp = new List<Point3d>();

                    if (Rhino.Geometry.Intersect.Intersection.MeshRay(interiorPartitionMesh, ray) > 0.0)
                    {
                        pt = ray.PointAt(Rhino.Geometry.Intersect.Intersection.MeshRay(interiorPartitionMesh, ray));
                        addPt = true;
                        pointforComp.Add(pt);

                    }
                    if (Rhino.Geometry.Intersect.Intersection.MeshRay(meshCore, ray) > 0.0)
                    {
                        pt = ray.PointAt(Rhino.Geometry.Intersect.Intersection.MeshRay(meshCore, ray));
                        addPt = true;
                        pointforComp.Add(pt);
                    }

                    if (Rhino.Geometry.Intersect.Intersection.MeshRay(meshOutline, ray) > 0.0)
                    {
                        pt = ray.PointAt(Rhino.Geometry.Intersect.Intersection.MeshRay(meshOutline, ray));
                        addPt = true;
                        pointforComp.Add(pt);
                    }

                    else addPt = false;

                    Point3d best;
                    if (addPt)
                    {
                        best = pointforComp.OrderBy(p => p.DistanceTo(dummyPt)).ToList()[0];
                        partialLength = best.DistanceTo(dummyPt);
                        interSum += partialLength;
                        poly.Add(best);

                        if (i == 0)
                            memPt = new Point3d(best.X, best.Y, best.Z);
                    }
                }

                poly.Add(memPt);
                isoNeighPolylines[count] = poly;
                double area;
                Mesh mesh;
                if (poly.IsClosed)
                {
                    mesh = Mesh.CreateFromClosedPolyline(poly);
                    if (mesh != null)
                        area = AreaMassProperties.Compute(mesh).Area;
                    else
                        area = 0.0;
                }
                else
                    area = 0.0;

                double neighborhood = area / (this._resolution * this._resolution);//approximates the number of grid nodes 'cell' is connected to
                cell.Value.neighSizeRaw = neighborhood;

                if (neighborhood < min)
                    min = neighborhood;
                if (neighborhood > max)
                    max = neighborhood;

                count++;
            }

            foreach (KeyValuePair<Vector2dInt, SmartCell> cell in cells)
            {
                var holder = PBUtilities.mapValue(cell.Value.neighSizeRaw, min, max, 0.00, 1.00);
                cell.Value.neighSize = holder;
            }

        }
        public void ComputeIsovist()
        {
            Transform mov = Transform.Translation(-0.5 * Vector3d.ZAxis);
            isoPolylines = new Polyline[cells.Count];

            interiorPartitionMesh = new Mesh();
            meshCore = new Mesh();

            Extrusion[] extrusionCores;
            if (_coreCurves != null)
            {
                extrusionCores = new Extrusion[_coreCurves.Length];
                for (int i = 0; i < _coreCurves.Length; i++)
                {
                    var extr = Extrusion.CreateExtrusion(_coreCurves[i], Vector3d.ZAxis);// core extrusion
                    extr.Transform(mov);
                    meshCore.Append(Mesh.CreateFromSurface(extr));
                }
            }

            var curveOff = this.perimCurve.Offset(_plane, -this._resolution / 2.0, 0.0001, CurveOffsetCornerStyle.Sharp);
            var extrPerimeter = Extrusion.CreateExtrusion(this.perimCurve, Vector3d.ZAxis); // perimeter extrusion

            for (int i = 0; i < _partCurves.Length; i++)
            {
                var extrLocal = Extrusion.CreateExtrusion(_partCurves[i], Vector3d.ZAxis);
                extrLocal.Transform(mov);
                var meshLocal = Mesh.CreateFromSurface(extrLocal);
                interiorPartitionMesh.Append(meshLocal);
            }

            meshOutline = Mesh.CreateFromSurface(extrPerimeter);
            var min = 1000000.0;
            var max = -1.0;

            int count = 0;

            foreach (KeyValuePair<Vector2dInt, SmartCell> cell in cells)
            {
                var interSum = 0.0;
                Polyline poly = new Polyline();
                Point3d memPt = Point3d.Unset;
                Point3d dummyPt = new Point3d(cell.Value.location.X, cell.Value.location.Y, 0);

                if (this.perimCurve.Contains(dummyPt, _plane, 0.00001) == Rhino.Geometry.PointContainment.Outside)
                {
                    double t;
                    if (curveOff[0].ClosestPoint(dummyPt, out t))
                        dummyPt = curveOff[0].PointAt(t);
                }

                bool addPt;
                Ray3d ray;
                Point3d pt;
                double partialLength;

                for (int i = 0; i < isovistDirections.Length; i++)
                {
                    addPt = false;
                    ray = new Ray3d(dummyPt, isovistDirections[i]);
                    pt = Point3d.Unset;

                    var pointforComp = new List<Point3d>();

                    if (Rhino.Geometry.Intersect.Intersection.MeshRay(interiorPartitionMesh, ray) > 0.0)
                    {
                        pt = ray.PointAt(Rhino.Geometry.Intersect.Intersection.MeshRay(interiorPartitionMesh, ray));
                        addPt = true;
                        pointforComp.Add(pt);

                    }
                    if (Rhino.Geometry.Intersect.Intersection.MeshRay(meshCore, ray) > 0.0)
                    {
                        pt = ray.PointAt(Rhino.Geometry.Intersect.Intersection.MeshRay(meshCore, ray));
                        addPt = true;
                        pointforComp.Add(pt);
                    }

                    if (Rhino.Geometry.Intersect.Intersection.MeshRay(meshOutline, ray) > 0.0)
                    {
                        pt = ray.PointAt(Rhino.Geometry.Intersect.Intersection.MeshRay(meshOutline, ray));
                        addPt = true;
                        pointforComp.Add(pt);
                    }

                    else addPt = false;

                    Point3d best;
                    if (addPt)
                    {
                        best = pointforComp.OrderBy(p => p.DistanceTo(dummyPt)).ToList()[0];
                        partialLength = best.DistanceTo(dummyPt);
                        interSum += partialLength;
                        poly.Add(best);

                        if (i == 0)
                            memPt = new Point3d(best.X, best.Y, best.Z);
                    }
                }

                cell.Value.metric1 = interSum;
                if (interSum < min)
                    min = interSum;
                if (interSum > max)
                    max = interSum;

                poly.Add(memPt);
                isoPolylines[count] = poly;
                count++;
            }

            foreach (KeyValuePair<Vector2dInt, SmartCell> cell in cells)
            {
                var holder = PBUtilities.mapValue(cell.Value.metric1, min, max, 0.00, 1.00);
                cell.Value.metric1 = holder;
            }

        }

        public void ComputeDistToPerimeter()
        {
            var min = 1000.0;
            var max = 1.0;
            foreach (KeyValuePair<Vector2dInt, SmartCell> cell in cells)
            {
                var testPoint = new Point3d(cell.Value.location.X, cell.Value.location.Y, 0);
                double t;
                Point3d otherPt = new Point3d();
                if (perimCurve.ClosestPoint(testPoint, out t))
                {
                    otherPt = perimCurve.PointAt(t);
                    double dist = testPoint.DistanceTo(otherPt);
                    cell.Value.metric2 = dist;
                    if (dist < min)
                        min = dist;
                    if (dist > max)
                        max = dist;
                }
            }

            foreach (KeyValuePair<Vector2dInt, SmartCell> cell in cells)
            {
                var holder = PBUtilities.mapValue(cell.Value.metric2, min, max, 0.0, 1.0);
                var final = 1.0 - holder;
                cell.Value.metric2 = final;
            }

        }

        public void ComputeAttractionViz()
        {

            try
            {
                obstacleMeshJoined = new Mesh();
                attractorMeshJoined = new Mesh();

                testLines = new DataTree<Line>();

                Mesh[] obstMeshExtrusions;
                if (_coreCurves != null)
                    obstMeshExtrusions = new Mesh[obstCrvs.Length + _coreCurves.Length];
                else
                    obstMeshExtrusions = new Mesh[obstCrvs.Length];

                Mesh[] attrMeshExtrusions = new Mesh[attrCrvs.Length];

                Surface srfExtrusion;

                Transform mov = Transform.Translation(-0.5 * Vector3d.ZAxis);

                //funky logic beware!
                for (int i = 0; i < obstMeshExtrusions.Length; i++)
                {
                    if (i >= obstCrvs.Length)
                        srfExtrusion = Extrusion.CreateExtrusion(_coreCurves[i - obstCrvs.Length], Vector3d.ZAxis);
                    else
                        srfExtrusion = Extrusion.CreateExtrusion(obstCrvs[i], Vector3d.ZAxis);

                    srfExtrusion.Transform(mov);
                    obstMeshExtrusions[i] = Mesh.CreateFromSurface(srfExtrusion);
                    obstacleMeshJoined.Append(obstMeshExtrusions[i]);
                }

                for (int i = 0; i < attrMeshExtrusions.Length; i++)
                {
                    srfExtrusion = Extrusion.CreateExtrusion(attrCrvs[i], Vector3d.ZAxis);
                    srfExtrusion.Transform(mov);
                    attrMeshExtrusions[i] = Mesh.CreateFromSurface(srfExtrusion);
                    attractorMeshJoined.Append(attrMeshExtrusions[i]);
                }

                var min = 100000.0;
                var max = -1.0;
                int count = 0;

                foreach (KeyValuePair<Vector2dInt, SmartCell> cell in cells)
                {
                    Point3d dummyPt = new Point3d(cell.Value.location.X, cell.Value.location.Y, 0);
                    Vector3d dDir;
                    Ray3d ray;
                    Ray3d rayUseful;

                    int hits = 0;
                    double dist = 0.0;

                    for (int i = 0; i < isovistDirections.Length; i++)
                    {
                        dDir = isovistDirections[i];
                        ray = new Ray3d(dummyPt, dDir);
                        rayUseful = new Ray3d(dummyPt, dDir);


                        if (Rhino.Geometry.Intersect.Intersection.MeshRay(obstacleMeshJoined, ray) < 0.0)
                        {
                            var usefulRayT = Rhino.Geometry.Intersect.Intersection.MeshRay(attractorMeshJoined, rayUseful);
                            if (usefulRayT > 0.0)
                            {
                                var usefulPt = rayUseful.PointAt(usefulRayT);
                                var tempDist = usefulPt.DistanceTo(dummyPt);
                                dist += tempDist;
                                hits++;
                                testLines.Add(new Line(usefulPt, dummyPt), new GH_Path(count));
                            }
                        }
                    }
                    cell.Value.metric3 = dist;
                    if (dist < min)
                        min = dist;
                    if (dist > max)
                        max = dist;

                    count++;
                }

                foreach (KeyValuePair<Vector2dInt, SmartCell> cell in cells)
                {
                    var holder = PBUtilities.mapValue(cell.Value.metric3, min, max, 0.00, 1.00);
                    cell.Value.metric3 = holder;
                }
            }
            catch (Exception e)
            {
                e.ToString();
            }
        }

        public void ComputeExitAccess()
        {
            pathCurves = new DataTree<Polyline>();

            var min = 1000000.0;
            var max = -1.0;
            int count = 0;
            Polyline localPath;

            foreach (KeyValuePair<Vector2dInt, SmartCell> cell in cells)
            {
                double distance = 0.0;
                double minDist = 100000000;
                for (int j = 0; j < exitCells.Length; j++)
                {
                    localPath = new Polyline();
                    var steps = FindPath(cell.Value, exitCells[j]);

                    localPath.Add(new Point3d(cell.Value.location.X, cell.Value.location.Y, 0));
                    for (int i = 0; i < steps.Count; i++)
                    {
                        double dist = double.NaN;
                        if (i == 0)
                            dist = (cell.Value.location - steps[i].location).Length;
                        else
                            dist = (steps[i].location - steps[i - 1].location).Length;

                        distance += dist;

                        localPath.Add(new Point3d(steps[i].location.X, steps[i].location.Y, 0));
                        if (i == steps.Count - 1)
                            pathCurves.Add(localPath, new GH_Path(count));
                    }

                    if (distance < minDist)
                        minDist = distance;
                }

                cell.Value.metric4 = minDist;
                if (minDist < min)
                    min = minDist;
                if (minDist > max)
                    max = minDist;

                count++;
            }

            foreach (KeyValuePair<Vector2dInt, SmartCell> cell in cells)
            {
                var holder = PBUtilities.mapValue(cell.Value.metric4, min, max, 0.00, 1.00);
                var final = 1.0 - holder;
                cell.Value.metric4 = final;
            }
        }

        public void ComputeExitBetweenAccess()
        {
            pathCurves = new DataTree<Polyline>();

            var min = 1000000.0;
            var max = -1.0;
            int count = 0;
            Polyline localPath;

            List<Vector2dInt> allTheCells = new List<Vector2dInt>();

            foreach (KeyValuePair<Vector2dInt, SmartCell> cell in cells)
            {
                for (int j = 0; j < exitCells.Length; j++)
                {
                    localPath = new Polyline();
                    var steps = FindPath(cell.Value, exitCells[j]);
                    allTheCells.AddRange(steps.Select(s => s.index));

                    localPath.Add(new Point3d(cell.Value.location.X, cell.Value.location.Y, 0));
                    for (int i = 0; i < steps.Count; i++)
                    {
                        localPath.Add(new Point3d(steps[i].location.X, steps[i].location.Y, 0));
                        if (i == steps.Count - 1)
                            pathCurves.Add(localPath, new GH_Path(count));
                    }

                }
                count++;
            }

            foreach (KeyValuePair<Vector2dInt, SmartCell> c in cells)
                c.Value.metric4 = 0.0;

            foreach (KeyValuePair<Vector2dInt, SmartCell> c in cells)
            {
                for (int j = 0; j < allTheCells.Count; j++)
                    if (c.Key == allTheCells[j])
                        c.Value.metric4++;

                if (c.Value.metric4 < min)
                    min = 0;
                if (c.Value.metric4 > max)
                    max = c.Value.metric4;
            }

            foreach (KeyValuePair<Vector2dInt, SmartCell> cell in cells)
            {
                var holder = PBUtilities.mapValue(cell.Value.metric4, min, max, 0.00, 1.00);
                var final = 1.0 - holder;
                cell.Value.metric4 = final;
            }
        }

        public void ComputeBetweenPaths()
        {
            pathCurves = new DataTree<Polyline>();

            var min = 1000000.0;
            var max = -1.0;
            int count = 0;
            Polyline localPath;

            List<Vector2dInt> allTheCells = new List<Vector2dInt>();

            foreach (KeyValuePair<Vector2dInt, SmartCell> cell in cells)
            {
                double distance = 0.0;
                if (exitCells.Contains(cell.Value))
                {
                    for (int j = 0; j < exitCells.Length; j++)
                    {
                        if (cell.Value.index != exitCells[j].index)
                        {
                            localPath = new Polyline();
                            var steps = FindPath(cell.Value, exitCells[j]);
                            allTheCells.AddRange(steps.Select(s => s.index));

                            localPath.Add(new Point3d(cell.Value.location.X, cell.Value.location.Y, 0));
                            for (int i = 0; i < steps.Count; i++)
                            {
                                if (i == 0)
                                    distance += (cell.Value.location - steps[i].location).Length;

                                else
                                    distance += (steps[i].location - steps[i - 1].location).Length;

                                localPath.Add(new Point3d(steps[i].location.X, steps[i].location.Y, 0));
                                if (i == steps.Count - 1)
                                    pathCurves.Add(localPath, new GH_Path(count));
                            }
                        }
                    }
                }
            }

            foreach (KeyValuePair<Vector2dInt, SmartCell> c in cells)
                c.Value.metric4 = 0.0;

            foreach (KeyValuePair<Vector2dInt, SmartCell> c in cells)
            {
                for (int j = 0; j < allTheCells.Count; j++)
                    if (c.Key == allTheCells[j])
                        c.Value.metric4++;

                if (c.Value.metric4 < min)
                    min = c.Value.metric4;
                if (c.Value.metric4 > max)
                    max = c.Value.metric4;
            }



            foreach (KeyValuePair<Vector2dInt, SmartCell> cell in cells)
            {
                var holder = PBUtilities.mapValue(cell.Value.metric4, min, max, 0.00, 1.00);
                var final = 1.0 - holder;
                cell.Value.metric4 = final;
            }
        }


        public int GetShortestPath(SmartCell start, SmartCell endIn, UndirectedGraph<SmartCell, TaggedEdge<SmartCell, Face>> graph, out List<Vector2dInt> indices)
        {
            int stepCount = 0;
            var vox = this._grid.GetVoxels();
            indices = new List<Vector2dInt>();

            if (_grid.Voxels.TryGetValue(endIn.index, out var end))
            {
                var shortest = _graph.ShortestPathsDijkstra(e => new Point3d(e.Source.location.X, e.Source.location.Y, 0).DistanceTo(new Point3d(e.Target.location.X, e.Target.location.Y, 0)), start);

                if (shortest(end, out var path))
                {
                    var current = start;
                    indices.Add(current.index);

                    foreach (var edge in path)
                    {
                        stepCount++;
                        current = edge.GetOtherVertex(current);
                        indices.Add(current.index);
                    }
                }
            }
            return stepCount;

        }

        public List<Face> GetFaces()
        {
            List<Face> outFaces = new List<Face>();
            var faces = _grid.GetFaces().Where(f => f.IsActive);
            foreach (var f in faces)
                outFaces.Add(f);

            return outFaces;
        }

        public void InitGraph()
        {
            var faces = GetFaces();
            graphEdges = faces.Select(f => new TaggedEdge<SmartCell, Face>(f.Voxels[0], f.Voxels[1], f));
            edgeLengths = graphEdges.Select(e => new Point3d(e.Source.location.X, e.Source.location.Y, 0).DistanceTo(new Point3d(e.Target.location.X, e.Target.location.Y, 0)));
            _graph = graphEdges.ToUndirectedGraph<SmartCell, TaggedEdge<SmartCell, Face>>();
        }

        /// <summary>
        /// MSP Quickgraph implementation, ortho pathing only.
        /// </summary>
        public void ComputeMSPQG()
        {
            pathCurves = new DataTree<Polyline>();

            var min = 100000000.0;
            var max = -1.0;
            Polyline localPath;
            int countOut = 0; //data tree paths, +=1 per cell

            var cells1 = _grid.GetVoxels().ToArray();
            var cells2 = _grid.GetVoxels().ToArray();

            for (int i = 0; i < cells1.Length; i++)
            {
                double distance = 0.0;
                int count = 0;

                for (int j = 0; j < cells2.Length; j++)
                {
                    if (i == j) continue;
                    else
                    {
                        localPath = new Polyline();
                        List<Vector2dInt> indices;
                        var localDist = GetShortestPath(cells1[i], cells2[j], _graph, out indices);
                        distance += localDist + 0.5;

                        for (int s = 0; s < indices.Count; s++)
                        {
                            SmartCell locCell;
                            if (cells.TryGetValue(indices[s], out locCell))
                                localPath.Add(new Point3d(locCell.location.X, locCell.location.Y, 0));

                            if (s == indices.Count - 1)
                                pathCurves.Add(localPath, new GH_Path(countOut));
                        }
                        count++;
                    }
                }
                distance *= 1.0;
                cells1[i].metric5 = distance / count;
                cells1[i].mspRaw = distance / count;
                countOut++;
            }

            cells = new Dictionary<Vector2dInt, SmartCell>();
            for (int i = 0; i < cells1.Length; i++)
                cells.Add(cells1[i].index, cells1[i]);

            //find min and max
            foreach (KeyValuePair<Vector2dInt, SmartCell> cell in cells)
            {
                double distance = cell.Value.metric5;
                if (distance < min)
                    min = distance;
                if (distance > max)
                    max = distance;
            }

            //remap vals
            foreach (KeyValuePair<Vector2dInt, SmartCell> cell in cells)
            {
                var holder = PBUtilities.mapValue(cell.Value.metric5, min, max, 0.00, 1.00);
                var final = 1.0 - holder;
                cell.Value.metric5 = final;
            }
        }

        /// <summary>
        /// Mean Shortest Path.. Dont use with a large number of components
        /// </summary>
        public void ComputeMeanShortestPath()
        {
            pathCurves = new DataTree<Polyline>();

            var min = 100000000.0;
            var max = -1.0;
            Polyline localPath;
            int countOut = 0; //data tree paths, +=1 per cell

            var cells1 = cells.ToList();
            var cells2 = cells.ToList();

            for (int i = 0; i < cells1.Count; i++)
            {
                double distance = 0.0;
                int count = 0;

                for (int j = 0; j < cells2.Count; j++)
                {
                    if (i == j) continue;
                    else
                    {
                        localPath = new Polyline();
                        var steps = FindPath(cells1[i].Value, cells2[j].Value);

                        localPath.Add(new Point3d(cells1[i].Value.location.X, cells1[i].Value.location.Y, 0));
                        for (int k = 0; k < steps.Count; k++)
                        {
                            if (k == 0)
                                distance += (cells1[k].Value.location - steps[k].location).Length;

                            else
                                distance += (steps[k].location - steps[k - 1].location).Length;

                            localPath.Add(new Point3d(steps[k].location.X, steps[k].location.Y, 0));
                            if (k == steps.Count - 1)
                                pathCurves.Add(localPath, new GH_Path(countOut));
                        }
                        count++;
                    }
                    j++;
                }
                cells1[i].Value.metric5 = distance / count;
                cells1[i].Value.mspRaw = distance / count;
                countOut++;
            }

            //find min and max
            foreach (KeyValuePair<Vector2dInt, SmartCell> cell in cells)
            {
                double distance = cell.Value.metric5;
                if (distance < min)
                    min = distance;
                if (distance > max)
                    max = distance;
            }

            //remap vals
            foreach (KeyValuePair<Vector2dInt, SmartCell> cell in cells)
            {
                var holder = PBUtilities.mapValue(cell.Value.metric5, min, max, 0.00, 1.00);
                var final = 1.0 - holder;
                cell.Value.metric5 = final;
            }
        }

        /// <summary>
        /// A sampling of cells used for sampeld MSP
        /// </summary>
        /// <param name="current"></param>
        /// <param name="cells"></param>
        /// <param name="numberCells"></param>
        /// <returns></returns>
        public List<SmartCell> SelectRandomCells(int current, SmartCell[] cells, int numberCells)
        {
            List<SmartCell> cellsOut = new List<SmartCell>();
            var count = cells.Length;
            var random = new Random(42);
            int[] indeces = new int[numberCells];
            var usedIndeces = new List<int>();
            usedIndeces.Add(current);

            int cellCount = 0;

            while (cellCount < numberCells)
            {
                var randInt = random.Next(0, cells.Length);
                if (usedIndeces.Contains(randInt))
                    continue;
                else
                {
                    indeces[cellCount] = randInt;
                    usedIndeces.Add(randInt);
                    cellCount++;
                }
            }

            for (int i = 0; i < indeces.Length; i++)
                cellsOut.Add(cells[indeces[i]]);

            return cellsOut;

        }

        //MSP Computation
        public void ComputeSampleMeanShortestPath()
        {
            pathCurves = new DataTree<Polyline>();

            var min = 100000000.0;
            var max = -1.0;
            Polyline localPath;
            int countOut = 0; //data tree paths, +=1 per cell

            var cells1 = cells.Select(s => s.Value).ToArray();

            for (int i = 0; i < cells1.Length; i++)
            {
                double distance = 0.0;
                int count = 0;

                List<SmartCell> cells2 = SelectRandomCells(i, cells1, 10);
                List<SmartCell> steps;
                for (int j = 0; j < cells2.Count; j++)
                {
                    localPath = new Polyline();
                    steps = FindPath(cells1[i], cells2[j]);

                    localPath.Add(new Point3d(cells1[i].location.X, cells1[i].location.Y, 0));
                    for (int k = 0; k < steps.Count; k++)
                    {
                        if (k == 0)
                            distance += (cells1[k].location - steps[k].location).Length;

                        else
                            distance += (steps[k].location - steps[k - 1].location).Length;

                        localPath.Add(new Point3d(steps[k].location.X, steps[k].location.Y, 0));
                        if (k == steps.Count - 1)
                            pathCurves.Add(localPath, new GH_Path(countOut));
                    }
                    count++;
                }
                cells1[i].metric5 = distance / count;
                cells1[i].mspRaw = distance / count;
                countOut++;
            }

            cells = new Dictionary<Vector2dInt, SmartCell>();
            for (int i = 0; i < cells1.Length; i++)
                cells.Add(cells1[i].index, cells1[i]);

            //find min and max
            foreach (KeyValuePair<Vector2dInt, SmartCell> cell in cells)
            {
                double distance = cell.Value.metric5;
                if (distance < min)
                    min = distance;
                if (distance > max)
                    max = distance;
            }

            //remap vals
            foreach (KeyValuePair<Vector2dInt, SmartCell> cell in cells)
            {
                var holder = PBUtilities.mapValue(cell.Value.metric5, min, max, 0.00, 1.00);
                var final = 1.0 - holder;
                cell.Value.metric5 = final;
            }
        }

        public void AssignInactiveCells()
        {
            foreach (KeyValuePair<Vector2dInt, SmartCell> _cell in cells)
            {
                var pt = new Point3d(_cell.Value.location.X, _cell.Value.location.Y, 0);
                var meshPt = interiorPartitionMesh.ClosestMeshPoint(pt, 1000000.0);
                var output = pt.DistanceTo(meshPt.Point).ToString();
                if (pt.DistanceTo(meshPt.Point) < _resolution * 0.75)
                    _cell.Value.isActive = false;

            }
        }

        public void AssignInactiveCells(Grid2d grid)
        {
            var vox = grid.GetVoxels();
            foreach (var v in vox)
            {
                var pt = new Point3d(v.location.X, v.location.Y, 0);
                var meshPt = interiorPartitionMesh.ClosestMeshPoint(pt, 1000000.0);
                var output = pt.DistanceTo(meshPt.Point).ToString();
                if (pt.DistanceTo(meshPt.Point) < _resolution * 0.75)
                    grid.Voxels.Remove(v.index);
            }
        }

        public void AssignExitCells(List<Point3d> exitPoints)
        {
            for (int i = 0; i < exitCells.Length; i++)
                exitCells[i] = GetClosestCell(exitPoints[i]);
        }

        public List<SmartCell> FindPath(SmartCell startPos, SmartCell targetPos)
        {

            List<SmartCell> OpenList = new List<SmartCell>();
            List<SmartCell> ClosedList = new List<SmartCell>();

            List<SmartCell> Path = new List<SmartCell>();

            OpenList.Add(startPos);

            while (OpenList.Count > 0)
            {
                SmartCell CurrentCell = OpenList[0];

                for (int i = 0; i < OpenList.Count; i++)
                {
                    if (OpenList[i].FCost < CurrentCell.FCost || OpenList[i].FCost == CurrentCell.FCost && OpenList[i].hCost < CurrentCell.hCost)
                    {
                        CurrentCell = OpenList[i];
                    }
                }
                OpenList.Remove(CurrentCell);
                ClosedList.Add(CurrentCell);

                if (CurrentCell == targetPos)
                {
                    Path = GetFinalPath(startPos, targetPos);
                    return Path;
                }

                var neighbors = GetNeighbors(CurrentCell);

                foreach (SmartCell NeighborCell in neighbors)
                {
                    if (ClosedList.Contains(NeighborCell))
                    {
                        continue;
                    }
                    int MoveCost = CurrentCell.gCost + GetManhattan(CurrentCell, NeighborCell);

                    if (MoveCost < NeighborCell.gCost || !OpenList.Contains(NeighborCell))
                    {
                        NeighborCell.gCost = MoveCost;
                        NeighborCell.hCost = GetManhattan(NeighborCell, targetPos);
                        NeighborCell.Parent = CurrentCell;

                        if (!OpenList.Contains(NeighborCell))
                            OpenList.Add(NeighborCell);
                    }
                }
            }

            return Path;

        }

        List<SmartCell> GetFinalPath(SmartCell a_StartingCell, SmartCell a_EndingCell)
        {
            List<SmartCell> FinalPath = new List<SmartCell>();
            SmartCell CurrentCell = a_EndingCell;

            while (CurrentCell != a_StartingCell)
            {
                FinalPath.Add(CurrentCell);
                CurrentCell = CurrentCell.Parent;
            }

            FinalPath.Reverse();

            return FinalPath;
        }

        public int GetManhattan(SmartCell Cell_A, SmartCell Cell_B)
        {
            double dX = Math.Abs(Cell_A.location.X - Cell_B.location.X);
            double dY = Math.Abs(Cell_A.location.Y - Cell_B.location.Y);

            return (int)(dX + dY);
        }

        public IEnumerable<SmartCell> GetNeighbors(SmartCell cell)
        {
            for (int yi = -1; yi <= 1; yi++)
            {
                int y = (int)(yi + cell.index.Y);

                for (int xi = -1; xi <= 1; xi++)
                {
                    int x = (int)(xi + cell.index.X);

                    var i = new Vector2dInt(x, y);

                    if (cell.index == i) continue;

                    SmartCell voxel;

                    if (cells.TryGetValue(i, out voxel))
                        if (voxel.isActive == true)
                            yield return voxel;
                }
            }

        }

        public SmartCell GetClosestCell(Point3d target)
        {
            SmartCell eCell;
            var c = new SmartCell[cells.Count];
            int count = 0;
            double[] distances = new double[cells.Count];

            foreach (KeyValuePair<Vector2dInt, SmartCell> _cell in cells)
                _cell.Value.tempMetric = 0.0;

            foreach (KeyValuePair<Vector2dInt, SmartCell> _cell in cells)
            {
                var pt = new Point3d(_cell.Value.location.X, _cell.Value.location.Y, 0);
                _cell.Value.tempMetric = pt.DistanceTo(target);
                c[count] = _cell.Value;
                count++;
            }

            var ordered = c.OrderBy(m => m.tempMetric).ToList();
            eCell = ordered[0];

            return eCell;

        }

        Vector2d PlaceLocation(Rectangle3d rectangle)
        {
            return new Vector2d(rectangle.Center.X, rectangle.Center.Y);
        }

        Vector2d PlaceLocation(double x, double y)
        {
            return new Vector2d(x, y);
        }
    }
}


