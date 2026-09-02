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

internal static class WorkspaceInteraction
{
    internal const long TicksPerDip = 1024;
    internal const double SoftBoundaryPadding = 18;
    internal const double BoundaryContactEpsilon = 0.75;

    private const long MaximumExactDoubleTick = 1L << 53;

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

    internal static WorkspaceBoundary GetPressedBoundaryContacts(
        Point actualResolvedPosition,
        Point rawDesiredPosition,
        SoftWorkspaceBounds bounds)
    {
        var contacts = WorkspaceBoundary.None;
        if (actualResolvedPosition.X <= bounds.MinimumX + BoundaryContactEpsilon
            && rawDesiredPosition.X < bounds.MinimumX)
        {
            contacts |= WorkspaceBoundary.Left;
        }
        else if (actualResolvedPosition.X >= bounds.MaximumX - BoundaryContactEpsilon
                 && rawDesiredPosition.X > bounds.MaximumX)
        {
            contacts |= WorkspaceBoundary.Right;
        }

        if (actualResolvedPosition.Y <= bounds.MinimumY + BoundaryContactEpsilon
            && rawDesiredPosition.Y < bounds.MinimumY)
        {
            contacts |= WorkspaceBoundary.Top;
        }
        else if (actualResolvedPosition.Y >= bounds.MaximumY - BoundaryContactEpsilon
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

    internal static AxisProjectionResult Project1D(
        AxisProjectionItem[] items,
        double boundaryStart,
        double boundaryEnd)
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

        if (IsLegalPreferredAxisLayout(orderedItems, boundaryStart, boundaryEnd))
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

    private readonly record struct AxisProjectionBlock(
        int FirstIndex,
        int LastIndex,
        long SumTicks,
        int Weight);
}
