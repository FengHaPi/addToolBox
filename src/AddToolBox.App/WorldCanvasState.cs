using System.Windows;
using System.Windows.Media;

namespace AddToolBox.App;

internal sealed class WorldCanvasState
{
    internal const double MinimumZoom = 0.25;
    internal const double MaximumZoom = 3.00;
    internal const double ZoomFactorPerNotch = 1.10;
    internal const double InitialWorldMargin = 4096;
    internal const double EdgeExpansionTrigger = 768;
    internal const double ExpansionAmount = 4096;
    internal const double RetentionMargin = 3072;
    internal const double ShrinkMinimumAmount = 4096;

    internal static Point InitialViewportCenterWorld { get; } = new(0, 0);

    internal Point ViewportCenterWorld { get; private set; } =
        InitialViewportCenterWorld;

    internal double ZoomScale { get; private set; } = 1.0;

    internal Rect WorldExtent { get; private set; } = Rect.Empty;

    internal bool IsInitialized => !WorldExtent.IsEmpty;

    internal void Initialize(
        IReadOnlyList<Rect> itemWorldBounds,
        Size viewportSize)
    {
        ResetView();
        var initialExtent = CreateRequiredBounds(itemWorldBounds, viewportSize);
        initialExtent.Inflate(InitialWorldMargin, InitialWorldMargin);
        WorldExtent = initialExtent;
    }

    internal void PanByScreenDelta(Vector screenDelta)
    {
        ValidateFinite(screenDelta.X, nameof(screenDelta));
        ValidateFinite(screenDelta.Y, nameof(screenDelta));
        ViewportCenterWorld = new Point(
            ViewportCenterWorld.X - (screenDelta.X / ZoomScale),
            ViewportCenterWorld.Y - (screenDelta.Y / ZoomScale));
    }

    internal void ResetView()
    {
        ViewportCenterWorld = InitialViewportCenterWorld;
        ZoomScale = 1.0;
    }

    internal bool ZoomAtScreenPoint(Point screenPosition, Size viewportSize, double zoom)
    {
        ValidateFinite(zoom, nameof(zoom));
        var newZoom = Math.Clamp(zoom, MinimumZoom, MaximumZoom);
        if (newZoom == ZoomScale)
        {
            return false;
        }

        var worldUnderCursor = ScreenToWorld(screenPosition, viewportSize);
        ViewportCenterWorld = new Point(
            worldUnderCursor.X - ((screenPosition.X - (viewportSize.Width / 2)) / newZoom),
            worldUnderCursor.Y - ((screenPosition.Y - (viewportSize.Height / 2)) / newZoom));
        ZoomScale = newZoom;
        return true;
    }

    internal Point WorldToScreen(Point worldPosition, Size viewportSize)
    {
        ValidateViewportSize(viewportSize);
        ValidatePoint(worldPosition, nameof(worldPosition));
        return new Point(
            ((worldPosition.X - ViewportCenterWorld.X) * ZoomScale) + (viewportSize.Width / 2),
            ((worldPosition.Y - ViewportCenterWorld.Y) * ZoomScale) + (viewportSize.Height / 2));
    }

    internal Point ScreenToWorld(Point screenPosition, Size viewportSize)
    {
        ValidateViewportSize(viewportSize);
        ValidatePoint(screenPosition, nameof(screenPosition));
        return new Point(
            ViewportCenterWorld.X + ((screenPosition.X - (viewportSize.Width / 2)) / ZoomScale),
            ViewportCenterWorld.Y + ((screenPosition.Y - (viewportSize.Height / 2)) / ZoomScale));
    }

    internal Matrix GetCameraMatrix(Size viewportSize)
    {
        var origin = WorldToScreen(new Point(0, 0), viewportSize);
        // Scale world coordinates first, then translate into the viewport.
        return new Matrix(ZoomScale, 0, 0, ZoomScale, origin.X, origin.Y);
    }

    internal Rect GetViewportWorldBounds(Size viewportSize)
    {
        ValidateViewportSize(viewportSize);
        var worldWidth = viewportSize.Width / ZoomScale;
        var worldHeight = viewportSize.Height / ZoomScale;
        return new Rect(
            ViewportCenterWorld.X - (worldWidth / 2),
            ViewportCenterWorld.Y - (worldHeight / 2),
            worldWidth,
            worldHeight);
    }

    internal void EnsureExpanded(
        Size viewportSize,
        Rect? activeItemWorldBounds = null)
    {
        EnsureInitialized();
        var probeBounds = GetViewportWorldBounds(viewportSize);
        if (activeItemWorldBounds is { } itemBounds)
        {
            ValidateRect(itemBounds, nameof(activeItemWorldBounds));
            probeBounds.Union(itemBounds);
        }

        var left = WorldExtent.Left;
        var top = WorldExtent.Top;
        var right = WorldExtent.Right;
        var bottom = WorldExtent.Bottom;
        left -= GetRequiredExpansion(
            probeBounds.Left - left,
            EdgeExpansionTrigger);
        top -= GetRequiredExpansion(
            probeBounds.Top - top,
            EdgeExpansionTrigger);
        right += GetRequiredExpansion(
            right - probeBounds.Right,
            EdgeExpansionTrigger);
        bottom += GetRequiredExpansion(
            bottom - probeBounds.Bottom,
            EdgeExpansionTrigger);
        WorldExtent = new Rect(
            new Point(left, top),
            new Point(right, bottom));
    }

    internal void Shrink(
        IReadOnlyList<Rect> itemWorldBounds,
        Size viewportSize)
    {
        EnsureInitialized();
        var protectedBounds = CreateRequiredBounds(itemWorldBounds, viewportSize);
        protectedBounds.Inflate(RetentionMargin, RetentionMargin);

        var expandedLeft = WorldExtent.Left
            - GetContainmentExpansion(WorldExtent.Left - protectedBounds.Left);
        var expandedTop = WorldExtent.Top
            - GetContainmentExpansion(WorldExtent.Top - protectedBounds.Top);
        var expandedRight = WorldExtent.Right
            + GetContainmentExpansion(protectedBounds.Right - WorldExtent.Right);
        var expandedBottom = WorldExtent.Bottom
            + GetContainmentExpansion(protectedBounds.Bottom - WorldExtent.Bottom);
        var left = expandedLeft
            + GetPermittedShrink(protectedBounds.Left - expandedLeft);
        var top = expandedTop
            + GetPermittedShrink(protectedBounds.Top - expandedTop);
        var right = expandedRight
            - GetPermittedShrink(expandedRight - protectedBounds.Right);
        var bottom = expandedBottom
            - GetPermittedShrink(expandedBottom - protectedBounds.Bottom);
        WorldExtent = new Rect(
            new Point(left, top),
            new Point(right, bottom));
    }

    private Rect CreateRequiredBounds(
        IReadOnlyList<Rect> itemWorldBounds,
        Size viewportSize)
    {
        ArgumentNullException.ThrowIfNull(itemWorldBounds);
        var requiredBounds = GetViewportWorldBounds(viewportSize);
        foreach (var itemBounds in itemWorldBounds)
        {
            ValidateRect(itemBounds, nameof(itemWorldBounds));
            requiredBounds.Union(itemBounds);
        }

        return requiredBounds;
    }

    private static double GetRequiredExpansion(
        double currentDistance,
        double triggerDistance)
    {
        if (currentDistance >= triggerDistance)
        {
            return 0;
        }

        var missingDistance = triggerDistance - currentDistance;
        var chunkCount = Math.Max(1, Math.Ceiling(missingDistance / ExpansionAmount));
        return chunkCount * ExpansionAmount;
    }

    private static double GetPermittedShrink(double availableDistance)
    {
        if (availableDistance < ShrinkMinimumAmount)
        {
            return 0;
        }

        return Math.Floor(availableDistance / ExpansionAmount) * ExpansionAmount;
    }

    private static double GetContainmentExpansion(double missingDistance)
    {
        if (missingDistance <= 0)
        {
            return 0;
        }

        return Math.Ceiling(missingDistance / ExpansionAmount) * ExpansionAmount;
    }

    private void EnsureInitialized()
    {
        if (!IsInitialized)
        {
            throw new InvalidOperationException("World canvas has not been initialized.");
        }
    }

    private static void ValidateViewportSize(Size viewportSize)
    {
        if (!IsFinite(viewportSize.Width)
            || !IsFinite(viewportSize.Height)
            || viewportSize.Width <= 0
            || viewportSize.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(viewportSize),
                "Viewport size must be finite and positive.");
        }
    }

    private static void ValidatePoint(Point point, string parameterName)
    {
        ValidateFinite(point.X, parameterName);
        ValidateFinite(point.Y, parameterName);
    }

    private static void ValidateRect(Rect bounds, string parameterName)
    {
        if (bounds.IsEmpty
            || !IsFinite(bounds.Left)
            || !IsFinite(bounds.Top)
            || !IsFinite(bounds.Width)
            || !IsFinite(bounds.Height)
            || bounds.Width < 0
            || bounds.Height < 0)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "World bounds must be finite and non-empty.");
        }
    }

    private static void ValidateFinite(double value, string parameterName)
    {
        if (!IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                "Coordinate values must be finite.");
        }
    }

    private static bool IsFinite(double value) =>
        !double.IsNaN(value) && !double.IsInfinity(value);
}
