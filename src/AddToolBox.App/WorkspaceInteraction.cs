using System.Windows;

namespace AddToolBox.App;

[Flags]
internal enum WorkspaceBoundary
{
    None = 0,
    Left = 1,
    Top = 2,
    Right = 4,
    Bottom = 8
}

internal readonly record struct SoftWorkspaceBounds(
    double MinimumX,
    double MinimumY,
    double MaximumX,
    double MaximumY)
{
    internal Point Clamp(Point point)
    {
        return new Point(
            Math.Clamp(point.X, MinimumX, MaximumX),
            Math.Clamp(point.Y, MinimumY, MaximumY));
    }
}

internal readonly record struct WindowBoundsSnapshot(
    double Left,
    double Top,
    double Width,
    double Height)
{
    internal double Right => Left + Width;

    internal double Bottom => Top + Height;
}

internal readonly record struct ResizeConstraintResult(
    bool IsValid,
    int Iterations,
    int IterationLimit);

internal enum AxisProjectionResultKind
{
    PreferredFastPath,
    FixedProjectedSuccess,
    FixedGridInfeasible,
    NumericRangeExceeded,
    NumericInvariantViolation,
    InvalidInput
}

internal readonly record struct AxisProjectionItem(
    string ToolId,
    double PreferredStart,
    double Size);

internal readonly record struct AxisProjectionPosition(
    string ToolId,
    double Start,
    double Size,
    long? FixedStartTick);

internal readonly record struct AxisProjectionResult(
    AxisProjectionResultKind Kind,
    AxisProjectionPosition[] Positions);

internal enum LayoutProjectionResultKind
{
    PreferredFastPath,
    ProjectedSuccess,
    GeometricInfeasible,
    FixedGridInfeasible,
    NumericRangeExceeded,
    NumericInvariantViolation,
    MergeLimitExceeded,
    InvalidInput
}

internal enum LayoutProjectionAxis
{
    None,
    X,
    Y,
    Ambiguous
}

internal enum LayoutProjectionCandidateKind
{
    NotAvailable,
    Feasible,
    GeometricInfeasible,
    FixedGridInfeasible
}

internal readonly record struct LayoutProjectionItem(
    string ToolId,
    double PreferredX,
    double PreferredY,
    double Width,
    double Height);

internal readonly record struct LayoutProjectionPosition(
    string ToolId,
    double ResolvedX,
    double ResolvedY,
    long? FixedXTick,
    long? FixedYTick);

internal readonly record struct LayoutProjectionTraceStep(
    int Pass,
    string FirstToolId,
    string SecondToolId,
    LayoutProjectionCandidateKind XCandidate,
    LayoutProjectionCandidateKind YCandidate,
    LayoutProjectionAxis PreferredAxis,
    LayoutProjectionAxis ChosenAxis,
    int RemainingGroupCount);

internal readonly record struct LayoutProjectionResult(
    LayoutProjectionResultKind Kind,
    LayoutProjectionPosition[] Positions,
    int AcceptedMerges,
    int PassCount,
    LayoutProjectionTraceStep[] MergeTrace);

internal static class WorkspaceInteraction
{
    internal const long TicksPerDip = 1024;
    internal const double SoftBoundaryPadding = 18;
    internal const double BoundaryContactEpsilon = 0.75;

    private const long MaximumExactDoubleTick = 1L << 53;
    private const double AxisDominanceRatio = 1.20;
    private const double RelationNumericalTolerance = 0.000001;

    internal static SoftWorkspaceBounds GetSoftBounds(
        double workspaceWidth,
        double workspaceHeight,
        double toolWidth,
        double toolHeight)
    {
        var maximumX = Math.Max(
            SoftBoundaryPadding,
            workspaceWidth - SoftBoundaryPadding - toolWidth);
        var maximumY = Math.Max(
            SoftBoundaryPadding,
            workspaceHeight - SoftBoundaryPadding - toolHeight);

        return new SoftWorkspaceBounds(
            SoftBoundaryPadding,
            SoftBoundaryPadding,
            maximumX,
            maximumY);
    }

    internal static SoftWorkspaceBounds GetWorldDragBounds(
        Rect viewportWorldBounds,
        Size toolWorldSize,
        double zoomScale)
    {
        var padding = SoftBoundaryPadding / zoomScale;
        var minimumX = viewportWorldBounds.Left + padding;
        var minimumY = viewportWorldBounds.Top + padding;
        return new SoftWorkspaceBounds(
            minimumX,
            minimumY,
            Math.Max(minimumX, viewportWorldBounds.Right - padding - toolWorldSize.Width),
            Math.Max(minimumY, viewportWorldBounds.Bottom - padding - toolWorldSize.Height));
    }

    internal static Point ConstrainDragPosition(
        Point currentPosition,
        Point desiredPosition,
        SoftWorkspaceBounds bounds)
    {
        // A camera change can leave an item outside the safety inset. Permit a
        // continuous drag back in, but never increase its existing overflow.
        return new Point(
            Math.Clamp(
                desiredPosition.X,
                Math.Min(currentPosition.X, bounds.MinimumX),
                Math.Max(currentPosition.X, bounds.MaximumX)),
            Math.Clamp(
                desiredPosition.Y,
                Math.Min(currentPosition.Y, bounds.MinimumY),
                Math.Max(currentPosition.Y, bounds.MaximumY)));
    }

    internal static WorkspaceBoundary GetPressedBoundaryContacts(
        Point actualResolvedPosition,
        Point rawDesiredPosition,
        SoftWorkspaceBounds bounds)
    {
        var contacts = WorkspaceBoundary.None;
        if (Math.Abs(actualResolvedPosition.X - bounds.MinimumX) <= BoundaryContactEpsilon
            && rawDesiredPosition.X < bounds.MinimumX)
        {
            contacts |= WorkspaceBoundary.Left;
        }
        else if (Math.Abs(actualResolvedPosition.X - bounds.MaximumX) <= BoundaryContactEpsilon
                 && rawDesiredPosition.X > bounds.MaximumX)
        {
            contacts |= WorkspaceBoundary.Right;
        }

        if (Math.Abs(actualResolvedPosition.Y - bounds.MinimumY) <= BoundaryContactEpsilon
            && rawDesiredPosition.Y < bounds.MinimumY)
        {
            contacts |= WorkspaceBoundary.Top;
        }
        else if (Math.Abs(actualResolvedPosition.Y - bounds.MaximumY) <= BoundaryContactEpsilon
                 && rawDesiredPosition.Y > bounds.MaximumY)
        {
            contacts |= WorkspaceBoundary.Bottom;
        }

        return contacts;
    }

    internal static WorkspaceBoundary GetInwardMovingBoundaries(
        WindowBoundsSnapshot previous,
        WindowBoundsSnapshot current)
    {
        const double movementTolerance = 0.5;
        var boundaries = WorkspaceBoundary.None;

        if (current.Width < previous.Width - movementTolerance)
        {
            var leftMovement = current.Left - previous.Left;
            var rightMovement = previous.Right - current.Right;
            if (leftMovement > movementTolerance)
            {
                boundaries |= WorkspaceBoundary.Left;
            }

            if (rightMovement > movementTolerance)
            {
                boundaries |= WorkspaceBoundary.Right;
            }

            if ((boundaries & (WorkspaceBoundary.Left | WorkspaceBoundary.Right)) == 0)
            {
                boundaries |= WorkspaceBoundary.Right;
            }
        }

        if (current.Height < previous.Height - movementTolerance)
        {
            var topMovement = current.Top - previous.Top;
            var bottomMovement = previous.Bottom - current.Bottom;
            if (topMovement > movementTolerance)
            {
                boundaries |= WorkspaceBoundary.Top;
            }

            if (bottomMovement > movementTolerance)
            {
                boundaries |= WorkspaceBoundary.Bottom;
            }

            if ((boundaries & (WorkspaceBoundary.Top | WorkspaceBoundary.Bottom)) == 0)
            {
                boundaries |= WorkspaceBoundary.Bottom;
            }
        }

        return boundaries;
    }

    internal static void PreserveScreenPositionsForMovingOrigin(
        Span<Rect> tools,
        WindowBoundsSnapshot previous,
        WindowBoundsSnapshot current,
        WorkspaceBoundary inwardBoundaries)
    {
        var horizontalOffset = (inwardBoundaries & WorkspaceBoundary.Left) != 0
            ? current.Left - previous.Left
            : 0;
        var verticalOffset = (inwardBoundaries & WorkspaceBoundary.Top) != 0
            ? current.Top - previous.Top
            : 0;

        if (Math.Abs(horizontalOffset) < double.Epsilon
            && Math.Abs(verticalOffset) < double.Epsilon)
        {
            return;
        }

        for (var index = 0; index < tools.Length; index++)
        {
            var tool = tools[index];
            tool.Offset(-horizontalOffset, -verticalOffset);
            tools[index] = tool;
        }
    }

    internal static ResizeConstraintResult ConstrainForResize(
        Span<Rect> tools,
        SoftWorkspaceBounds bounds,
        WorkspaceBoundary inwardBoundaries)
    {
        var iterationLimit = Math.Max(1, tools.Length * tools.Length * 4);
        ClampToolsToBounds(tools, bounds);

        for (var iteration = 0; iteration < iterationLimit; iteration++)
        {
            var changed = false;
            for (var firstIndex = 0; firstIndex < tools.Length; firstIndex++)
            {
                for (var secondIndex = firstIndex + 1;
                     secondIndex < tools.Length;
                     secondIndex++)
                {
                    if (!RectanglesOverlap(tools[firstIndex], tools[secondIndex]))
                    {
                        continue;
                    }

                    if (!ResolveResizeOverlap(
                            tools,
                            firstIndex,
                            secondIndex,
                            bounds,
                            inwardBoundaries))
                    {
                        return new ResizeConstraintResult(false, iteration + 1, iterationLimit);
                    }

                    changed = true;
                }
            }

            if (!changed)
            {
                return new ResizeConstraintResult(true, iteration, iterationLimit);
            }
        }

        return new ResizeConstraintResult(false, iterationLimit, iterationLimit);
    }

    internal static Size GetRequiredWorkspaceSize(ReadOnlySpan<Rect> tools)
    {
        if (tools.Length == 0)
        {
            var emptySpan = SoftBoundaryPadding * 2;
            return new Size(emptySpan, emptySpan);
        }

        var requiredWidth = GetRequiredAxisSpan(tools, horizontal: true);
        var requiredHeight = GetRequiredAxisSpan(tools, horizontal: false);
        return new Size(
            requiredWidth + (SoftBoundaryPadding * 2),
            requiredHeight + (SoftBoundaryPadding * 2));
    }

    internal static bool IsLegalLayout(
        ReadOnlySpan<Rect> tools,
        double workspaceWidth,
        double workspaceHeight)
    {
        for (var index = 0; index < tools.Length; index++)
        {
            var tool = tools[index];
            var bounds = GetSoftBounds(
                workspaceWidth,
                workspaceHeight,
                tool.Width,
                tool.Height);
            if (!IsWithinBounds(tool.Location, bounds))
            {
                return false;
            }

            for (var otherIndex = index + 1; otherIndex < tools.Length; otherIndex++)
            {
                if (RectanglesOverlap(tool, tools[otherIndex]))
                {
                    return false;
                }
            }
        }

        return true;
    }

    internal static LayoutProjectionResult Solve2D(
        LayoutProjectionItem[] items,
        double workspaceWidth,
        double workspaceHeight,
        double softBoundaryPadding)
    {
        if (items is null
            || !double.IsFinite(workspaceWidth)
            || !double.IsFinite(workspaceHeight)
            || !double.IsFinite(softBoundaryPadding)
            || workspaceWidth <= 0
            || workspaceHeight <= 0)
        {
            return LayoutProjectionFailure(LayoutProjectionResultKind.InvalidInput);
        }

        var boundaryLeft = softBoundaryPadding;
        var boundaryTop = softBoundaryPadding;
        var boundaryRight = workspaceWidth - softBoundaryPadding;
        var boundaryBottom = workspaceHeight - softBoundaryPadding;
        if (!double.IsFinite(boundaryRight)
            || !double.IsFinite(boundaryBottom)
            || boundaryRight < boundaryLeft
            || boundaryBottom < boundaryTop)
        {
            return LayoutProjectionFailure(LayoutProjectionResultKind.InvalidInput);
        }

        var orderedItems = (LayoutProjectionItem[])items.Clone();
        Array.Sort(
            orderedItems,
            static (first, second) =>
                StringComparer.Ordinal.Compare(first.ToolId, second.ToolId));

        for (var index = 0; index < orderedItems.Length; index++)
        {
            var item = orderedItems[index];
            if (string.IsNullOrWhiteSpace(item.ToolId)
                || (index > 0
                    && StringComparer.Ordinal.Equals(
                        item.ToolId,
                        orderedItems[index - 1].ToolId))
                || !double.IsFinite(item.PreferredX)
                || !double.IsFinite(item.PreferredY)
                || !double.IsFinite(item.Width)
                || !double.IsFinite(item.Height)
                || item.Width <= 0
                || item.Height <= 0)
            {
                return LayoutProjectionFailure(LayoutProjectionResultKind.InvalidInput);
            }

            var preferredRight = item.PreferredX + item.Width;
            var preferredBottom = item.PreferredY + item.Height;
            var preferredCenterX = item.PreferredX + (item.Width / 2);
            var preferredCenterY = item.PreferredY + (item.Height / 2);
            if (!double.IsFinite(preferredRight)
                || !double.IsFinite(preferredBottom)
                || !double.IsFinite(preferredCenterX)
                || !double.IsFinite(preferredCenterY))
            {
                return LayoutProjectionFailure(
                    LayoutProjectionResultKind.NumericRangeExceeded);
            }

            if (item.Width > boundaryRight - boundaryLeft
                || item.Height > boundaryBottom - boundaryTop)
            {
                return LayoutProjectionFailure(
                    LayoutProjectionResultKind.GeometricInfeasible);
            }
        }

        if (IsLegalPreferredLayout(
                orderedItems,
                boundaryLeft,
                boundaryTop,
                boundaryRight,
                boundaryBottom))
        {
            var preferredPositions = new LayoutProjectionPosition[orderedItems.Length];
            for (var index = 0; index < orderedItems.Length; index++)
            {
                var item = orderedItems[index];
                preferredPositions[index] = new LayoutProjectionPosition(
                    item.ToolId,
                    item.PreferredX,
                    item.PreferredY,
                    null,
                    null);
            }

            return new LayoutProjectionResult(
                LayoutProjectionResultKind.PreferredFastPath,
                preferredPositions,
                0,
                0,
                []);
        }

        if (!TryCeilingTick(boundaryLeft, out var boundaryLeftTick)
            || !TryCeilingTick(boundaryTop, out var boundaryTopTick)
            || !TryFloorTick(boundaryRight, out var boundaryRightTick)
            || !TryFloorTick(boundaryBottom, out var boundaryBottomTick))
        {
            return LayoutProjectionFailure(
                LayoutProjectionResultKind.NumericRangeExceeded);
        }

        var itemCount = orderedItems.Length;
        var widthTicks = new long[itemCount];
        var heightTicks = new long[itemCount];
        var preferredXTicks = new long[itemCount];
        var preferredYTicks = new long[itemCount];
        long horizontalSpanTick;
        long verticalSpanTick;
        try
        {
            horizontalSpanTick = checked(boundaryRightTick - boundaryLeftTick);
            verticalSpanTick = checked(boundaryBottomTick - boundaryTopTick);
        }
        catch (OverflowException)
        {
            return LayoutProjectionFailure(
                LayoutProjectionResultKind.NumericRangeExceeded);
        }

        for (var index = 0; index < itemCount; index++)
        {
            var item = orderedItems[index];
            if (!TryCeilingTick(item.Width, out widthTicks[index])
                || !TryCeilingTick(item.Height, out heightTicks[index])
                || !TryNearestEvenTick(item.PreferredX, out preferredXTicks[index])
                || !TryNearestEvenTick(item.PreferredY, out preferredYTicks[index]))
            {
                return LayoutProjectionFailure(
                    LayoutProjectionResultKind.NumericRangeExceeded);
            }

            if (widthTicks[index] > horizontalSpanTick
                || heightTicks[index] > verticalSpanTick)
            {
                return LayoutProjectionFailure(
                    LayoutProjectionResultKind.FixedGridInfeasible);
            }
        }

        var xGroups = CreateSingletonGroups(itemCount);
        var yGroups = CreateSingletonGroups(itemCount);
        var resolvedXTicks = new long[itemCount];
        var resolvedYTicks = new long[itemCount];
        var mergeTrace = new List<LayoutProjectionTraceStep>();
        var acceptedMerges = 0;
        int mergeLimit;
        int passLimit;
        try
        {
            mergeLimit = checked(2 * Math.Max(0, itemCount - 1));
            passLimit = checked((2 * itemCount) - 1);
        }
        catch (OverflowException)
        {
            return LayoutProjectionFailure(
                LayoutProjectionResultKind.NumericRangeExceeded);
        }

        for (var pass = 1; pass <= passLimit; pass++)
        {
            var xProjectionKind = ProjectAxisGroups(
                orderedItems,
                xGroups,
                LayoutProjectionAxis.X,
                boundaryLeft,
                boundaryRight,
                resolvedXTicks);
            if (xProjectionKind != LayoutProjectionResultKind.ProjectedSuccess)
            {
                return LayoutProjectionFailure(
                    xProjectionKind,
                    acceptedMerges,
                    pass,
                    mergeTrace);
            }

            var yProjectionKind = ProjectAxisGroups(
                orderedItems,
                yGroups,
                LayoutProjectionAxis.Y,
                boundaryTop,
                boundaryBottom,
                resolvedYTicks);
            if (yProjectionKind != LayoutProjectionResultKind.ProjectedSuccess)
            {
                return LayoutProjectionFailure(
                    yProjectionKind,
                    acceptedMerges,
                    pass,
                    mergeTrace);
            }

            LayoutConflictEdge[] conflictEdges;
            try
            {
                conflictEdges = BuildConflictEdges(
                    resolvedXTicks,
                    resolvedYTicks,
                    widthTicks,
                    heightTicks);
            }
            catch (OverflowException)
            {
                return LayoutProjectionFailure(
                    LayoutProjectionResultKind.NumericRangeExceeded,
                    acceptedMerges,
                    pass,
                    mergeTrace);
            }

            if (conflictEdges.Length == 0)
            {
                return CreateSuccessfulProjectedLayout(
                    orderedItems,
                    resolvedXTicks,
                    resolvedYTicks,
                    widthTicks,
                    heightTicks,
                    boundaryLeftTick,
                    boundaryTopTick,
                    boundaryRightTick,
                    boundaryBottomTick,
                    boundaryLeft,
                    boundaryTop,
                    boundaryRight,
                    boundaryBottom,
                    acceptedMerges,
                    pass,
                    mergeTrace);
            }

            var conflict = SelectFirstComponentEdge(itemCount, conflictEdges);
            var xGroupIndex = FindGroupIndex(xGroups, conflict.FirstIndex);
            var otherXGroupIndex = FindGroupIndex(xGroups, conflict.SecondIndex);
            var yGroupIndex = FindGroupIndex(yGroups, conflict.FirstIndex);
            var otherYGroupIndex = FindGroupIndex(yGroups, conflict.SecondIndex);
            if (xGroupIndex < 0
                || otherXGroupIndex < 0
                || yGroupIndex < 0
                || otherYGroupIndex < 0)
            {
                return LayoutProjectionFailure(
                    LayoutProjectionResultKind.NumericInvariantViolation,
                    acceptedMerges,
                    pass,
                    mergeTrace);
            }

            var xCandidateExists = xGroupIndex != otherXGroupIndex;
            var yCandidateExists = yGroupIndex != otherYGroupIndex;
            if (!xCandidateExists && !yCandidateExists)
            {
                return LayoutProjectionFailure(
                    LayoutProjectionResultKind.NumericInvariantViolation,
                    acceptedMerges,
                    pass,
                    mergeTrace);
            }

            var xCandidate = SimulateCandidate(
                orderedItems,
                xGroups,
                LayoutProjectionAxis.X,
                xGroupIndex,
                otherXGroupIndex,
                boundaryLeft,
                boundaryRight,
                resolvedXTicks,
                resolvedYTicks,
                preferredXTicks,
                preferredYTicks);
            if (xCandidate.FatalKind is { } xFatalKind)
            {
                return LayoutProjectionFailure(
                    xFatalKind,
                    acceptedMerges,
                    pass,
                    mergeTrace);
            }

            var yCandidate = SimulateCandidate(
                orderedItems,
                yGroups,
                LayoutProjectionAxis.Y,
                yGroupIndex,
                otherYGroupIndex,
                boundaryTop,
                boundaryBottom,
                resolvedXTicks,
                resolvedYTicks,
                preferredXTicks,
                preferredYTicks);
            if (yCandidate.FatalKind is { } yFatalKind)
            {
                return LayoutProjectionFailure(
                    yFatalKind,
                    acceptedMerges,
                    pass,
                    mergeTrace);
            }

            if (!xCandidate.IsFeasible && !yCandidate.IsFeasible)
            {
                var infeasibleKind = xCandidate.Kind
                    == LayoutProjectionCandidateKind.FixedGridInfeasible
                    || yCandidate.Kind
                    == LayoutProjectionCandidateKind.FixedGridInfeasible
                        ? LayoutProjectionResultKind.FixedGridInfeasible
                        : LayoutProjectionResultKind.GeometricInfeasible;
                return LayoutProjectionFailure(
                    infeasibleKind,
                    acceptedMerges,
                    pass,
                    mergeTrace);
            }

            var preferredAxis = GetPreferredAxis(
                orderedItems[conflict.FirstIndex],
                orderedItems[conflict.SecondIndex]);
            if (preferredAxis == LayoutProjectionAxis.None)
            {
                return LayoutProjectionFailure(
                    LayoutProjectionResultKind.NumericRangeExceeded,
                    acceptedMerges,
                    pass,
                    mergeTrace);
            }

            var chosenAxis = ChooseAxis(preferredAxis, xCandidate, yCandidate);
            if (chosenAxis == LayoutProjectionAxis.None)
            {
                return LayoutProjectionFailure(
                    LayoutProjectionResultKind.NumericInvariantViolation,
                    acceptedMerges,
                    pass,
                    mergeTrace);
            }

            if (acceptedMerges >= mergeLimit)
            {
                return LayoutProjectionFailure(
                    LayoutProjectionResultKind.MergeLimitExceeded,
                    acceptedMerges,
                    pass,
                    mergeTrace);
            }

            if (chosenAxis == LayoutProjectionAxis.X)
            {
                xGroups = MergeGroups(xGroups, xGroupIndex, otherXGroupIndex);
            }
            else
            {
                yGroups = MergeGroups(yGroups, yGroupIndex, otherYGroupIndex);
            }

            acceptedMerges++;
            mergeTrace.Add(new LayoutProjectionTraceStep(
                pass,
                orderedItems[conflict.FirstIndex].ToolId,
                orderedItems[conflict.SecondIndex].ToolId,
                xCandidate.Kind,
                yCandidate.Kind,
                preferredAxis,
                chosenAxis,
                checked(xGroups.Count + yGroups.Count)));
        }

        return LayoutProjectionFailure(
            LayoutProjectionResultKind.MergeLimitExceeded,
            acceptedMerges,
            passLimit,
            mergeTrace);
    }

    internal static AxisProjectionResult Project1D(
        AxisProjectionItem[] items,
        double boundaryStart,
        double boundaryEnd)
    {
        return Project1DCore(items, boundaryStart, boundaryEnd, allowPreferredFastPath: true);
    }

    private static AxisProjectionResult Project1DCore(
        AxisProjectionItem[] items,
        double boundaryStart,
        double boundaryEnd,
        bool allowPreferredFastPath)
    {
        if (items is null
            || !double.IsFinite(boundaryStart)
            || !double.IsFinite(boundaryEnd)
            || boundaryEnd < boundaryStart)
        {
            return AxisProjectionFailure(AxisProjectionResultKind.InvalidInput);
        }

        var uniqueToolIds = new HashSet<string>(StringComparer.Ordinal);
        var orderedItems = (AxisProjectionItem[])items.Clone();
        foreach (var item in orderedItems)
        {
            if (string.IsNullOrWhiteSpace(item.ToolId)
                || !uniqueToolIds.Add(item.ToolId)
                || !double.IsFinite(item.PreferredStart)
                || !double.IsFinite(item.Size)
                || item.Size <= 0)
            {
                return AxisProjectionFailure(AxisProjectionResultKind.InvalidInput);
            }

            var preferredCenter = item.PreferredStart + (item.Size / 2);
            var preferredEnd = item.PreferredStart + item.Size;
            if (!double.IsFinite(preferredCenter) || !double.IsFinite(preferredEnd))
            {
                return AxisProjectionFailure(AxisProjectionResultKind.NumericRangeExceeded);
            }
        }

        Array.Sort(
            orderedItems,
            static (first, second) =>
            {
                var firstCenter = first.PreferredStart + (first.Size / 2);
                var secondCenter = second.PreferredStart + (second.Size / 2);
                var centerComparison = firstCenter.CompareTo(secondCenter);
                return centerComparison != 0
                    ? centerComparison
                    : StringComparer.Ordinal.Compare(first.ToolId, second.ToolId);
            });

        if (allowPreferredFastPath
            && IsLegalPreferredAxisLayout(orderedItems, boundaryStart, boundaryEnd))
        {
            var preferredPositions = new AxisProjectionPosition[orderedItems.Length];
            for (var index = 0; index < orderedItems.Length; index++)
            {
                var item = orderedItems[index];
                preferredPositions[index] = new AxisProjectionPosition(
                    item.ToolId,
                    item.PreferredStart,
                    item.Size,
                    null);
            }

            return new AxisProjectionResult(
                AxisProjectionResultKind.PreferredFastPath,
                preferredPositions);
        }

        if (!TryCeilingTick(boundaryStart, out var boundaryStartTick)
            || !TryFloorTick(boundaryEnd, out var boundaryEndTick))
        {
            return AxisProjectionFailure(AxisProjectionResultKind.NumericRangeExceeded);
        }

        try
        {
            return ProjectFixed1D(orderedItems, boundaryStartTick, boundaryEndTick,
                boundaryStart, boundaryEnd);
        }
        catch (OverflowException)
        {
            return AxisProjectionFailure(AxisProjectionResultKind.NumericRangeExceeded);
        }
    }

    private static bool IsLegalPreferredLayout(
        LayoutProjectionItem[] items,
        double boundaryLeft,
        double boundaryTop,
        double boundaryRight,
        double boundaryBottom)
    {
        for (var index = 0; index < items.Length; index++)
        {
            var item = items[index];
            if (item.PreferredX < boundaryLeft
                || item.PreferredY < boundaryTop
                || item.PreferredX + item.Width > boundaryRight
                || item.PreferredY + item.Height > boundaryBottom)
            {
                return false;
            }

            for (var otherIndex = index + 1;
                 otherIndex < items.Length;
                 otherIndex++)
            {
                if (LayoutItemsOverlap(items[index], items[otherIndex]))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static List<AxisGroup> CreateSingletonGroups(int itemCount)
    {
        var groups = new List<AxisGroup>(itemCount);
        for (var index = 0; index < itemCount; index++)
        {
            groups.Add(new AxisGroup([index]));
        }

        return groups;
    }

    private static LayoutProjectionResultKind ProjectAxisGroups(
        LayoutProjectionItem[] items,
        List<AxisGroup> groups,
        LayoutProjectionAxis axis,
        double boundaryStart,
        double boundaryEnd,
        long[] resolvedTicks)
    {
        if (axis is not (LayoutProjectionAxis.X or LayoutProjectionAxis.Y)
            || resolvedTicks.Length != items.Length)
        {
            return LayoutProjectionResultKind.NumericInvariantViolation;
        }

        var assigned = new bool[items.Length];
        foreach (var group in groups)
        {
            var projectionItems = new AxisProjectionItem[group.Members.Length];
            for (var memberIndex = 0;
                 memberIndex < group.Members.Length;
                 memberIndex++)
            {
                var itemIndex = group.Members[memberIndex];
                if ((uint)itemIndex >= (uint)items.Length || assigned[itemIndex])
                {
                    return LayoutProjectionResultKind.NumericInvariantViolation;
                }

                var item = items[itemIndex];
                projectionItems[memberIndex] = new AxisProjectionItem(
                    item.ToolId,
                    axis == LayoutProjectionAxis.X
                        ? item.PreferredX
                        : item.PreferredY,
                    axis == LayoutProjectionAxis.X ? item.Width : item.Height);
            }

            var projection = Project1DCore(
                projectionItems,
                boundaryStart,
                boundaryEnd,
                allowPreferredFastPath: false);
            if (projection.Kind != AxisProjectionResultKind.FixedProjectedSuccess)
            {
                return MapAxisProjectionFailure(
                    projection.Kind,
                    projectionItems,
                    boundaryStart,
                    boundaryEnd);
            }

            if (projection.Positions.Length != group.Members.Length)
            {
                return LayoutProjectionResultKind.NumericInvariantViolation;
            }

            foreach (var position in projection.Positions)
            {
                var itemIndex = FindToolIndex(items, position.ToolId);
                if (itemIndex < 0
                    || assigned[itemIndex]
                    || position.FixedStartTick is not { } fixedStartTick)
                {
                    return LayoutProjectionResultKind.NumericInvariantViolation;
                }

                assigned[itemIndex] = true;
                resolvedTicks[itemIndex] = fixedStartTick;
            }
        }

        for (var index = 0; index < assigned.Length; index++)
        {
            if (!assigned[index])
            {
                return LayoutProjectionResultKind.NumericInvariantViolation;
            }
        }

        return LayoutProjectionResultKind.ProjectedSuccess;
    }

    private static LayoutProjectionResultKind MapAxisProjectionFailure(
        AxisProjectionResultKind kind,
        AxisProjectionItem[] items,
        double boundaryStart,
        double boundaryEnd)
    {
        if (kind == AxisProjectionResultKind.FixedGridInfeasible)
        {
            var totalSize = 0d;
            foreach (var item in items)
            {
                totalSize += item.Size;
            }

            if (!double.IsFinite(totalSize))
            {
                return LayoutProjectionResultKind.NumericRangeExceeded;
            }

            return totalSize > boundaryEnd - boundaryStart
                ? LayoutProjectionResultKind.GeometricInfeasible
                : LayoutProjectionResultKind.FixedGridInfeasible;
        }

        return kind switch
        {
            AxisProjectionResultKind.NumericRangeExceeded =>
                LayoutProjectionResultKind.NumericRangeExceeded,
            AxisProjectionResultKind.NumericInvariantViolation =>
                LayoutProjectionResultKind.NumericInvariantViolation,
            _ => LayoutProjectionResultKind.NumericInvariantViolation
        };
    }

    private static LayoutCandidate SimulateCandidate(
        LayoutProjectionItem[] items,
        List<AxisGroup> currentGroups,
        LayoutProjectionAxis axis,
        int firstGroupIndex,
        int secondGroupIndex,
        double boundaryStart,
        double boundaryEnd,
        long[] currentXTicks,
        long[] currentYTicks,
        long[] preferredXTicks,
        long[] preferredYTicks)
    {
        if (firstGroupIndex == secondGroupIndex)
        {
            return LayoutCandidate.NotAvailable;
        }

        var candidateGroups = MergeGroups(
            currentGroups,
            firstGroupIndex,
            secondGroupIndex);
        var candidateXTicks = (long[])currentXTicks.Clone();
        var candidateYTicks = (long[])currentYTicks.Clone();
        var projectionKind = ProjectAxisGroups(
            items,
            candidateGroups,
            axis,
            boundaryStart,
            boundaryEnd,
            axis == LayoutProjectionAxis.X
                ? candidateXTicks
                : candidateYTicks);

        if (projectionKind == LayoutProjectionResultKind.GeometricInfeasible)
        {
            return LayoutCandidate.GeometricInfeasible;
        }

        if (projectionKind == LayoutProjectionResultKind.FixedGridInfeasible)
        {
            return LayoutCandidate.FixedGridInfeasible;
        }

        if (projectionKind != LayoutProjectionResultKind.ProjectedSuccess)
        {
            return new LayoutCandidate(
                LayoutProjectionCandidateKind.NotAvailable,
                0,
                0,
                projectionKind);
        }

        if (!TryComputeMovementCost(
                candidateXTicks,
                candidateYTicks,
                preferredXTicks,
                preferredYTicks,
                out var totalSquaredMovement,
                out var maximumIndividualDisplacement))
        {
            return new LayoutCandidate(
                LayoutProjectionCandidateKind.NotAvailable,
                0,
                0,
                LayoutProjectionResultKind.NumericRangeExceeded);
        }

        return new LayoutCandidate(
            LayoutProjectionCandidateKind.Feasible,
            totalSquaredMovement,
            maximumIndividualDisplacement,
            null);
    }

    private static bool TryComputeMovementCost(
        long[] resolvedXTicks,
        long[] resolvedYTicks,
        long[] preferredXTicks,
        long[] preferredYTicks,
        out Int128 totalSquaredMovement,
        out Int128 maximumIndividualDisplacement)
    {
        totalSquaredMovement = 0;
        maximumIndividualDisplacement = 0;
        if (resolvedXTicks.Length != resolvedYTicks.Length
            || resolvedXTicks.Length != preferredXTicks.Length
            || resolvedXTicks.Length != preferredYTicks.Length)
        {
            return false;
        }

        try
        {
            for (var index = 0; index < resolvedXTicks.Length; index++)
            {
                var deltaX = (Int128)resolvedXTicks[index] - preferredXTicks[index];
                var deltaY = (Int128)resolvedYTicks[index] - preferredYTicks[index];
                var individualDisplacement = checked(
                    checked(deltaX * deltaX) + checked(deltaY * deltaY));
                totalSquaredMovement = checked(
                    totalSquaredMovement + individualDisplacement);
                maximumIndividualDisplacement = Int128.Max(
                    maximumIndividualDisplacement,
                    individualDisplacement);
            }

            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    private static LayoutProjectionAxis GetPreferredAxis(
        LayoutProjectionItem first,
        LayoutProjectionItem second)
    {
        var firstCenterX = first.PreferredX + (first.Width / 2);
        var firstCenterY = first.PreferredY + (first.Height / 2);
        var secondCenterX = second.PreferredX + (second.Width / 2);
        var secondCenterY = second.PreferredY + (second.Height / 2);
        var deltaX = Math.Abs(firstCenterX - secondCenterX);
        var deltaY = Math.Abs(firstCenterY - secondCenterY);
        if (!double.IsFinite(deltaX) || !double.IsFinite(deltaY))
        {
            return LayoutProjectionAxis.None;
        }

        if (deltaX > (deltaY * AxisDominanceRatio) + RelationNumericalTolerance)
        {
            return LayoutProjectionAxis.X;
        }

        if (deltaY > (deltaX * AxisDominanceRatio) + RelationNumericalTolerance)
        {
            return LayoutProjectionAxis.Y;
        }

        return LayoutProjectionAxis.Ambiguous;
    }

    private static LayoutProjectionAxis ChooseAxis(
        LayoutProjectionAxis preferredAxis,
        LayoutCandidate xCandidate,
        LayoutCandidate yCandidate)
    {
        if (xCandidate.IsFeasible && !yCandidate.IsFeasible)
        {
            return LayoutProjectionAxis.X;
        }

        if (yCandidate.IsFeasible && !xCandidate.IsFeasible)
        {
            return LayoutProjectionAxis.Y;
        }

        if (!xCandidate.IsFeasible || !yCandidate.IsFeasible)
        {
            return LayoutProjectionAxis.None;
        }

        if (preferredAxis == LayoutProjectionAxis.X)
        {
            return LayoutProjectionAxis.X;
        }

        if (preferredAxis == LayoutProjectionAxis.Y)
        {
            return LayoutProjectionAxis.Y;
        }

        var totalComparison = xCandidate.TotalSquaredMovement.CompareTo(
            yCandidate.TotalSquaredMovement);
        if (totalComparison != 0)
        {
            return totalComparison < 0
                ? LayoutProjectionAxis.X
                : LayoutProjectionAxis.Y;
        }

        var maximumComparison = xCandidate.MaximumIndividualDisplacement.CompareTo(
            yCandidate.MaximumIndividualDisplacement);
        if (maximumComparison != 0)
        {
            return maximumComparison < 0
                ? LayoutProjectionAxis.X
                : LayoutProjectionAxis.Y;
        }

        return LayoutProjectionAxis.X;
    }

    private static List<AxisGroup> MergeGroups(
        List<AxisGroup> groups,
        int firstGroupIndex,
        int secondGroupIndex)
    {
        if (firstGroupIndex == secondGroupIndex
            || (uint)firstGroupIndex >= (uint)groups.Count
            || (uint)secondGroupIndex >= (uint)groups.Count)
        {
            throw new InvalidOperationException("Axis group merge must reduce group count.");
        }

        var firstMembers = groups[firstGroupIndex].Members;
        var secondMembers = groups[secondGroupIndex].Members;
        var mergedMembers = new int[firstMembers.Length + secondMembers.Length];
        firstMembers.CopyTo(mergedMembers, 0);
        secondMembers.CopyTo(mergedMembers, firstMembers.Length);
        Array.Sort(mergedMembers);

        var mergedGroups = new List<AxisGroup>(groups.Count - 1);
        for (var index = 0; index < groups.Count; index++)
        {
            if (index != firstGroupIndex && index != secondGroupIndex)
            {
                mergedGroups.Add(groups[index]);
            }
        }

        mergedGroups.Add(new AxisGroup(mergedMembers));
        mergedGroups.Sort(static (first, second) =>
            first.Members[0].CompareTo(second.Members[0]));
        return mergedGroups;
    }

    private static int FindGroupIndex(List<AxisGroup> groups, int itemIndex)
    {
        for (var groupIndex = 0; groupIndex < groups.Count; groupIndex++)
        {
            if (Array.BinarySearch(groups[groupIndex].Members, itemIndex) >= 0)
            {
                return groupIndex;
            }
        }

        return -1;
    }

    private static int FindToolIndex(
        LayoutProjectionItem[] items,
        string toolId)
    {
        var low = 0;
        var high = items.Length - 1;
        while (low <= high)
        {
            var middle = low + ((high - low) / 2);
            var comparison = StringComparer.Ordinal.Compare(
                items[middle].ToolId,
                toolId);
            if (comparison == 0)
            {
                return middle;
            }

            if (comparison < 0)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return -1;
    }

    private static LayoutConflictEdge[] BuildConflictEdges(
        long[] resolvedXTicks,
        long[] resolvedYTicks,
        long[] widthTicks,
        long[] heightTicks)
    {
        var edges = new List<LayoutConflictEdge>();
        for (var firstIndex = 0;
             firstIndex < resolvedXTicks.Length;
             firstIndex++)
        {
            var firstRight = checked(
                resolvedXTicks[firstIndex] + widthTicks[firstIndex]);
            var firstBottom = checked(
                resolvedYTicks[firstIndex] + heightTicks[firstIndex]);
            for (var secondIndex = firstIndex + 1;
                 secondIndex < resolvedXTicks.Length;
                 secondIndex++)
            {
                var secondRight = checked(
                    resolvedXTicks[secondIndex] + widthTicks[secondIndex]);
                var secondBottom = checked(
                    resolvedYTicks[secondIndex] + heightTicks[secondIndex]);
                if (resolvedXTicks[firstIndex] < secondRight
                    && firstRight > resolvedXTicks[secondIndex]
                    && resolvedYTicks[firstIndex] < secondBottom
                    && firstBottom > resolvedYTicks[secondIndex])
                {
                    edges.Add(new LayoutConflictEdge(firstIndex, secondIndex));
                }
            }
        }

        return [.. edges];
    }

    private static LayoutConflictEdge SelectFirstComponentEdge(
        int itemCount,
        LayoutConflictEdge[] edges)
    {
        var parents = new int[itemCount];
        for (var index = 0; index < parents.Length; index++)
        {
            parents[index] = index;
        }

        foreach (var edge in edges)
        {
            UnionConflictComponents(parents, edge.FirstIndex, edge.SecondIndex);
        }

        var firstComponent = itemCount;
        foreach (var edge in edges)
        {
            firstComponent = Math.Min(
                firstComponent,
                FindConflictComponent(parents, edge.FirstIndex));
        }

        foreach (var edge in edges)
        {
            if (FindConflictComponent(parents, edge.FirstIndex) == firstComponent)
            {
                return edge;
            }
        }

        throw new InvalidOperationException("Conflict graph must contain an edge.");
    }

    private static void UnionConflictComponents(
        int[] parents,
        int firstIndex,
        int secondIndex)
    {
        var firstRoot = FindConflictComponent(parents, firstIndex);
        var secondRoot = FindConflictComponent(parents, secondIndex);
        if (firstRoot == secondRoot)
        {
            return;
        }

        if (firstRoot < secondRoot)
        {
            parents[secondRoot] = firstRoot;
        }
        else
        {
            parents[firstRoot] = secondRoot;
        }
    }

    private static int FindConflictComponent(int[] parents, int index)
    {
        while (parents[index] != index)
        {
            parents[index] = parents[parents[index]];
            index = parents[index];
        }

        return index;
    }

    private static LayoutProjectionResult CreateSuccessfulProjectedLayout(
        LayoutProjectionItem[] items,
        long[] resolvedXTicks,
        long[] resolvedYTicks,
        long[] widthTicks,
        long[] heightTicks,
        long boundaryLeftTick,
        long boundaryTopTick,
        long boundaryRightTick,
        long boundaryBottomTick,
        double boundaryLeft,
        double boundaryTop,
        double boundaryRight,
        double boundaryBottom,
        int acceptedMerges,
        int passCount,
        List<LayoutProjectionTraceStep> mergeTrace)
    {
        var positions = new LayoutProjectionPosition[items.Length];
        try
        {
            for (var index = 0; index < items.Length; index++)
            {
                var rightTick = checked(resolvedXTicks[index] + widthTicks[index]);
                var bottomTick = checked(resolvedYTicks[index] + heightTicks[index]);
                if (!IsExactlyRepresentableTick(resolvedXTicks[index])
                    || !IsExactlyRepresentableTick(resolvedYTicks[index])
                    || !IsExactlyRepresentableTick(rightTick)
                    || !IsExactlyRepresentableTick(bottomTick)
                    || resolvedXTicks[index] < boundaryLeftTick
                    || resolvedYTicks[index] < boundaryTopTick
                    || rightTick > boundaryRightTick
                    || bottomTick > boundaryBottomTick)
                {
                    return LayoutProjectionFailure(
                        LayoutProjectionResultKind.NumericInvariantViolation,
                        acceptedMerges,
                        passCount,
                        mergeTrace);
                }

                var resolvedX = resolvedXTicks[index] / (double)TicksPerDip;
                var resolvedY = resolvedYTicks[index] / (double)TicksPerDip;
                var actualRight = resolvedX + items[index].Width;
                var actualBottom = resolvedY + items[index].Height;
                if (!double.IsFinite(resolvedX)
                    || !double.IsFinite(resolvedY)
                    || !double.IsFinite(actualRight)
                    || !double.IsFinite(actualBottom)
                    || resolvedX < boundaryLeft
                    || resolvedY < boundaryTop
                    || actualRight > boundaryRight
                    || actualBottom > boundaryBottom)
                {
                    return LayoutProjectionFailure(
                        LayoutProjectionResultKind.NumericInvariantViolation,
                        acceptedMerges,
                        passCount,
                        mergeTrace);
                }

                positions[index] = new LayoutProjectionPosition(
                    items[index].ToolId,
                    resolvedX,
                    resolvedY,
                    resolvedXTicks[index],
                    resolvedYTicks[index]);
            }

            for (var firstIndex = 0;
                 firstIndex < positions.Length;
                 firstIndex++)
            {
                var firstRightTick = checked(
                    resolvedXTicks[firstIndex] + widthTicks[firstIndex]);
                var firstBottomTick = checked(
                    resolvedYTicks[firstIndex] + heightTicks[firstIndex]);
                for (var secondIndex = firstIndex + 1;
                     secondIndex < positions.Length;
                     secondIndex++)
                {
                    var secondRightTick = checked(
                        resolvedXTicks[secondIndex] + widthTicks[secondIndex]);
                    var secondBottomTick = checked(
                        resolvedYTicks[secondIndex] + heightTicks[secondIndex]);
                    var fixedOverlap = resolvedXTicks[firstIndex] < secondRightTick
                        && firstRightTick > resolvedXTicks[secondIndex]
                        && resolvedYTicks[firstIndex] < secondBottomTick
                        && firstBottomTick > resolvedYTicks[secondIndex];
                    var actualOverlap = positions[firstIndex].ResolvedX
                            < positions[secondIndex].ResolvedX
                            + items[secondIndex].Width
                        && positions[firstIndex].ResolvedX + items[firstIndex].Width
                            > positions[secondIndex].ResolvedX
                        && positions[firstIndex].ResolvedY
                            < positions[secondIndex].ResolvedY
                            + items[secondIndex].Height
                        && positions[firstIndex].ResolvedY + items[firstIndex].Height
                            > positions[secondIndex].ResolvedY;
                    if (fixedOverlap || actualOverlap)
                    {
                        return LayoutProjectionFailure(
                            LayoutProjectionResultKind.NumericInvariantViolation,
                            acceptedMerges,
                            passCount,
                            mergeTrace);
                    }
                }
            }
        }
        catch (OverflowException)
        {
            return LayoutProjectionFailure(
                LayoutProjectionResultKind.NumericRangeExceeded,
                acceptedMerges,
                passCount,
                mergeTrace);
        }

        return new LayoutProjectionResult(
            LayoutProjectionResultKind.ProjectedSuccess,
            positions,
            acceptedMerges,
            passCount,
            [.. mergeTrace]);
    }

    private static LayoutProjectionResult LayoutProjectionFailure(
        LayoutProjectionResultKind kind,
        int acceptedMerges = 0,
        int passCount = 0,
        List<LayoutProjectionTraceStep>? mergeTrace = null)
    {
        return new LayoutProjectionResult(
            kind,
            [],
            acceptedMerges,
            passCount,
            mergeTrace is null ? [] : [.. mergeTrace]);
    }

    private static bool LayoutItemsOverlap(
        LayoutProjectionItem first,
        LayoutProjectionItem second)
    {
        return first.PreferredX < second.PreferredX + second.Width
            && first.PreferredX + first.Width > second.PreferredX
            && first.PreferredY < second.PreferredY + second.Height
            && first.PreferredY + first.Height > second.PreferredY;
    }

    private static AxisProjectionResult ProjectFixed1D(
        AxisProjectionItem[] orderedItems,
        long boundaryStartTick,
        long boundaryEndTick,
        double originalBoundaryStart,
        double originalBoundaryEnd)
    {
        var itemCount = orderedItems.Length;
        var sizeTicks = new long[itemCount];
        var preferredTicks = new long[itemCount];
        for (var index = 0; index < itemCount; index++)
        {
            if (!TryCeilingTick(orderedItems[index].Size, out sizeTicks[index])
                || sizeTicks[index] <= 0
                || !TryNearestEvenTick(
                    orderedItems[index].PreferredStart,
                    out preferredTicks[index]))
            {
                return AxisProjectionFailure(AxisProjectionResultKind.NumericRangeExceeded);
            }
        }

        var offsets = new long[itemCount];
        var totalSizeTick = 0L;
        for (var index = 0; index < itemCount; index++)
        {
            offsets[index] = totalSizeTick;
            totalSizeTick = checked(totalSizeTick + sizeTicks[index]);
        }

        var axisSpanTick = checked(boundaryEndTick - boundaryStartTick);
        if (totalSizeTick > axisSpanTick)
        {
            return AxisProjectionFailure(AxisProjectionResultKind.FixedGridInfeasible);
        }

        var lowerTick = boundaryStartTick;
        var upperTick = checked(boundaryEndTick - totalSizeTick);
        var blocks = new AxisProjectionBlock[itemCount];
        var blockCount = 0;
        for (var index = 0; index < itemCount; index++)
        {
            var targetTick = checked(preferredTicks[index] - offsets[index]);
            blocks[blockCount++] = new AxisProjectionBlock(
                index,
                index,
                targetTick,
                1);

            while (blockCount >= 2
                   && CompareBlockMeans(
                       blocks[blockCount - 2],
                       blocks[blockCount - 1]) > 0)
            {
                var previous = blocks[blockCount - 2];
                var current = blocks[blockCount - 1];
                blocks[blockCount - 2] = new AxisProjectionBlock(
                    previous.FirstIndex,
                    current.LastIndex,
                    checked(previous.SumTicks + current.SumTicks),
                    checked(previous.Weight + current.Weight));
                blockCount--;
            }
        }

        var projectedTicks = new long[itemCount];
        long? previousProjectedTick = null;
        for (var blockIndex = 0; blockIndex < blockCount; blockIndex++)
        {
            var block = blocks[blockIndex];
            long projectedTick;
            if (CompareBlockMeanToTick(block, lowerTick) < 0)
            {
                projectedTick = lowerTick;
            }
            else if (CompareBlockMeanToTick(block, upperTick) > 0)
            {
                projectedTick = upperTick;
            }
            else
            {
                projectedTick = RationalNearestEven(block.SumTicks, block.Weight);
            }

            if (previousProjectedTick is { } previous
                && projectedTick < previous)
            {
                return AxisProjectionFailure(
                    AxisProjectionResultKind.NumericInvariantViolation);
            }

            for (var itemIndex = block.FirstIndex;
                 itemIndex <= block.LastIndex;
                 itemIndex++)
            {
                projectedTicks[itemIndex] = projectedTick;
            }

            previousProjectedTick = projectedTick;
        }

        var startTicks = new long[itemCount];
        var endTicks = new long[itemCount];
        for (var index = 0; index < itemCount; index++)
        {
            startTicks[index] = checked(projectedTicks[index] + offsets[index]);
            endTicks[index] = checked(startTicks[index] + sizeTicks[index]);
            if (!IsExactlyRepresentableTick(startTicks[index])
                || !IsExactlyRepresentableTick(endTicks[index])
                || startTicks[index] < boundaryStartTick
                || endTicks[index] > boundaryEndTick
                || (index > 0 && startTicks[index] < endTicks[index - 1]))
            {
                return AxisProjectionFailure(
                    AxisProjectionResultKind.NumericInvariantViolation);
            }
        }

        var resolvedPositions = new AxisProjectionPosition[itemCount];
        for (var index = 0; index < itemCount; index++)
        {
            var start = startTicks[index] / (double)TicksPerDip;
            var actualEnd = start + orderedItems[index].Size;
            if (!double.IsFinite(start)
                || !double.IsFinite(actualEnd)
                || start < originalBoundaryStart
                || actualEnd > originalBoundaryEnd
                || (index > 0
                    && start
                    < resolvedPositions[index - 1].Start
                    + resolvedPositions[index - 1].Size))
            {
                return AxisProjectionFailure(
                    AxisProjectionResultKind.NumericInvariantViolation);
            }

            resolvedPositions[index] = new AxisProjectionPosition(
                orderedItems[index].ToolId,
                start,
                orderedItems[index].Size,
                startTicks[index]);
        }

        return new AxisProjectionResult(
            AxisProjectionResultKind.FixedProjectedSuccess,
            resolvedPositions);
    }

    private static bool IsLegalPreferredAxisLayout(
        AxisProjectionItem[] orderedItems,
        double boundaryStart,
        double boundaryEnd)
    {
        for (var index = 0; index < orderedItems.Length; index++)
        {
            var item = orderedItems[index];
            var end = item.PreferredStart + item.Size;
            if (item.PreferredStart < boundaryStart
                || end > boundaryEnd
                || (index > 0
                    && item.PreferredStart
                    < orderedItems[index - 1].PreferredStart
                    + orderedItems[index - 1].Size))
            {
                return false;
            }
        }

        return true;
    }

    private static AxisProjectionResult AxisProjectionFailure(
        AxisProjectionResultKind kind)
    {
        return new AxisProjectionResult(kind, []);
    }

    private static int CompareBlockMeans(
        AxisProjectionBlock first,
        AxisProjectionBlock second)
    {
        var left = (Int128)first.SumTicks * second.Weight;
        var right = (Int128)second.SumTicks * first.Weight;
        return left.CompareTo(right);
    }

    private static int CompareBlockMeanToTick(
        AxisProjectionBlock block,
        long tick)
    {
        var left = (Int128)block.SumTicks;
        var right = (Int128)tick * block.Weight;
        return left.CompareTo(right);
    }

    private static long RationalNearestEven(long sumTicks, int weight)
    {
        if (weight <= 0)
        {
            throw new InvalidOperationException("PAV block weight must be positive.");
        }

        var quotient = sumTicks / weight;
        var remainder = sumTicks % weight;
        var doubledRemainderMagnitude = Math.Abs((long)remainder) * 2;
        if (doubledRemainderMagnitude < weight)
        {
            return quotient;
        }

        var direction = Math.Sign(sumTicks);
        if (doubledRemainderMagnitude > weight)
        {
            return checked(quotient + direction);
        }

        return (quotient & 1) == 0
            ? quotient
            : checked(quotient + direction);
    }

    private static bool TryCeilingTick(double value, out long tick)
    {
        return TryScaledTick(Math.Ceiling(value * TicksPerDip), out tick);
    }

    private static bool TryFloorTick(double value, out long tick)
    {
        return TryScaledTick(Math.Floor(value * TicksPerDip), out tick);
    }

    private static bool TryNearestEvenTick(double value, out long tick)
    {
        return TryScaledTick(
            Math.Round(value * TicksPerDip, MidpointRounding.ToEven),
            out tick);
    }

    private static bool TryScaledTick(double scaledValue, out long tick)
    {
        if (!double.IsFinite(scaledValue)
            || scaledValue < -MaximumExactDoubleTick
            || scaledValue > MaximumExactDoubleTick)
        {
            tick = 0;
            return false;
        }

        tick = (long)scaledValue;
        return true;
    }

    private static bool IsExactlyRepresentableTick(long tick)
    {
        return tick >= -MaximumExactDoubleTick
            && tick <= MaximumExactDoubleTick;
    }

    private static void ClampToolsToBounds(
        Span<Rect> tools,
        SoftWorkspaceBounds bounds)
    {
        for (var index = 0; index < tools.Length; index++)
        {
            var tool = tools[index];
            tool.X = Math.Clamp(tool.X, bounds.MinimumX, bounds.MaximumX);
            tool.Y = Math.Clamp(tool.Y, bounds.MinimumY, bounds.MaximumY);
            tools[index] = tool;
        }
    }

    private static bool IsWithinBounds(Point position, SoftWorkspaceBounds bounds)
    {
        return position.X >= bounds.MinimumX
            && position.X <= bounds.MaximumX
            && position.Y >= bounds.MinimumY
            && position.Y <= bounds.MaximumY;
    }

    private static bool ResolveResizeOverlap(
        Span<Rect> tools,
        int firstIndex,
        int secondIndex,
        SoftWorkspaceBounds bounds,
        WorkspaceBoundary inwardBoundaries)
    {
        var first = tools[firstIndex];
        var second = tools[secondIndex];
        var overlapX = Math.Min(first.Right, second.Right) - Math.Max(first.Left, second.Left);
        var overlapY = Math.Min(first.Bottom, second.Bottom) - Math.Max(first.Top, second.Top);
        var hasHorizontalPressure =
            (inwardBoundaries & (WorkspaceBoundary.Left | WorkspaceBoundary.Right)) != 0;
        var hasVerticalPressure =
            (inwardBoundaries & (WorkspaceBoundary.Top | WorkspaceBoundary.Bottom)) != 0;
        var resolveHorizontally = hasHorizontalPressure
            && (!hasVerticalPressure || overlapX <= overlapY);

        if (resolveHorizontally)
        {
            return ResolveHorizontalResizeOverlap(
                tools,
                firstIndex,
                secondIndex,
                bounds,
                inwardBoundaries);
        }

        if (hasVerticalPressure)
        {
            return ResolveVerticalResizeOverlap(
                tools,
                firstIndex,
                secondIndex,
                bounds,
                inwardBoundaries);
        }

        return false;
    }

    private static bool ResolveHorizontalResizeOverlap(
        Span<Rect> tools,
        int firstIndex,
        int secondIndex,
        SoftWorkspaceBounds bounds,
        WorkspaceBoundary inwardBoundaries)
    {
        var first = tools[firstIndex];
        var second = tools[secondIndex];
        var pressureFromLeft = (inwardBoundaries & WorkspaceBoundary.Left) != 0;
        var pressureFromRight = (inwardBoundaries & WorkspaceBoundary.Right) != 0;
        if (pressureFromLeft && pressureFromRight)
        {
            var pairCenter = (Math.Min(first.Left, second.Left) + Math.Max(first.Right, second.Right)) / 2;
            pressureFromLeft = pairCenter <= (bounds.MinimumX + bounds.MaximumX + first.Width) / 2;
            pressureFromRight = !pressureFromLeft;
        }

        if (pressureFromLeft)
        {
            var leftIndex = first.Left <= second.Left ? firstIndex : secondIndex;
            var rightIndex = leftIndex == firstIndex ? secondIndex : firstIndex;
            var leftTool = tools[leftIndex];
            var rightTool = tools[rightIndex];
            rightTool.X = leftTool.Right;
            if (rightTool.X > bounds.MaximumX)
            {
                return false;
            }

            tools[rightIndex] = rightTool;
            return true;
        }

        if (pressureFromRight)
        {
            var rightIndex = first.Right >= second.Right ? firstIndex : secondIndex;
            var leftIndex = rightIndex == firstIndex ? secondIndex : firstIndex;
            var rightTool = tools[rightIndex];
            var leftTool = tools[leftIndex];
            leftTool.X = rightTool.Left - leftTool.Width;
            if (leftTool.X < bounds.MinimumX)
            {
                return false;
            }

            tools[leftIndex] = leftTool;
            return true;
        }

        return false;
    }

    private static bool ResolveVerticalResizeOverlap(
        Span<Rect> tools,
        int firstIndex,
        int secondIndex,
        SoftWorkspaceBounds bounds,
        WorkspaceBoundary inwardBoundaries)
    {
        var first = tools[firstIndex];
        var second = tools[secondIndex];
        var pressureFromTop = (inwardBoundaries & WorkspaceBoundary.Top) != 0;
        var pressureFromBottom = (inwardBoundaries & WorkspaceBoundary.Bottom) != 0;
        if (pressureFromTop && pressureFromBottom)
        {
            var pairCenter = (Math.Min(first.Top, second.Top) + Math.Max(first.Bottom, second.Bottom)) / 2;
            pressureFromTop = pairCenter <= (bounds.MinimumY + bounds.MaximumY + first.Height) / 2;
            pressureFromBottom = !pressureFromTop;
        }

        if (pressureFromTop)
        {
            var topIndex = first.Top <= second.Top ? firstIndex : secondIndex;
            var bottomIndex = topIndex == firstIndex ? secondIndex : firstIndex;
            var topTool = tools[topIndex];
            var bottomTool = tools[bottomIndex];
            bottomTool.Y = topTool.Bottom;
            if (bottomTool.Y > bounds.MaximumY)
            {
                return false;
            }

            tools[bottomIndex] = bottomTool;
            return true;
        }

        if (pressureFromBottom)
        {
            var bottomIndex = first.Bottom >= second.Bottom ? firstIndex : secondIndex;
            var topIndex = bottomIndex == firstIndex ? secondIndex : firstIndex;
            var bottomTool = tools[bottomIndex];
            var topTool = tools[topIndex];
            topTool.Y = bottomTool.Top - topTool.Height;
            if (topTool.Y < bounds.MinimumY)
            {
                return false;
            }

            tools[topIndex] = topTool;
            return true;
        }

        return false;
    }

    private static bool RectanglesOverlap(Rect first, Rect second)
    {
        return first.Left < second.Right
            && first.Right > second.Left
            && first.Top < second.Bottom
            && first.Bottom > second.Top;
    }

    private static double GetRequiredAxisSpan(ReadOnlySpan<Rect> tools, bool horizontal)
    {
        var order = new int[tools.Length];
        var endDistances = new double[tools.Length];
        for (var index = 0; index < order.Length; index++)
        {
            order[index] = index;
        }

        for (var position = 0; position < order.Length - 1; position++)
        {
            var smallestPosition = position;
            for (var candidate = position + 1; candidate < order.Length; candidate++)
            {
                var candidateCoordinate = horizontal
                    ? tools[order[candidate]].Left
                    : tools[order[candidate]].Top;
                var smallestCoordinate = horizontal
                    ? tools[order[smallestPosition]].Left
                    : tools[order[smallestPosition]].Top;
                if (candidateCoordinate < smallestCoordinate)
                {
                    smallestPosition = candidate;
                }
            }

            (order[position], order[smallestPosition]) =
                (order[smallestPosition], order[position]);
        }

        var requiredSpan = 0d;
        for (var position = 0; position < order.Length; position++)
        {
            var currentIndex = order[position];
            var current = tools[currentIndex];
            var startDistance = 0d;
            for (var previousPosition = 0; previousPosition < position; previousPosition++)
            {
                var previousIndex = order[previousPosition];
                var previous = tools[previousIndex];
                var perpendicularOverlap = horizontal
                    ? previous.Top < current.Bottom && previous.Bottom > current.Top
                    : previous.Left < current.Right && previous.Right > current.Left;
                if (perpendicularOverlap)
                {
                    startDistance = Math.Max(startDistance, endDistances[previousIndex]);
                }
            }

            var length = horizontal ? current.Width : current.Height;
            endDistances[currentIndex] = startDistance + length;
            requiredSpan = Math.Max(requiredSpan, endDistances[currentIndex]);
        }

        return requiredSpan;
    }

    private readonly record struct AxisGroup(int[] Members);

    private readonly record struct LayoutConflictEdge(
        int FirstIndex,
        int SecondIndex);

    private readonly record struct LayoutCandidate(
        LayoutProjectionCandidateKind Kind,
        Int128 TotalSquaredMovement,
        Int128 MaximumIndividualDisplacement,
        LayoutProjectionResultKind? FatalKind)
    {
        internal bool IsFeasible => Kind == LayoutProjectionCandidateKind.Feasible;

        internal static LayoutCandidate NotAvailable => new(
            LayoutProjectionCandidateKind.NotAvailable,
            0,
            0,
            null);

        internal static LayoutCandidate GeometricInfeasible => new(
            LayoutProjectionCandidateKind.GeometricInfeasible,
            0,
            0,
            null);

        internal static LayoutCandidate FixedGridInfeasible => new(
            LayoutProjectionCandidateKind.FixedGridInfeasible,
            0,
            0,
            null);
    }

    private readonly record struct AxisProjectionBlock(
        int FirstIndex,
        int LastIndex,
        long SumTicks,
        int Weight);
}
