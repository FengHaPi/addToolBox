using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace AddToolBox.App;

public partial class MainWindow : Window
{
    private const double DragHighlightOpacity = 0.14;
    private const double PickupLiftOffset = -3;
    private const double PickupScale = 1.04;
    private const double CollisionDisplacement = 2.5;
    private const double CollisionRotationDegrees = 0.65;
    private const double SoftCollisionFullPressure = 28;
    private const double DraggedToolMinimumCollisionScale = 0.90;
    private const double DraggedToolMaximumCollisionBulge = 1.065;
    private const double BlockedToolMinimumCollisionScale = 0.955;
    private const double BlockedToolMaximumCollisionBulge = 1.035;
    private const double IconCollisionShapeInheritance = 0.28;
    private const double LongPressMoveTolerance = 5;
    private const int CollisionImpactMilliseconds = 50;
    private const int CollisionFeedbackMilliseconds = 180;
    private const double ToolHoverOpacity = 0.58;
    private const double ToolHoverOffset = -1;
    private const int DragTransitionMilliseconds = 120;
    private const int PickupTransitionMilliseconds = 140;
    private const int PickupShakeMilliseconds = 200;
    private const int PickupParticleMinimumCount = 24;
    private const int PickupParticleMaximumCount = 36;
    private const int PickupParticleMinimumLifetimeMilliseconds = 350;
    private const int PickupParticleMaximumLifetimeMilliseconds = 650;
    private const int CollisionParticleMinimumCount = 8;
    private const int CollisionParticleMaximumCount = 16;
    private const int CollisionParticleMinimumLifetimeMilliseconds = 250;
    private const int CollisionParticleMaximumLifetimeMilliseconds = 500;
    private const int LongPressMilliseconds = 260;
    private const int PointerFollowMilliseconds = 90;
    private const int ToolEnterMilliseconds = 160;
    private const int ToolLeaveMilliseconds = 190;
    private const int ToolPressedMilliseconds = 70;
    private const double ChromeMinimumWidth = 420;
    private const double ChromeMinimumHeight = 180;
    private const int DwmWindowCornerPreferenceAttribute = 33;
    private const int DwmWindowCornerPreferenceRound = 2;
    private const string MaximizeGlyphText = "\uE922";
    private const string RestoreGlyphText = "\uE923";
    private const string PointerHighlightPartName = "PointerHighlight";
    private const string ToolPickupScaleTransformPartName = "ToolPickupScaleTransform";
    private const string ToolSoftBodyScaleTransformPartName = "ToolSoftBodyScaleTransform";
    private const string ToolIconScaleTransformPartName = "ToolIconScaleTransform";
    private const string ToolLiftTransformPartName = "ToolLiftTransform";
    private const string ToolWobbleTransformPartName = "ToolWobbleTransform";

    private static readonly Brush[] PickupParticleBrushes =
    [
        CreateFrozenBrush(Color.FromArgb(132, 91, 127, 214)),
        CreateFrozenBrush(Color.FromArgb(150, 255, 255, 255)),
        CreateFrozenBrush(Color.FromArgb(118, 132, 153, 184))
    ];

    private readonly DispatcherTimer _longPressTimer;
    private HashSet<Button> _currentCollisionContacts = new();
    private HashSet<Button> _nextCollisionContacts = new();
    private readonly Dictionary<Button, Vector> _collisionForces = new();
    private readonly Dictionary<Button, Vector> _collisionPressures = new();
    private Button? _pressedTool;
    private Point _pressPointInWorkspace;
    private Point _dragGrabOffset;
    private WorkspaceBoundary _currentBoundaryContacts;
    private Button[] _toolButtons = [];
    private Rect[] _resizeToolBounds = [];
    private WindowBoundsSnapshot _previousWindowBounds;
    private bool _hasWindowBoundsSnapshot;
    private bool _resizeUpdateScheduled;
    private bool _isApplyingAdaptiveResize;
    private int _lastResizeIterations;
    private int _lastResizeIterationLimit;
    private bool _isLongPressPending;
    private bool _isDragging;
    private bool _draggedToolHasSoftPressure;

    private static readonly SineEase PointerFollowEasing = new()
    {
        EasingMode = EasingMode.EaseOut
    };

    private static readonly CubicEase ToolTransitionEasing = new()
    {
        EasingMode = EasingMode.EaseOut
    };

    private static readonly QuadraticEase CollisionImpactEasing = new()
    {
        EasingMode = EasingMode.EaseOut
    };

    private static readonly CubicEase CollisionReturnEasing = new()
    {
        EasingMode = EasingMode.EaseOut
    };

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr windowHandle,
        int attribute,
        ref int attributeValue,
        int attributeSize);

    public MainWindow()
    {
        InitializeComponent();
        _longPressTimer = new DispatcherTimer(DispatcherPriority.Input)
        {
            Interval = TimeSpan.FromMilliseconds(LongPressMilliseconds)
        };
        _longPressTimer.Tick += OnLongPressTimerTick;
        UpdateMaximizeGlyph();
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        var windowHandle = new WindowInteropHelper(this).Handle;
        var cornerPreference = DwmWindowCornerPreferenceRound;
        var result = DwmSetWindowAttribute(
            windowHandle,
            DwmWindowCornerPreferenceAttribute,
            ref cornerPreference,
            Marshal.SizeOf<int>());

        Marshal.ThrowExceptionForHR(result);
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        EnsureToolBuffers();
        _previousWindowBounds = GetCurrentWindowBounds();
        _hasWindowBoundsSnapshot = true;
        UpdateDynamicMinimumSize();
    }

    private void OnWindowLocationChanged(object? sender, EventArgs e)
    {
        ScheduleAdaptiveResize();
    }

    private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ScheduleAdaptiveResize();
    }

    private void ScheduleAdaptiveResize()
    {
        if (!IsLoaded
            || _isApplyingAdaptiveResize
            || _resizeUpdateScheduled
            || WindowState == WindowState.Minimized)
        {
            return;
        }

        _resizeUpdateScheduled = true;
        Dispatcher.BeginInvoke(
            DispatcherPriority.Render,
            new Action(ApplyAdaptiveResize));
    }

    private void ApplyAdaptiveResize()
    {
        _resizeUpdateScheduled = false;
        if (!IsLoaded || WindowState == WindowState.Minimized)
        {
            return;
        }

        var currentWindowBounds = GetCurrentWindowBounds();
        if (!_hasWindowBoundsSnapshot)
        {
            _previousWindowBounds = currentWindowBounds;
            _hasWindowBoundsSnapshot = true;
            UpdateDynamicMinimumSize();
            return;
        }

        var inwardBoundaries = WorkspaceInteraction.GetInwardMovingBoundaries(
            _previousWindowBounds,
            currentWindowBounds);
        if (inwardBoundaries == WorkspaceBoundary.None)
        {
            _previousWindowBounds = currentWindowBounds;
            UpdateDynamicMinimumSize();
            return;
        }

        EnsureToolBuffers();
        if (_toolButtons.Length == 0)
        {
            _previousWindowBounds = currentWindowBounds;
            UpdateDynamicMinimumSize();
            return;
        }

        CaptureToolBounds();
        WorkspaceInteraction.PreserveScreenPositionsForMovingOrigin(
            _resizeToolBounds,
            _previousWindowBounds,
            currentWindowBounds,
            inwardBoundaries);
        var firstTool = _resizeToolBounds[0];
        var bounds = WorkspaceInteraction.GetSoftBounds(
            Workspace.ActualWidth,
            Workspace.ActualHeight,
            firstTool.Width,
            firstTool.Height);
        var result = WorkspaceInteraction.ConstrainForResize(
            _resizeToolBounds,
            bounds,
            inwardBoundaries);
        _lastResizeIterations = result.Iterations;
        _lastResizeIterationLimit = result.IterationLimit;

        if (!result.IsValid)
        {
            PreventInvalidResize(inwardBoundaries);
            return;
        }

        _isApplyingAdaptiveResize = true;
        try
        {
            ApplyToolBounds();
            _previousWindowBounds = currentWindowBounds;
            UpdateDynamicMinimumSize();
        }
        finally
        {
            _isApplyingAdaptiveResize = false;
        }
    }

    private void PreventInvalidResize(WorkspaceBoundary inwardBoundaries)
    {
        _isApplyingAdaptiveResize = true;
        try
        {
            if ((inwardBoundaries & (WorkspaceBoundary.Left | WorkspaceBoundary.Right)) != 0)
            {
                MinWidth = Math.Max(MinWidth, Math.Ceiling(_previousWindowBounds.Width));
                Width = Math.Max(ActualWidth, MinWidth);
            }

            if ((inwardBoundaries & (WorkspaceBoundary.Top | WorkspaceBoundary.Bottom)) != 0)
            {
                MinHeight = Math.Max(MinHeight, Math.Ceiling(_previousWindowBounds.Height));
                Height = Math.Max(ActualHeight, MinHeight);
            }
        }
        finally
        {
            _isApplyingAdaptiveResize = false;
        }
    }

    private void UpdateDynamicMinimumSize()
    {
        if (!IsLoaded || Workspace.ActualWidth <= 0 || Workspace.ActualHeight <= 0)
        {
            return;
        }

        EnsureToolBuffers();
        CaptureToolBounds();
        var requiredWorkspaceSize = WorkspaceInteraction.GetRequiredWorkspaceSize(
            _resizeToolBounds);
        var chromeWidth = Math.Max(0, ActualWidth - Workspace.ActualWidth);
        var chromeHeight = Math.Max(0, ActualHeight - Workspace.ActualHeight);
        var minimumWidth = Math.Ceiling(Math.Max(
            ChromeMinimumWidth,
            requiredWorkspaceSize.Width + chromeWidth));
        var minimumHeight = Math.Ceiling(Math.Max(
            ChromeMinimumHeight,
            requiredWorkspaceSize.Height + chromeHeight));

        if (Math.Abs(MinWidth - minimumWidth) > 0.5)
        {
            MinWidth = minimumWidth;
        }

        if (Math.Abs(MinHeight - minimumHeight) > 0.5)
        {
            MinHeight = minimumHeight;
        }
    }

    private void EnsureToolBuffers()
    {
        var toolCount = 0;
        foreach (UIElement child in Workspace.Children)
        {
            if (child is Button)
            {
                toolCount++;
            }
        }

        if (_toolButtons.Length == toolCount)
        {
            return;
        }

        _toolButtons = new Button[toolCount];
        _resizeToolBounds = new Rect[toolCount];
        var toolIndex = 0;
        foreach (UIElement child in Workspace.Children)
        {
            if (child is Button toolButton)
            {
                _toolButtons[toolIndex++] = toolButton;
            }
        }
    }

    private void CaptureToolBounds()
    {
        for (var index = 0; index < _toolButtons.Length; index++)
        {
            var toolButton = _toolButtons[index];
            _resizeToolBounds[index] = new Rect(
                GetFiniteCanvasCoordinate(Canvas.GetLeft(toolButton)),
                GetFiniteCanvasCoordinate(Canvas.GetTop(toolButton)),
                toolButton.ActualWidth,
                toolButton.ActualHeight);
        }
    }

    private void ApplyToolBounds()
    {
        for (var index = 0; index < _toolButtons.Length; index++)
        {
            Canvas.SetLeft(_toolButtons[index], _resizeToolBounds[index].Left);
            Canvas.SetTop(_toolButtons[index], _resizeToolBounds[index].Top);
        }
    }

    private WindowBoundsSnapshot GetCurrentWindowBounds()
    {
        return new WindowBoundsSnapshot(Left, Top, ActualWidth, ActualHeight);
    }

    private void OnTitleBarMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        if (e.ClickCount == 2)
        {
            ToggleMaximizeRestore();
            return;
        }

        if (WindowState == WindowState.Normal)
        {
            DragMove();
        }
    }

    private void OnToolButtonMouseEnter(object sender, MouseEventArgs e)
    {
        var toolButton = (Button)sender;
        if (_isDragging && ReferenceEquals(_pressedTool, toolButton))
        {
            return;
        }

        AnimatePointerPosition(toolButton, e.GetPosition(toolButton));
        AnimateToolOpacity(toolButton, ToolHoverOpacity, ToolEnterMilliseconds);
        AnimateToolOffset(toolButton, ToolHoverOffset, ToolEnterMilliseconds);
    }

    private void OnToolButtonMouseMove(object sender, MouseEventArgs e)
    {
        var toolButton = (Button)sender;
        if (_isDragging && ReferenceEquals(_pressedTool, toolButton))
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                EndDragging(toolButton);
                return;
            }

            MoveDraggedTool(toolButton, e.GetPosition(Workspace));
            e.Handled = true;
            return;
        }

        AnimatePointerPosition(toolButton, e.GetPosition(toolButton));

        if (_isLongPressPending && ReferenceEquals(_pressedTool, toolButton))
        {
            var currentPoint = e.GetPosition(Workspace);
            var delta = currentPoint - _pressPointInWorkspace;
            if ((delta.X * delta.X) + (delta.Y * delta.Y)
                > LongPressMoveTolerance * LongPressMoveTolerance)
            {
                CancelLongPressCandidate();
            }
        }
    }

    private void OnToolButtonMouseLeave(object sender, MouseEventArgs e)
    {
        var toolButton = (Button)sender;
        if (_isDragging && ReferenceEquals(_pressedTool, toolButton))
        {
            return;
        }

        if (_isLongPressPending && ReferenceEquals(_pressedTool, toolButton))
        {
            CancelLongPressCandidate();
        }

        AnimateToolOpacity(toolButton, 0, ToolLeaveMilliseconds);
        AnimateToolOffset(toolButton, 0, ToolLeaveMilliseconds);
    }

    private void OnToolButtonPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var toolButton = (Button)sender;
        CancelLongPressCandidate();

        _pressedTool = toolButton;
        _pressPointInWorkspace = e.GetPosition(Workspace);
        _isLongPressPending = true;
        _longPressTimer.Start();

        AnimateToolOffset(toolButton, 0, ToolPressedMilliseconds);
        e.Handled = true;
    }

    private void OnToolButtonPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var toolButton = (Button)sender;
        if (!ReferenceEquals(_pressedTool, toolButton))
        {
            return;
        }

        if (_isDragging)
        {
            EndDragging(toolButton);
        }
        else
        {
            CancelLongPressCandidate();
        }

        e.Handled = true;
    }

    private void OnToolButtonLostMouseCapture(object sender, MouseEventArgs e)
    {
        var toolButton = (Button)sender;
        if (!ReferenceEquals(_pressedTool, toolButton))
        {
            return;
        }

        if (_isDragging)
        {
            EndDragging(toolButton);
        }
        else
        {
            CancelLongPressCandidate();
        }
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        if (_pressedTool is not { } toolButton)
        {
            return;
        }

        if (_isDragging)
        {
            EndDragging(toolButton);
        }
        else
        {
            CancelLongPressCandidate();
        }
    }

    private void OnLongPressTimerTick(object? sender, EventArgs e)
    {
        _longPressTimer.Stop();
        if (!_isLongPressPending || _pressedTool is not { } toolButton)
        {
            return;
        }

        if (Mouse.LeftButton != MouseButtonState.Pressed)
        {
            CancelLongPressCandidate();
            return;
        }

        _isLongPressPending = false;
        _dragGrabOffset = Mouse.GetPosition(toolButton);
        if (!toolButton.CaptureMouse())
        {
            _pressedTool = null;
            AnimateToolOffset(
                toolButton,
                toolButton.IsMouseOver ? ToolHoverOffset : 0,
                DragTransitionMilliseconds);
            return;
        }

        _isDragging = true;
        _currentBoundaryContacts = WorkspaceBoundary.None;
        ClearCollisionContacts();
        Panel.SetZIndex(toolButton, 1);
        toolButton.Effect = new DropShadowEffect
        {
            BlurRadius = 21,
            Direction = 270,
            ShadowDepth = 4,
            Color = Color.FromRgb(38, 58, 85),
            Opacity = 0.18
        };
        AnimateToolOpacity(toolButton, DragHighlightOpacity, PickupTransitionMilliseconds);
        AnimateToolOffset(toolButton, PickupLiftOffset, PickupTransitionMilliseconds);
        AnimateToolScale(toolButton, PickupScale, PickupTransitionMilliseconds);
        StartPickupShake(toolButton);
        SpawnPickupParticles(toolButton);
    }

    private void CancelLongPressCandidate()
    {
        if (!_isLongPressPending)
        {
            return;
        }

        _longPressTimer.Stop();
        _isLongPressPending = false;
        var toolButton = _pressedTool;
        _pressedTool = null;

        if (toolButton is not null)
        {
            AnimateToolOffset(
                toolButton,
                toolButton.IsMouseOver ? ToolHoverOffset : 0,
                ToolPressedMilliseconds);
        }
    }

    private void MoveDraggedTool(Button toolButton, Point pointerPosition)
    {
        _nextCollisionContacts.Clear();
        _collisionForces.Clear();
        _collisionPressures.Clear();

        var bounds = WorkspaceInteraction.GetSoftBounds(
            Workspace.ActualWidth,
            Workspace.ActualHeight,
            toolButton.ActualWidth,
            toolButton.ActualHeight);
        var currentX = Math.Clamp(
            GetFiniteCanvasCoordinate(Canvas.GetLeft(toolButton)),
            bounds.MinimumX,
            bounds.MaximumX);
        var currentY = Math.Clamp(
            GetFiniteCanvasCoordinate(Canvas.GetTop(toolButton)),
            bounds.MinimumY,
            bounds.MaximumY);
        var rawDesiredPosition = new Point(
            pointerPosition.X - _dragGrabOffset.X,
            pointerPosition.Y - _dragGrabOffset.Y);
        var desiredPosition = bounds.Clamp(rawDesiredPosition);
        var desiredX = desiredPosition.X;
        var desiredY = desiredPosition.Y;
        var resolvedX = ResolveHorizontalMovement(toolButton, currentX, currentY, desiredX);
        var resolvedY = ResolveVerticalMovement(toolButton, resolvedX, currentY, desiredY);

        Canvas.SetLeft(toolButton, resolvedX);
        Canvas.SetTop(toolButton, resolvedY);
        UpdateBoundaryFeedback(toolButton, rawDesiredPosition, bounds);
        UpdateCollisionFeedback(
            toolButton,
            new Vector(desiredX - resolvedX, desiredY - resolvedY));
    }

    private void EndDragging(Button toolButton)
    {
        if (!_isDragging || !ReferenceEquals(_pressedTool, toolButton))
        {
            return;
        }

        _longPressTimer.Stop();
        _isLongPressPending = false;
        _isDragging = false;
        _pressedTool = null;
        _currentBoundaryContacts = WorkspaceBoundary.None;
        ClearCollisionContacts();

        if (Mouse.Captured == toolButton)
        {
            toolButton.ReleaseMouseCapture();
        }

        Panel.SetZIndex(toolButton, 0);
        toolButton.ClearValue(EffectProperty);
        ClearToolMotionVisual(toolButton);
        ClearSoftBodyScale(toolButton);

        if (toolButton.IsMouseOver)
        {
            AnimatePointerPosition(toolButton, Mouse.GetPosition(toolButton));
            SetToolHighlightOpacity(toolButton, ToolHoverOpacity);
        }
        else
        {
            SetToolHighlightOpacity(toolButton, 0);
        }

        UpdateDynamicMinimumSize();
    }

    private double ResolveHorizontalMovement(
        Button toolButton,
        double currentX,
        double currentY,
        double desiredX)
    {
        var resolvedX = desiredX;
        var toolWidth = toolButton.ActualWidth;
        var toolTop = currentY;
        var toolBottom = currentY + toolButton.ActualHeight;
        Button? collidedTool = null;
        var collisionDirection = 0d;

        foreach (UIElement child in Workspace.Children)
        {
            if (child is not Button otherTool
                || ReferenceEquals(otherTool, toolButton))
            {
                continue;
            }

            var otherLeft = GetFiniteCanvasCoordinate(Canvas.GetLeft(otherTool));
            var otherTop = GetFiniteCanvasCoordinate(Canvas.GetTop(otherTool));
            var otherRight = otherLeft + otherTool.ActualWidth;
            var otherBottom = otherTop + otherTool.ActualHeight;
            if (!IntervalsOverlap(toolTop, toolBottom, otherTop, otherBottom))
            {
                continue;
            }

            if (desiredX > currentX
                && currentX + toolWidth <= otherLeft
                && desiredX + toolWidth > otherLeft)
            {
                var collisionBoundary = otherLeft - toolWidth;
                if (collisionBoundary < resolvedX)
                {
                    resolvedX = collisionBoundary;
                    collidedTool = otherTool;
                    collisionDirection = 1;
                }
            }
            else if (desiredX < currentX
                && currentX >= otherRight
                && desiredX < otherRight)
            {
                if (otherRight > resolvedX)
                {
                    resolvedX = otherRight;
                    collidedTool = otherTool;
                    collisionDirection = -1;
                }
            }
        }

        if (collidedTool is not null)
        {
            RegisterCollisionContact(
                collidedTool,
                new Vector(collisionDirection, 0),
                new Vector(desiredX - resolvedX, 0));
        }

        return resolvedX;
    }

    private double ResolveVerticalMovement(
        Button toolButton,
        double resolvedX,
        double currentY,
        double desiredY)
    {
        var resolvedY = desiredY;
        var toolHeight = toolButton.ActualHeight;
        var toolLeft = resolvedX;
        var toolRight = resolvedX + toolButton.ActualWidth;
        Button? collidedTool = null;
        var collisionDirection = 0d;

        foreach (UIElement child in Workspace.Children)
        {
            if (child is not Button otherTool
                || ReferenceEquals(otherTool, toolButton))
            {
                continue;
            }

            var otherLeft = GetFiniteCanvasCoordinate(Canvas.GetLeft(otherTool));
            var otherTop = GetFiniteCanvasCoordinate(Canvas.GetTop(otherTool));
            var otherRight = otherLeft + otherTool.ActualWidth;
            var otherBottom = otherTop + otherTool.ActualHeight;
            if (!IntervalsOverlap(toolLeft, toolRight, otherLeft, otherRight))
            {
                continue;
            }

            if (desiredY > currentY
                && currentY + toolHeight <= otherTop
                && desiredY + toolHeight > otherTop)
            {
                var collisionBoundary = otherTop - toolHeight;
                if (collisionBoundary < resolvedY)
                {
                    resolvedY = collisionBoundary;
                    collidedTool = otherTool;
                    collisionDirection = 1;
                }
            }
            else if (desiredY < currentY
                && currentY >= otherBottom
                && desiredY < otherBottom)
            {
                if (otherBottom > resolvedY)
                {
                    resolvedY = otherBottom;
                    collidedTool = otherTool;
                    collisionDirection = -1;
                }
            }
        }

        if (collidedTool is not null)
        {
            RegisterCollisionContact(
                collidedTool,
                new Vector(0, collisionDirection),
                new Vector(0, desiredY - resolvedY));
        }

        return resolvedY;
    }

    private void RegisterCollisionContact(
        Button collidedTool,
        Vector forceDirection,
        Vector pressure)
    {
        _nextCollisionContacts.Add(collidedTool);
        if (_collisionForces.TryGetValue(collidedTool, out var existingDirection))
        {
            forceDirection += existingDirection;
            forceDirection.Normalize();
        }

        _collisionForces[collidedTool] = forceDirection;

        if (_collisionPressures.TryGetValue(collidedTool, out var existingPressure))
        {
            pressure += existingPressure;
        }

        _collisionPressures[collidedTool] = pressure;
    }

    private void UpdateCollisionFeedback(Button draggedTool, Vector totalPressure)
    {
        foreach (var previousTool in _currentCollisionContacts)
        {
            if (!_nextCollisionContacts.Contains(previousTool))
            {
                ResetSoftBodyScale(previousTool);
            }
        }

        foreach (var collidedTool in _nextCollisionContacts)
        {
            ApplySoftCollisionVisual(
                collidedTool,
                _collisionPressures[collidedTool],
                isDraggedTool: false);

            if (!_currentCollisionContacts.Contains(collidedTool))
            {
                var forceDirection = _collisionForces[collidedTool];
                AnimateCollisionFeedback(collidedTool, forceDirection);
                SpawnContactParticles(
                    draggedTool,
                    collidedTool,
                    forceDirection,
                    CollisionParticleMinimumCount,
                    CollisionParticleMaximumCount,
                    CollisionParticleMinimumLifetimeMilliseconds,
                    CollisionParticleMaximumLifetimeMilliseconds,
                    travelScale: 1);
            }
        }

        if (_nextCollisionContacts.Count > 0 && totalPressure.Length > 0.01)
        {
            ApplySoftCollisionVisual(draggedTool, totalPressure, isDraggedTool: true);
            _draggedToolHasSoftPressure = true;
        }
        else if (_draggedToolHasSoftPressure)
        {
            ResetSoftBodyScale(draggedTool);
            _draggedToolHasSoftPressure = false;
        }

        var previousContacts = _currentCollisionContacts;
        _currentCollisionContacts = _nextCollisionContacts;
        _nextCollisionContacts = previousContacts;
    }

    private void ClearCollisionContacts()
    {
        foreach (var collidedTool in _currentCollisionContacts)
        {
            ClearSoftBodyScale(collidedTool);
        }

        foreach (var collidedTool in _nextCollisionContacts)
        {
            if (!_currentCollisionContacts.Contains(collidedTool))
            {
                ClearSoftBodyScale(collidedTool);
            }
        }

        _currentCollisionContacts.Clear();
        _nextCollisionContacts.Clear();
        _collisionForces.Clear();
        _collisionPressures.Clear();
        _draggedToolHasSoftPressure = false;
    }

    private void UpdateBoundaryFeedback(
        Button toolButton,
        Point rawDesiredPosition,
        SoftWorkspaceBounds bounds)
    {
        var contacts = WorkspaceInteraction.GetBoundaryContacts(rawDesiredPosition, bounds);
        var enteredContacts = contacts & ~_currentBoundaryContacts;

        TriggerBoundaryFeedbackIfEntered(toolButton, enteredContacts, WorkspaceBoundary.Left);
        TriggerBoundaryFeedbackIfEntered(toolButton, enteredContacts, WorkspaceBoundary.Top);
        TriggerBoundaryFeedbackIfEntered(toolButton, enteredContacts, WorkspaceBoundary.Right);
        TriggerBoundaryFeedbackIfEntered(toolButton, enteredContacts, WorkspaceBoundary.Bottom);

        _currentBoundaryContacts = contacts;
    }

    private void TriggerBoundaryFeedbackIfEntered(
        Button toolButton,
        WorkspaceBoundary enteredContacts,
        WorkspaceBoundary boundary)
    {
        if ((enteredContacts & boundary) == 0)
        {
            return;
        }

        AnimateBoundaryCollision(toolButton, boundary);
        AnimateBoundaryHighlight(boundary);
        SpawnBoundaryParticles(toolButton, boundary);
    }

    private static void AnimateBoundaryCollision(Button toolButton, WorkspaceBoundary boundary)
    {
        var scale = GetTemplatePart<ScaleTransform>(
            toolButton,
            ToolSoftBodyScaleTransformPartName);
        var isHorizontalCollision = boundary is WorkspaceBoundary.Left or WorkspaceBoundary.Right;
        var impactScaleX = isHorizontalCollision ? 0.94 : 1.035;
        var impactScaleY = isHorizontalCollision ? 1.035 : 0.94;

        scale.BeginAnimation(
            ScaleTransform.ScaleXProperty,
            CreateBoundaryPulseAnimation(scale.ScaleX, impactScaleX),
            HandoffBehavior.SnapshotAndReplace);
        scale.BeginAnimation(
            ScaleTransform.ScaleYProperty,
            CreateBoundaryPulseAnimation(scale.ScaleY, impactScaleY),
            HandoffBehavior.SnapshotAndReplace);

        if (toolButton.Effect is DropShadowEffect shadow)
        {
            shadow.BeginAnimation(
                DropShadowEffect.OpacityProperty,
                CreateBoundaryPulseAnimation(shadow.Opacity, 0.23, 0.18),
                HandoffBehavior.SnapshotAndReplace);
        }
    }

    private static DoubleAnimationUsingKeyFrames CreateBoundaryPulseAnimation(
        double currentValue,
        double impactValue,
        double returnValue = 1)
    {
        var animation = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromMilliseconds(210),
            FillBehavior = FillBehavior.Stop
        };
        animation.KeyFrames.Add(new LinearDoubleKeyFrame(
            currentValue,
            KeyTime.FromTimeSpan(TimeSpan.Zero)));
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(
            impactValue,
            KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(55)))
        {
            EasingFunction = CollisionImpactEasing
        });
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(
            returnValue,
            KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(210)))
        {
            EasingFunction = CollisionReturnEasing
        });
        return animation;
    }

    private void AnimateBoundaryHighlight(WorkspaceBoundary boundary)
    {
        var highlight = boundary switch
        {
            WorkspaceBoundary.Left => LeftBoundaryHighlight,
            WorkspaceBoundary.Top => TopBoundaryHighlight,
            WorkspaceBoundary.Right => RightBoundaryHighlight,
            WorkspaceBoundary.Bottom => BottomBoundaryHighlight,
            _ => throw new ArgumentOutOfRangeException(nameof(boundary))
        };
        var animation = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromMilliseconds(230),
            FillBehavior = FillBehavior.Stop
        };
        animation.KeyFrames.Add(new LinearDoubleKeyFrame(
            highlight.Opacity,
            KeyTime.FromTimeSpan(TimeSpan.Zero)));
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(
            0.3,
            KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(55)))
        {
            EasingFunction = CollisionImpactEasing
        });
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(
            0,
            KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(230)))
        {
            EasingFunction = CollisionReturnEasing
        });
        highlight.BeginAnimation(
            OpacityProperty,
            animation,
            HandoffBehavior.SnapshotAndReplace);
    }

    private void SpawnBoundaryParticles(Button toolButton, WorkspaceBoundary boundary)
    {
        var random = Random.Shared;
        var particleCount = random.Next(6, 13);
        var toolLeft = GetFiniteCanvasCoordinate(Canvas.GetLeft(toolButton));
        var toolTop = GetFiniteCanvasCoordinate(Canvas.GetTop(toolButton));

        for (var index = 0; index < particleCount; index++)
        {
            var size = 2 + (random.NextDouble() * 2.5);
            var tangentOffset = random.NextDouble() * 48 + 8;
            var tangentDrift = (random.NextDouble() - 0.5) * 18;
            var normalDrift = 8 + (random.NextDouble() * 16);
            var startLeft = toolLeft;
            var startTop = toolTop;
            var driftX = 0d;
            var driftY = 0d;

            switch (boundary)
            {
                case WorkspaceBoundary.Left:
                    startLeft = WorkspaceInteraction.SoftBoundaryPadding - (size / 2);
                    startTop += tangentOffset - (size / 2);
                    driftX = normalDrift;
                    driftY = tangentDrift;
                    break;
                case WorkspaceBoundary.Top:
                    startLeft += tangentOffset - (size / 2);
                    startTop = WorkspaceInteraction.SoftBoundaryPadding - (size / 2);
                    driftX = tangentDrift;
                    driftY = normalDrift;
                    break;
                case WorkspaceBoundary.Right:
                    startLeft = Workspace.ActualWidth
                        - WorkspaceInteraction.SoftBoundaryPadding
                        - (size / 2);
                    startTop += tangentOffset - (size / 2);
                    driftX = -normalDrift;
                    driftY = tangentDrift;
                    break;
                case WorkspaceBoundary.Bottom:
                    startLeft += tangentOffset - (size / 2);
                    startTop = Workspace.ActualHeight
                        - WorkspaceInteraction.SoftBoundaryPadding
                        - (size / 2);
                    driftX = tangentDrift;
                    driftY = -normalDrift;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(boundary));
            }

            var lifetime = random.Next(260, 441);
            var particle = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = PickupParticleBrushes[random.Next(PickupParticleBrushes.Length)],
                IsHitTestVisible = false,
                Opacity = 0.34 + (random.NextDouble() * 0.24)
            };
            Canvas.SetLeft(particle, startLeft);
            Canvas.SetTop(particle, startTop);
            Panel.SetZIndex(particle, 3);
            Workspace.Children.Add(particle);

            particle.BeginAnimation(
                Canvas.LeftProperty,
                new DoubleAnimation
                {
                    To = startLeft + driftX,
                    Duration = TimeSpan.FromMilliseconds(lifetime),
                    EasingFunction = ToolTransitionEasing
                },
                HandoffBehavior.SnapshotAndReplace);
            particle.BeginAnimation(
                Canvas.TopProperty,
                new DoubleAnimation
                {
                    To = startTop + driftY,
                    Duration = TimeSpan.FromMilliseconds(lifetime),
                    EasingFunction = ToolTransitionEasing
                },
                HandoffBehavior.SnapshotAndReplace);

            var fade = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(lifetime),
                EasingFunction = PointerFollowEasing
            };
            fade.Completed += (_, _) => Workspace.Children.Remove(particle);
            particle.BeginAnimation(
                OpacityProperty,
                fade,
                HandoffBehavior.SnapshotAndReplace);
        }
    }

    private static void ApplySoftCollisionVisual(
        Button toolButton,
        Vector pressure,
        bool isDraggedTool)
    {
        var horizontalPressure = Math.Abs(pressure.X);
        var verticalPressure = Math.Abs(pressure.Y);
        var isHorizontal = horizontalPressure >= verticalPressure;
        var dominantPressure = isHorizontal ? horizontalPressure : verticalPressure;
        var intensity = Math.Clamp(dominantPressure / SoftCollisionFullPressure, 0, 1);
        var minimumScale = isDraggedTool
            ? DraggedToolMinimumCollisionScale
            : BlockedToolMinimumCollisionScale;
        var maximumBulge = isDraggedTool
            ? DraggedToolMaximumCollisionBulge
            : BlockedToolMaximumCollisionBulge;
        var direction = isHorizontal ? new Vector(1, 0) : new Vector(0, 1);
        ApplySoftCollisionVisualForAxis(
            toolButton,
            direction,
            intensity,
            minimumScale,
            maximumBulge);
    }

    private static void ApplySoftCollisionVisualForAxis(
        Button toolButton,
        Vector direction,
        double intensity,
        double minimumScale,
        double maximumBulge)
    {
        intensity = Math.Clamp(intensity, 0, 1);
        var isHorizontal = Math.Abs(direction.X) >= Math.Abs(direction.Y);
        var compressedScale = 1 - ((1 - minimumScale) * intensity);
        var bulgedScale = 1 + ((maximumBulge - 1) * intensity);
        var shellScaleX = isHorizontal ? compressedScale : bulgedScale;
        var shellScaleY = isHorizontal ? bulgedScale : compressedScale;

        var shellScale = GetTemplatePart<ScaleTransform>(
            toolButton,
            ToolSoftBodyScaleTransformPartName);
        var iconScale = GetTemplatePart<ScaleTransform>(
            toolButton,
            ToolIconScaleTransformPartName);
        SetAnimatedScaleBase(shellScale, shellScaleX, shellScaleY);

        var iconTotalScaleX = 1 + ((shellScaleX - 1) * IconCollisionShapeInheritance);
        var iconTotalScaleY = 1 + ((shellScaleY - 1) * IconCollisionShapeInheritance);
        SetAnimatedScaleBase(iconScale, iconTotalScaleX, iconTotalScaleY);
    }

    private static void SetAnimatedScaleBase(
        ScaleTransform transform,
        double scaleX,
        double scaleY)
    {
        transform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        transform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        transform.ScaleX = scaleX;
        transform.ScaleY = scaleY;
    }

    private static void SetToolHighlightOpacity(Button toolButton, double opacity)
    {
        var highlight = GetTemplatePart<Border>(toolButton, PointerHighlightPartName);
        highlight.BeginAnimation(UIElement.OpacityProperty, null);
        highlight.Opacity = Math.Clamp(opacity, 0, 1);
    }

    private void SpawnContactParticles(
        Button draggedTool,
        Button collidedTool,
        Vector forceDirection,
        int minimumCount,
        int maximumCount,
        int minimumLifetimeMilliseconds,
        int maximumLifetimeMilliseconds,
        double travelScale)
    {
        var random = Random.Shared;
        var particleCount = random.Next(
            minimumCount,
            maximumCount + 1);
        var draggedBounds = new Rect(
            GetFiniteCanvasCoordinate(Canvas.GetLeft(draggedTool)),
            GetFiniteCanvasCoordinate(Canvas.GetTop(draggedTool)),
            draggedTool.ActualWidth,
            draggedTool.ActualHeight);
        var collidedBounds = new Rect(
            GetFiniteCanvasCoordinate(Canvas.GetLeft(collidedTool)),
            GetFiniteCanvasCoordinate(Canvas.GetTop(collidedTool)),
            collidedTool.ActualWidth,
            collidedTool.ActualHeight);
        Point contactPoint;
        Vector contactNormal;

        if (Math.Abs(forceDirection.X) >= Math.Abs(forceDirection.Y))
        {
            var overlapTop = Math.Max(draggedBounds.Top, collidedBounds.Top);
            var overlapBottom = Math.Min(draggedBounds.Bottom, collidedBounds.Bottom);
            contactPoint = new Point(
                forceDirection.X > 0 ? collidedBounds.Left : collidedBounds.Right,
                (overlapTop + overlapBottom) / 2);
            contactNormal = new Vector(Math.Sign(forceDirection.X), 0);
        }
        else
        {
            var overlapLeft = Math.Max(draggedBounds.Left, collidedBounds.Left);
            var overlapRight = Math.Min(draggedBounds.Right, collidedBounds.Right);
            contactPoint = new Point(
                (overlapLeft + overlapRight) / 2,
                forceDirection.Y > 0 ? collidedBounds.Top : collidedBounds.Bottom);
            contactNormal = new Vector(0, Math.Sign(forceDirection.Y));
        }

        var tangent = new Vector(-contactNormal.Y, contactNormal.X);
        for (var index = 0; index < particleCount; index++)
        {
            var size = random.NextDouble() < 0.08
                ? 4 + random.NextDouble()
                : 2 + (random.NextDouble() * 2);
            var lifetime = random.Next(
                minimumLifetimeMilliseconds,
                maximumLifetimeMilliseconds + 1);
            var tangentStart = (random.NextDouble() - 0.5) * 18;
            var startPoint = contactPoint + (tangent * tangentStart);
            var side = random.NextDouble() < 0.5 ? -1 : 1;
            var normalTravel = side * (8 + (random.NextDouble() * 16)) * travelScale;
            var tangentTravel = (random.NextDouble() - 0.5) * 15 * travelScale;
            var destination = startPoint
                + (contactNormal * normalTravel)
                + (tangent * tangentTravel);
            var particle = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = PickupParticleBrushes[random.Next(PickupParticleBrushes.Length)],
                IsHitTestVisible = false,
                Opacity = 0.36 + (random.NextDouble() * 0.26)
            };
            var startLeft = startPoint.X - (size / 2);
            var startTop = startPoint.Y - (size / 2);

            Canvas.SetLeft(particle, startLeft);
            Canvas.SetTop(particle, startTop);
            Panel.SetZIndex(particle, 2);
            Workspace.Children.Add(particle);

            particle.BeginAnimation(
                Canvas.LeftProperty,
                new DoubleAnimation
                {
                    To = destination.X - (size / 2),
                    Duration = TimeSpan.FromMilliseconds(lifetime),
                    EasingFunction = ToolTransitionEasing
                },
                HandoffBehavior.SnapshotAndReplace);
            particle.BeginAnimation(
                Canvas.TopProperty,
                new DoubleAnimation
                {
                    To = destination.Y - (size / 2),
                    Duration = TimeSpan.FromMilliseconds(lifetime),
                    EasingFunction = ToolTransitionEasing
                },
                HandoffBehavior.SnapshotAndReplace);

            var fade = new DoubleAnimation
            {
                To = 0,
                BeginTime = TimeSpan.FromMilliseconds(lifetime * 0.12),
                Duration = TimeSpan.FromMilliseconds(lifetime * 0.88),
                EasingFunction = PointerFollowEasing
            };
            fade.Completed += (_, _) => Workspace.Children.Remove(particle);
            particle.BeginAnimation(
                OpacityProperty,
                fade,
                HandoffBehavior.SnapshotAndReplace);
        }
    }

    private static void AnimateCollisionFeedback(Button collidedTool, Vector forceDirection)
    {
        var translation = GetTemplatePart<TranslateTransform>(
            collidedTool,
            ToolLiftTransformPartName);
        var rotation = GetTemplatePart<RotateTransform>(
            collidedTool,
            ToolWobbleTransformPartName);
        var rotationDirection = forceDirection.X != 0
            ? forceDirection.X
            : -forceDirection.Y;

        translation.BeginAnimation(
            TranslateTransform.XProperty,
            CreateCollisionFeedbackAnimation(
                translation.X,
                forceDirection.X * CollisionDisplacement),
            HandoffBehavior.SnapshotAndReplace);
        translation.BeginAnimation(
            TranslateTransform.YProperty,
            CreateCollisionFeedbackAnimation(
                translation.Y,
                forceDirection.Y * CollisionDisplacement),
            HandoffBehavior.SnapshotAndReplace);
        rotation.BeginAnimation(
            RotateTransform.AngleProperty,
            CreateCollisionFeedbackAnimation(
                rotation.Angle,
                rotationDirection * CollisionRotationDegrees),
            HandoffBehavior.SnapshotAndReplace);
    }

    private static DoubleAnimationUsingKeyFrames CreateCollisionFeedbackAnimation(
        double currentValue,
        double impactValue)
    {
        var animation = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromMilliseconds(CollisionFeedbackMilliseconds),
            FillBehavior = FillBehavior.Stop
        };
        animation.KeyFrames.Add(new LinearDoubleKeyFrame(currentValue, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(
            impactValue,
            KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(CollisionImpactMilliseconds)))
        {
            EasingFunction = CollisionImpactEasing
        });
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(
            0,
            KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(CollisionFeedbackMilliseconds)))
        {
            EasingFunction = CollisionReturnEasing
        });

        return animation;
    }

    private static bool IntervalsOverlap(
        double firstStart,
        double firstEnd,
        double secondStart,
        double secondEnd)
    {
        return firstStart < secondEnd && firstEnd > secondStart;
    }

    private static double GetFiniteCanvasCoordinate(double coordinate)
    {
        return double.IsFinite(coordinate) ? coordinate : 0;
    }

    private static void StartPickupShake(Button toolButton)
    {
        var rotation = GetTemplatePart<RotateTransform>(toolButton, ToolWobbleTransformPartName);
        var animation = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromMilliseconds(PickupShakeMilliseconds),
            FillBehavior = FillBehavior.Stop
        };
        animation.KeyFrames.Add(new LinearDoubleKeyFrame(
            rotation.Angle,
            KeyTime.FromTimeSpan(TimeSpan.Zero)));
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(
            -1.2,
            KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(45)))
        {
            EasingFunction = PointerFollowEasing
        });
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(
            1.2,
            KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(95)))
        {
            EasingFunction = PointerFollowEasing
        });
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(
            -0.6,
            KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(145)))
        {
            EasingFunction = PointerFollowEasing
        });
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(
            0,
            KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(PickupShakeMilliseconds)))
        {
            EasingFunction = ToolTransitionEasing
        });

        rotation.BeginAnimation(
            RotateTransform.AngleProperty,
            animation,
            HandoffBehavior.SnapshotAndReplace);
    }

    private static void ClearToolMotionVisual(Button toolButton)
    {
        var pickupScale = GetTemplatePart<ScaleTransform>(
            toolButton,
            ToolPickupScaleTransformPartName);
        var rotation = GetTemplatePart<RotateTransform>(toolButton, ToolWobbleTransformPartName);
        var translation = GetTemplatePart<TranslateTransform>(
            toolButton,
            ToolLiftTransformPartName);

        SetAnimatedScaleBase(pickupScale, 1, 1);
        rotation.BeginAnimation(RotateTransform.AngleProperty, null);
        rotation.Angle = 0;
        translation.BeginAnimation(TranslateTransform.XProperty, null);
        translation.BeginAnimation(TranslateTransform.YProperty, null);
        translation.X = 0;
        translation.Y = 0;
    }

    private static void ResetSoftBodyScale(Button toolButton)
    {
        var shellScale = GetTemplatePart<ScaleTransform>(
            toolButton,
            ToolSoftBodyScaleTransformPartName);
        var iconScale = GetTemplatePart<ScaleTransform>(
            toolButton,
            ToolIconScaleTransformPartName);
        AnimateScaleTransformToIdentity(shellScale);
        AnimateScaleTransformToIdentity(iconScale);
    }

    private static void ClearSoftBodyScale(Button toolButton)
    {
        var shellScale = GetTemplatePart<ScaleTransform>(
            toolButton,
            ToolSoftBodyScaleTransformPartName);
        var iconScale = GetTemplatePart<ScaleTransform>(
            toolButton,
            ToolIconScaleTransformPartName);
        SetAnimatedScaleBase(shellScale, 1, 1);
        SetAnimatedScaleBase(iconScale, 1, 1);
    }

    private static void AnimateScaleTransformToIdentity(ScaleTransform transform)
    {
        var currentScaleX = transform.ScaleX;
        var currentScaleY = transform.ScaleY;
        transform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        transform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        transform.ScaleX = 1;
        transform.ScaleY = 1;
        transform.BeginAnimation(
            ScaleTransform.ScaleXProperty,
            CreateSoftBodyResetAnimation(currentScaleX),
            HandoffBehavior.SnapshotAndReplace);
        transform.BeginAnimation(
            ScaleTransform.ScaleYProperty,
            CreateSoftBodyResetAnimation(currentScaleY),
            HandoffBehavior.SnapshotAndReplace);
    }

    private static DoubleAnimation CreateSoftBodyResetAnimation(double from)
    {
        return new DoubleAnimation
        {
            From = from,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(DragTransitionMilliseconds),
            EasingFunction = ToolTransitionEasing,
            FillBehavior = FillBehavior.Stop
        };
    }

    private void SpawnPickupParticles(Button toolButton)
    {
        var random = Random.Shared;
        var particleCount = random.Next(
            PickupParticleMinimumCount,
            PickupParticleMaximumCount + 1);
        var toolLeft = GetFiniteCanvasCoordinate(Canvas.GetLeft(toolButton));
        var toolTop = GetFiniteCanvasCoordinate(Canvas.GetTop(toolButton));
        var toolWidth = toolButton.ActualWidth;
        var toolHeight = toolButton.ActualHeight;

        for (var index = 0; index < particleCount; index++)
        {
            var size = random.NextDouble() < 0.1
                ? 5 + random.NextDouble()
                : 2 + (random.NextDouble() * 3);
            var spawnAlongBottom = random.NextDouble() < 0.65;
            double localX;
            double localY;
            if (spawnAlongBottom)
            {
                localX = toolWidth * (0.08 + (random.NextDouble() * 0.84));
                localY = toolHeight * (0.68 + (random.NextDouble() * 0.38));
            }
            else
            {
                var angle = random.NextDouble() * Math.PI * 2;
                var radius = toolWidth * (0.36 + (random.NextDouble() * 0.2));
                localX = (toolWidth / 2) + (Math.Cos(angle) * radius);
                localY = (toolHeight / 2) + (Math.Sin(angle) * radius);
            }

            var startLeft = toolLeft + localX - (size / 2);
            var startTop = toolTop + localY - (size / 2);
            var lifetime = random.Next(
                PickupParticleMinimumLifetimeMilliseconds,
                PickupParticleMaximumLifetimeMilliseconds + 1);
            var horizontalDrift = (random.NextDouble() - 0.5) * 44;
            var verticalFall = 18 + (random.NextDouble() * 34);
            var particle = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = PickupParticleBrushes[random.Next(PickupParticleBrushes.Length)],
                IsHitTestVisible = false,
                Opacity = 0.42 + (random.NextDouble() * 0.28)
            };

            Canvas.SetLeft(particle, startLeft);
            Canvas.SetTop(particle, startTop);
            Panel.SetZIndex(particle, 2);
            Workspace.Children.Add(particle);

            particle.BeginAnimation(
                Canvas.LeftProperty,
                new DoubleAnimation
                {
                    To = startLeft + horizontalDrift,
                    Duration = TimeSpan.FromMilliseconds(lifetime),
                    EasingFunction = ToolTransitionEasing
                },
                HandoffBehavior.SnapshotAndReplace);
            particle.BeginAnimation(
                Canvas.TopProperty,
                new DoubleAnimation
                {
                    To = startTop + verticalFall,
                    Duration = TimeSpan.FromMilliseconds(lifetime),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
                },
                HandoffBehavior.SnapshotAndReplace);

            var fade = new DoubleAnimation
            {
                To = 0,
                BeginTime = TimeSpan.FromMilliseconds(lifetime * 0.16),
                Duration = TimeSpan.FromMilliseconds(lifetime * 0.84),
                EasingFunction = PointerFollowEasing
            };
            fade.Completed += (_, _) => Workspace.Children.Remove(particle);
            particle.BeginAnimation(
                OpacityProperty,
                fade,
                HandoffBehavior.SnapshotAndReplace);
        }
    }

    private static void AnimateToolScale(Button toolButton, double scale, int durationMilliseconds)
    {
        var transform = GetTemplatePart<ScaleTransform>(
            toolButton,
            ToolPickupScaleTransformPartName);
        var animation = new DoubleAnimation
        {
            To = scale,
            Duration = TimeSpan.FromMilliseconds(durationMilliseconds),
            EasingFunction = ToolTransitionEasing
        };

        transform.BeginAnimation(
            ScaleTransform.ScaleXProperty,
            animation,
            HandoffBehavior.SnapshotAndReplace);
        transform.BeginAnimation(
            ScaleTransform.ScaleYProperty,
            animation,
            HandoffBehavior.SnapshotAndReplace);
    }

    private static SolidColorBrush CreateFrozenBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static void AnimatePointerPosition(Button toolButton, Point pointerPosition)
    {
        var highlight = GetTemplatePart<Border>(toolButton, PointerHighlightPartName);
        var highlightBrush = highlight.Background as RadialGradientBrush
            ?? throw new InvalidOperationException("Pointer highlight must use a RadialGradientBrush.");
        if (highlightBrush.IsFrozen)
        {
            highlightBrush = highlightBrush.Clone();
            highlight.Background = highlightBrush;
        }

        var target = new Point(
            Math.Clamp(pointerPosition.X / toolButton.ActualWidth, 0.12, 0.88),
            Math.Clamp(pointerPosition.Y / toolButton.ActualHeight, 0.12, 0.88));
        var animation = new PointAnimation
        {
            To = target,
            Duration = TimeSpan.FromMilliseconds(PointerFollowMilliseconds),
            EasingFunction = PointerFollowEasing
        };

        highlightBrush.BeginAnimation(
            RadialGradientBrush.CenterProperty,
            animation,
            HandoffBehavior.SnapshotAndReplace);
        highlightBrush.BeginAnimation(
            RadialGradientBrush.GradientOriginProperty,
            animation,
            HandoffBehavior.SnapshotAndReplace);
    }

    private static void AnimateToolOpacity(Button toolButton, double opacity, int durationMilliseconds)
    {
        var highlight = GetTemplatePart<Border>(toolButton, PointerHighlightPartName);
        highlight.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation
            {
                To = opacity,
                Duration = TimeSpan.FromMilliseconds(durationMilliseconds),
                EasingFunction = ToolTransitionEasing
            },
            HandoffBehavior.SnapshotAndReplace);
    }

    private static void AnimateToolOffset(Button toolButton, double offset, int durationMilliseconds)
    {
        var transform = GetTemplatePart<TranslateTransform>(toolButton, ToolLiftTransformPartName);
        transform.BeginAnimation(
            TranslateTransform.YProperty,
            new DoubleAnimation
            {
                To = offset,
                Duration = TimeSpan.FromMilliseconds(durationMilliseconds),
                EasingFunction = ToolTransitionEasing
            },
            HandoffBehavior.SnapshotAndReplace);
    }

    private static T GetTemplatePart<T>(Button toolButton, string partName)
        where T : DependencyObject
    {
        toolButton.ApplyTemplate();
        return toolButton.Template.FindName(partName, toolButton) as T
            ?? throw new InvalidOperationException($"Tool template part '{partName}' is missing.");
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnMaximizeRestoreClick(object sender, RoutedEventArgs e)
    {
        ToggleMaximizeRestore();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnWindowStateChanged(object? sender, EventArgs e)
    {
        UpdateMaximizeGlyph();
        ScheduleAdaptiveResize();
    }

    private void ToggleMaximizeRestore()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void UpdateMaximizeGlyph()
    {
        var isMaximized = WindowState == WindowState.Maximized;
        MaximizeGlyph.Text = isMaximized
            ? RestoreGlyphText
            : MaximizeGlyphText;
        var outerRadius = new CornerRadius(isMaximized ? 0 : 9);
        var innerRadius = new CornerRadius(isMaximized ? 0 : 8);
        WindowSurface.CornerRadius = outerRadius;
        WindowTopLeftEdge.CornerRadius = innerRadius;
        WindowBottomRightEdge.CornerRadius = innerRadius;
    }
}
