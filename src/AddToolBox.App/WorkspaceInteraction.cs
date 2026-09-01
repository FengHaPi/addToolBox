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

internal static class WorkspaceInteraction
{
    internal const double SoftBoundaryPadding = 18;

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

    internal static WorkspaceBoundary GetBoundaryContacts(
        Point desiredPosition,
        SoftWorkspaceBounds bounds)
    {
        var contacts = WorkspaceBoundary.None;
        if (desiredPosition.X < bounds.MinimumX)
        {
            contacts |= WorkspaceBoundary.Left;
        }
        else if (desiredPosition.X > bounds.MaximumX)
        {
            contacts |= WorkspaceBoundary.Right;
        }

        if (desiredPosition.Y < bounds.MinimumY)
        {
            contacts |= WorkspaceBoundary.Top;
        }
        else if (desiredPosition.Y > bounds.MaximumY)
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
}
