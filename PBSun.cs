using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace Planbee
{
    public class PBSun
    {
        Rhino.Render.Sun Sun;
        double[] altitudes;
        double[] azimuths;
        public List<Vector3d> SunVectors;

        double Latitude; //location coordinates
        double Longitude;
        int MonthStart; //1-12
        int MonthEnd;
        int TimeStart; //0 to 24
        int TimeEnd;

        int _Interval = 4;

        public PBSun(double lat, double lon, int monthStart, int monthEnd, int timeStart, int timeEnd)
        {
            Sun = new Rhino.Render.Sun();

            Latitude = lat;
            Longitude = lon;
            MonthStart = monthStart;
            MonthEnd = monthEnd;
            TimeStart = timeStart;
            TimeEnd = timeEnd;
        }

        public void ComputeVectors()
        {
            int delta;

            if (MonthStart > MonthEnd)
            {
                delta = 11 - MonthStart;
                delta += MonthEnd + 1;
            }
            else delta = MonthEnd - MonthStart;

            int hours = TimeEnd - TimeStart;
            int time;
            int month;
            int days;
            DateTime date;
            double _altitude = Double.NaN;
            double _azimuth = Double.NaN;

            int count = 0;
            for (int i = 0; i < delta + 1; i++)
            {
                month = ((MonthStart + i) % 12);
                days = DateTime.DaysInMonth(2019, month + 1);

                for (int j = 0; j < days; j++)
                    if (j % _Interval == 0)
                        for (int h = 0; h < hours; h++)
                            count++;
            }

            altitudes = new double[count];
            azimuths = new double[count];
            SunVectors = new List<Vector3d>();

            int count2 = 0;
            Vector3d vec = Vector3d.Unset;
            for (int i = 0; i < delta + 1; i++)
            {
                month = ((MonthStart + i) % 12);
                days = DateTime.DaysInMonth(2019, month + 1);

                for (int j = 0; j < days; j++)
                    if (j % _Interval == 0) // to avoid computing ALL angles but rather get something representative
                        for (int h = 0; h < hours; h++)
                        {
                            time = TimeStart + h;
                            date = new DateTime(2019, month + 1, j + 1, time, 0, 0);
                            Sun.SetPosition(date, Latitude, Longitude);
                            _altitude = Sun.Altitude * Math.PI / 180.0;
                            _azimuth = Sun.Azimuth * Math.PI / 180.0;
                            altitudes[count2] = _altitude;
                            azimuths[count2] = _azimuth;
                            count2++;

                            vec = new Vector3d(0, 1, 0);
                            vec.Rotate(_altitude, Vector3d.XAxis);
                            vec.Rotate(_azimuth, Vector3d.ZAxis);
                            if(vec.Z>0.0)
                                SunVectors.Add(vec);
                            
                        }
            }
        }
    }
}