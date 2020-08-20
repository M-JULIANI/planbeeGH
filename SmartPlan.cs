using Grasshopper;
using Grasshopper.Kernel.Data;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Planbee
{
    public class SmartPlan
    {
        public double _resolution;
        public Point3d minPoint;
        public Curve perimCurve;
        public Curve[] _coreCurves;
        public Mesh coreMesh;
        public Point3d[] pts;
        SortedDictionary<Vector2d, SmartCell> cells;
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
        public Vector3d[] isovistDirections;
        Curve[] _partCurves;

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

            cells = new SortedDictionary<Vector2d, SmartCell>();
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

            cells = new SortedDictionary<Vector2d, SmartCell>();
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

            cells = new SortedDictionary<Vector2d, SmartCell>();
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

            cells = new SortedDictionary<Vector2d, SmartCell>();
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

            cells = new SortedDictionary<Vector2d, SmartCell>();
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

            cells = new SortedDictionary<Vector2d, SmartCell>();

            for (int i = 0; i < rectangles.Count; i++)
            {
                var loc = new Vector2d(rectangles[i].Center.X, rectangles[i].Center.Y);
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

            cells = new SortedDictionary<Vector2d, SmartCell>();

            for (int i = 0; i < rectangles.Count; i++)
            {
                var loc = new Vector2d(rectangles[i].Center.X, rectangles[i].Center.Y);
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
        public SmartPlan(Curve perimCurve, List<Curve> coreCurves, List<Rectangle3d> rectangles, Plane plane)
        {
            _plane = new Plane(plane.Origin, Vector3d.ZAxis);
            project = Transform.PlanarProjection(_plane);

            this._resolution = Math.Sqrt(rectangles[0].Area);
            exitCells = new SmartCell[0];

            this.perimCurve = perimCurve;
            _coreCurves = new Curve[coreCurves.Count];
            for (int i = 0; i < _coreCurves.Length; i++)
                _coreCurves[i] = coreCurves[i];

            cells = new SortedDictionary<Vector2d, SmartCell>();

            for (int i = 0; i < rectangles.Count; i++)
            {
                var loc = new Vector2d(rectangles[i].Center.X, rectangles[i].Center.Y);
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


        //simple shortest path
        public SmartPlan(List<Rectangle3d> rectangles, List<Point3d> exitPoints, Plane plane)
        {
            _plane = new Plane(plane.Origin, Vector3d.ZAxis);
            project = Transform.PlanarProjection(_plane);

            this._resolution = Math.Sqrt(rectangles[0].Area);
            exitCells = new SmartCell[exitPoints.Count];

            cells = new SortedDictionary<Vector2d, SmartCell>();

            for (int i = 0; i < rectangles.Count; i++)
            {
                var loc = new Vector2d(rectangles[i].Center.X, rectangles[i].Center.Y);
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

            cells = new SortedDictionary<Vector2d, SmartCell>();

            for (int i = 0; i < rectangles.Count; i++)
            {
                var loc = new Vector2d(rectangles[i].Center.X, rectangles[i].Center.Y);
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
            _coreCurves = new Curve[coreCurves.Count];
            for (int i = 0; i < _coreCurves.Length; i++)
                _coreCurves[i] = _coreCurves[i];

            cells = new SortedDictionary<Vector2d, SmartCell>();

            for (int i = 0; i < rectangles.Count; i++)
            {
                var loc = new Vector2d(rectangles[i].Center.X, rectangles[i].Center.Y);
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

            testLines = new DataTree<Line>();

        }

        public List<Rectangle3d> getCells()
        {
            var rects = new List<Rectangle3d>();

            foreach (KeyValuePair<Vector2d, SmartCell> _cell in cells)
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
                foreach (KeyValuePair<Vector2d, SmartCell> _cell in cells)
                {
                    att[count] = _cell.Value.metric4;
                    count++;
                }
                return att;
            }
        }

        public double[] getIsovist()
        {
            double[] isos = new double[cells.Count];
            int count = 0;
            foreach (KeyValuePair<Vector2d, SmartCell> _cell in cells)
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
            foreach (KeyValuePair<Vector2d, SmartCell> _cell in cells)
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
            foreach (KeyValuePair<Vector2d, SmartCell> _cell in cells)
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
            foreach (KeyValuePair<Vector2d, SmartCell> _cell in cells)
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
                    var loc = new Vector2d(samplePt.X, samplePt.Y);
                    var index = new Vector2d(Math.Round(roundedPt.X / this._resolution), Math.Round(roundedPt.Y / this._resolution));

                    if (this.perimCurve.Contains(samplePt, _plane, 0.00001) != Rhino.Geometry.PointContainment.Inside)
                    {
                        continue;
                    }
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

            foreach (KeyValuePair<Vector2d, SmartCell> cell in cells)
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

            foreach (KeyValuePair<Vector2d, SmartCell> cell in cells)
            {
                var holder = PBUtilities.mapValue(cell.Value.metric2, min, max, 0.00, 1.00);
                cell.Value.metric2 = holder;
            }
        }

        public void ComputeIsovist()
        {
            Transform mov = Transform.Translation(-0.5 * Vector3d.ZAxis);
            isoPolylines = new Polyline[cells.Count];

            interiorPartitionMesh = new Mesh();
            meshCore = new Mesh();

            Extrusion[] extrusionCores = new Extrusion[_coreCurves.Length];
            for (int i = 0; i < _coreCurves.Length; i++)
            {
                var extr = Extrusion.CreateExtrusion(_coreCurves[i], Vector3d.ZAxis);// core extrusion
                extr.Transform(mov);
                meshCore.Append(Mesh.CreateFromSurface(extr));
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

            foreach (KeyValuePair<Vector2d, SmartCell> cell in cells)
            {
                var interSum = 0.0;
                Polyline poly = new Polyline();
                Point3d memPt = new Point3d(0, 0, 0);
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

                //cell.Value.metric1 = interSum;
                //if (interSum < min)
                //    min = interSum;
                //if (interSum > max)
                //    max = interSum;

                poly.Add(memPt);

                isoPolylines[count] = poly;

                var segments = poly.BreakAtAngles(20);
                var crvs = new List<Curve>();
                for (int i = 0; i < segments.Length; i++)
                    crvs.Add(segments[i].ToNurbsCurve());

                var brep = Brep.CreateEdgeSurface(crvs);
                var area = AreaMassProperties.Compute(brep).Area;

                cell.Value.metric1 = area;
                if (area < min)
                    min = area;
                if (area > max)
                    max = area;

                count++;
            }

            foreach (KeyValuePair<Vector2d, SmartCell> cell in cells)
            {
                var holder = PBUtilities.mapValue(cell.Value.metric1, min, max, 0.00, 1.00);
                cell.Value.metric1 = holder;
            }

        }

        public void ComputeDistToPerimeter()
        {
            var min = 1000.0;
            var max = 1.0;
            foreach (KeyValuePair<Vector2d, SmartCell> cell in cells)
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

            foreach (KeyValuePair<Vector2d, SmartCell> cell in cells)
            {
                var holder = PBUtilities.mapValue(cell.Value.metric2, min, max, 0.0, 1.0);
                var final = 1.0 - holder;
                cell.Value.metric2 = final;
            }

        }

        public void ComputeAttractionViz()
        {
            obstacleMeshJoined = new Mesh();
            attractorMeshJoined = new Mesh();

            Mesh[] obstMeshExtrusions = new Mesh[obstCrvs.Length + _coreCurves.Length];
            Mesh[] attrMeshExtrusions = new Mesh[attrCrvs.Length];

            Surface srfExtrusion;

            Transform mov = Transform.Translation(-0.5 * Vector3d.ZAxis);

            for (int i = 0; i < obstMeshExtrusions.Length; i++)
            {
                if (i > obstCrvs.Length)
                    srfExtrusion = Extrusion.CreateExtrusion(_coreCurves[i + obstCrvs.Length], Vector3d.ZAxis);
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

            var min = 1000.0;
            var max = 1.0;
            int count = 0;

            foreach (KeyValuePair<Vector2d, SmartCell> cell in cells)
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

            foreach (KeyValuePair<Vector2d, SmartCell> cell in cells)
            {
                var holder = PBUtilities.mapValue(cell.Value.metric3, min, max, 0.00, 1.00);
                cell.Value.metric3 = holder;
            }
        }

        public void ComputeExitAccess()
        {
            pathCurves = new DataTree<Polyline>();

            var min = 1000000.0;
            var max = -1.0;
            int count = 0;
            Polyline localPath;

            foreach (KeyValuePair<Vector2d, SmartCell> cell in cells)
            {
                double distance = 0.0;
                for (int j = 0; j < exitCells.Length; j++)
                {
                    localPath = new Polyline();
                    var steps = FindPath(cell.Value, exitCells[j]);

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

                cell.Value.metric4 = distance;
                if (distance < min)
                    min = distance;
                if (distance > max)
                    max = distance;

                count++;
            }

            foreach (KeyValuePair<Vector2d, SmartCell> cell in cells)
            {
                var holder = PBUtilities.mapValue(cell.Value.metric4, min, max, 0.00, 1.00);
                var final = 1.0 - holder;
                cell.Value.metric4 = final;
            }
        }


        public void AssignInactiveCells()
        {
            foreach (KeyValuePair<Vector2d, SmartCell> _cell in cells)
            {
                var pt = new Point3d(_cell.Value.location.X, _cell.Value.location.Y, 0);
                var meshPt = interiorPartitionMesh.ClosestMeshPoint(pt, 100000);
                var output = pt.DistanceTo(meshPt.Point).ToString();
                if (pt.DistanceTo(meshPt.Point) < 0.5)
                    _cell.Value.isActive = false;

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

        public List<SmartCell> GetNeighbors(SmartCell cell)
        {
            var neighbors = new List<SmartCell>();

            for (int yi = -1; yi <= 1; yi++)
            {
                int y = (int)(yi + cell.index.Y);

                for (int xi = -1; xi <= 1; xi++)
                {
                    int x = (int)(xi + cell.index.X);

                    var i = new Vector2d(x, y);

                    if (cell.index == i) continue;

                    SmartCell voxel;

                    if (cells.TryGetValue(i, out voxel))
                    {
                        if (voxel.isActive == true)
                            neighbors.Add(voxel);

                    }
                }
            }

            return neighbors;
        }

        public SmartCell GetClosestCell(Point3d target)
        {
            SmartCell eCell;
            var c = new SmartCell[cells.Count];
            int count = 0;
            double[] distances = new double[cells.Count];

            foreach (KeyValuePair<Vector2d, SmartCell> _cell in cells)
                _cell.Value.tempMetric = 0.0;

            foreach (KeyValuePair<Vector2d, SmartCell> _cell in cells)
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
    }
}


