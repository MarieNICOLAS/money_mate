using System.Collections;

namespace MoneyMate.Components;

public sealed class DonutChartView : GraphicsView
{
    public static readonly BindableProperty SegmentsProperty =
        BindableProperty.Create(
            nameof(Segments),
            typeof(IEnumerable),
            typeof(DonutChartView),
            null,
            propertyChanged: OnSegmentsChanged);

    private readonly DonutChartDrawable _drawable;

    public DonutChartView()
    {
        HeightRequest = 190;
        WidthRequest = 190;
        _drawable = new DonutChartDrawable(this);
        Drawable = _drawable;
    }

    public IEnumerable? Segments
    {
        get => (IEnumerable?)GetValue(SegmentsProperty);
        set => SetValue(SegmentsProperty, value);
    }

    private static void OnSegmentsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is DonutChartView chart)
            chart.Invalidate();
    }

    private sealed class DonutChartDrawable : IDrawable
    {
        private readonly DonutChartView _chart;

        public DonutChartDrawable(DonutChartView chart)
        {
            _chart = chart;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            List<DonutChartSegment> segments = _chart.Segments?
                .OfType<DonutChartSegment>()
                .Where(segment => segment.Value > 0)
                .ToList() ?? [];

            float size = Math.Min(dirtyRect.Width, dirtyRect.Height);
            float strokeSize = size * 0.20f;
            float radius = (size - strokeSize) / 2f;
            float centerX = dirtyRect.Center.X;
            float centerY = dirtyRect.Center.Y;
            RectF arcRect = new(centerX - radius, centerY - radius, radius * 2, radius * 2);

            canvas.StrokeSize = strokeSize;
            canvas.StrokeLineCap = LineCap.Butt;

            if (segments.Count == 0)
            {
                canvas.StrokeColor = Color.FromArgb("#E3EAF1");
                canvas.DrawArc(arcRect, 0, 360, false, false);
                return;
            }

            double total = segments.Sum(segment => segment.Value);
            float startAngle = -90f;

            foreach (DonutChartSegment segment in segments)
            {
                float sweepAngle = (float)(segment.Value / total * 360d);

                canvas.StrokeColor = segment.Color;
                canvas.DrawArc(arcRect, startAngle, startAngle + sweepAngle, false, false);

                startAngle += sweepAngle;
            }
        }
    }
}
