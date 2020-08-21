using System;
using System.Collections.Generic;
using System.Drawing;

using Rhino.Geometry;

using Grasshopper;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;

namespace PlanBee
{
    public class Testingcs : GH_Component
    {

        public override GH_Exposure Exposure => GH_Exposure.hidden;

        public Testingcs() : base("Curve Tangents", "CT", "Create a bunch of curve tangent lines", "PlanBee", "Testing")
        {
            SampleCount = 10;
        }
        public override void CreateAttributes()
        {
            m_attributes = new CustomAttributes(this);
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("Curve", "C", "Curve to divide", GH_ParamAccess.item);
            pManager.AddNumberParameter("Length", "L", "Length of tangent lines", GH_ParamAccess.item, 10.0);
        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddLineParameter("Tangents", "T", "Tangent lines", GH_ParamAccess.list);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve curve = null;
            double length = 0.0;

            if (!DA.GetData(0, ref curve)) return;
            if (!DA.GetData(1, ref length)) return;

            double[] ts = curve.DivideByCount(SampleCount, true);
            List<Line> lines = new List<Line>(ts.Length);
            foreach (double t in ts)
            {
                Line line = new Line(curve.PointAt(t), length * curve.TangentAt(t));
                lines.Add(line);
            }
            DA.SetDataList(0, lines);
        }
        public int SampleCount { get; set; }

        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetInt32("SampleCount", SampleCount);
            return base.Write(writer);
        }
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            SampleCount = Math.Max(1, reader.GetInt32("SampleCount"));
            return base.Read(reader);
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("0323e446-a50e-4666-995f-33f791b3eb52"); }
        }


    }

    public class CustomAttributes : GH_ComponentAttributes
    {
        public CustomAttributes(Testingcs owner) : base(owner) { }

        #region Custom layout logic
        private RectangleF SamplesFewBounds { get; set; }
        private RectangleF SamplesSomeBounds { get; set; }
        private RectangleF SamplesManyBounds { get; set; }
        private GH_CustomButton butt { get; set; }

        protected override void Layout()
        {
            base.Layout();


            //We'll extend the basic layout by adding three regions to the bottom of this component,
            SamplesFewBounds = new RectangleF(Bounds.X, Bounds.Bottom, Bounds.Width, 20);
            SamplesSomeBounds = new RectangleF(Bounds.X, Bounds.Bottom + 20, Bounds.Width, 20);
            SamplesManyBounds = new RectangleF(Bounds.X, Bounds.Bottom + 40, Bounds.Width, 20);
            Bounds = new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height + 60);
        }
        #endregion

        #region Custom Mouse handling
        public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                Testingcs comp = Owner as Testingcs;

                if (SamplesFewBounds.Contains(e.CanvasLocation))
                {
                    if (comp.SampleCount == 10) return GH_ObjectResponse.Handled;
                    comp.RecordUndoEvent("Few Samples");
                    comp.SampleCount = 10;
                    comp.ExpireSolution(true);
                    return GH_ObjectResponse.Handled;
                }

                if (SamplesSomeBounds.Contains(e.CanvasLocation))
                {
                    if (comp.SampleCount == 100) return GH_ObjectResponse.Handled;
                    comp.RecordUndoEvent("Some Samples");
                    comp.SampleCount = 100;
                    comp.ExpireSolution(true);
                    return GH_ObjectResponse.Handled;
                }

                if (SamplesManyBounds.Contains(e.CanvasLocation))
                {
                    if (comp.SampleCount == 1000) return GH_ObjectResponse.Handled;
                    comp.RecordUndoEvent("Many Samples");
                    comp.SampleCount = 1000;
                    comp.ExpireSolution(true);
                    return GH_ObjectResponse.Handled;
                }
            }
            return base.RespondToMouseDown(sender, e);
        }
        #endregion

        #region Custom Render logic
        protected override void Render(GH_Canvas canvas, System.Drawing.Graphics graphics, GH_CanvasChannel channel)
        {
            switch (channel)
            {
                case GH_CanvasChannel.Objects:
                    //We need to draw everything outselves.
                    base.RenderComponentCapsule(canvas, graphics, true, false, false, true, true, true);

                    Testingcs comp = Owner as Testingcs;

                    GH_Capsule buttonFew = GH_Capsule.CreateCapsule(SamplesFewBounds, comp.SampleCount == 10 ? GH_Palette.Black : GH_Palette.White);
                    buttonFew.Render(graphics, this.Selected, Owner.Locked, Owner.Hidden);
                    buttonFew.Dispose();

                    GH_Capsule buttonSome = GH_Capsule.CreateCapsule(SamplesSomeBounds, comp.SampleCount == 100 ? GH_Palette.Black : GH_Palette.White);
                    buttonSome.Render(graphics, this.Selected, Owner.Locked, Owner.Hidden);
                    buttonSome.Dispose();

                    GH_Capsule buttonMany = GH_Capsule.CreateCapsule(SamplesManyBounds, comp.SampleCount == 1000 ? GH_Palette.Black : GH_Palette.White);
                    buttonMany.Render(graphics, this.Selected, Owner.Locked, Owner.Hidden);
                    buttonMany.Dispose();

                    graphics.DrawString("Few", GH_FontServer.Standard, comp.SampleCount == 10 ? Brushes.White : Brushes.Black, SamplesFewBounds, GH_TextRenderingConstants.CenterCenter);
                    graphics.DrawString("Some", GH_FontServer.Standard, comp.SampleCount == 100 ? Brushes.White : Brushes.Black, SamplesSomeBounds, GH_TextRenderingConstants.CenterCenter);
                    graphics.DrawString("Many", GH_FontServer.Standard, comp.SampleCount == 1000 ? Brushes.White : Brushes.Black, SamplesManyBounds, GH_TextRenderingConstants.CenterCenter);

                    break;
                default:
                    base.Render(canvas, graphics, channel);
                    break;
            }
        }
        #endregion
    }
    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>

    
}