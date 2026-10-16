using SkiaSharp;

namespace RealmStudioShapeRenderingLib
{
    public class ImportRegion : Shape2D
    {
        public int Index { get; init; } = 0;

        public ImportRegionSource Source { get; init; }

        public ImportRegionState State { get; set; }

        public double? Confidence { get; init; }

        public double Area { get; init; }

        public double PerimeterLength { get; init; }

        public double ScaffoldPerimeterSupport { get; init; }

        public ImportRegion(SKPath geometry)
        {
            ArgumentNullException.ThrowIfNull(geometry);

            RestoreGeometry(geometry);
        }

        public static ImportRegion Clone(ImportRegion other)
        {
            ArgumentNullException.ThrowIfNull(other.HitPath);

            ImportRegion newRegion = new(other.HitPath)
            {
                Index = other.Index,
                Source = other.Source,
                State = other.State,
                Confidence = other.Confidence,
                Area = other.Area,
                PerimeterLength = other.PerimeterLength,
                ScaffoldPerimeterSupport = other.ScaffoldPerimeterSupport
            };

            return newRegion;
        }

        public override void Render(SKCanvas canvas, FontManager? fontManager, SKPath? clipPath = null)
        {
            if (IsSelected)
            {
                canvas.DrawPath(HitPath, PaintObjects.SelectedImportRegionFillPaint);
                canvas.DrawPath(HitPath, PaintObjects.SelectedImportRegionOutlinePaint);
            }
            else
            {
                if (State == ImportRegionState.Proposed)
                {
                    canvas.DrawPath(HitPath, PaintObjects.ImportRegionProposedOutlinePaint);
                }
                else if (State == ImportRegionState.Accepted)
                {
                    canvas.DrawPath(HitPath, PaintObjects.AcceptedImportRegionFillPaint);
                    canvas.DrawPath(HitPath, PaintObjects.AcceptedImportRegionOutlinePaint);
                }
                else if (State == ImportRegionState.Committed)
                {
                    canvas.DrawPath(HitPath, PaintObjects.CommittedImportRegionFillPaint);
                    canvas.DrawPath(HitPath, PaintObjects.CommittedImportRegionOutlinePaint);
                }
                else
                {
                    canvas.DrawPath(HitPath, PaintObjects.ImportRegionOutlinePaint);
                }
            }

        }
    }
}
