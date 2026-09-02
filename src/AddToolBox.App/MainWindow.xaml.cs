using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using AddToolBox.Core;
using System.Windows;
using System.Windows.Automation;
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
    private const int BoundaryImpactMilliseconds = 70;
    private const int BoundaryFeedbackMilliseconds = 220;
    private const int BoundaryReleaseMilliseconds = 120;
    private const double BoundaryPeakOpacity = 0.52;
    private const double BoundarySustainedOpacity = 0.12;
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

    private static readonly Brush ActiveLayoutControlBackground =
        CreateFrozenBrush(Color.FromRgb(221, 232, 250));
    private static readonly Brush ActiveLayoutControlForeground =
        CreateFrozenBrush(Color.FromRgb(49, 95, 179));
    private static readonly Brush InactiveLayoutControlForeground =
        CreateFrozenBrush(Color.FromRgb(80, 84, 91));

    private readonly DispatcherTimer _longPressTimer;
    private readonly IReadOnlyDictionary<FrameworkElement, ToolDefinition> _toolDefinitionsByVisual;
    private readonly WorldCanvasState _worldCanvas = new();
    private ToolDefinition? _activeTool;
    private HashSet<Button> _currentCollisionContacts = new();
    private HashSet<Button> _nextCollisionContacts = new();
    private readonly Dictionary<Button, Vector> _collisionForces = new();
    private readonly Dictionary<Button, Vector> _collisionPressures = new();
    private Button? _pressedTool;
    private Point _pressPointInWorkspace;
    private Vector _worldGrabOffset;
    private WorkspaceBoundary _currentBoundaryContacts;
    private Button[] _toolButtons = [];
    private readonly Dictionary<string, Point> _defaultToolWorldPositions =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, Point> _preferredToolWorldPositions =
        new(StringComparer.Ordinal);
    private bool _hasInitialToolPositions;
    private bool _cameraUpdateScheduled;
    private bool _isLongPressPending;
    private bool _isToolClickCandidate;
    private bool _isDragging;
    private bool _draggedToolHasSoftPressure;
    private bool _isPanningWorkspace;
    private Point _workspacePanLastPoint;

    private bool IsLayoutLocked { get; set; }

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
        _toolDefinitionsByVisual = new Dictionary<FrameworkElement, ToolDefinition>();
        ValidateToolIdentityMapping();
        _longPressTimer = new DispatcherTimer(DispatcherPriority.Input)
        {
            Interval = TimeSpan.FromMilliseconds(LongPressMilliseconds)
        };
        _longPressTimer.Tick += OnLongPressTimerTick;
        Workspace.PreviewMouseDown += OnWorkspacePreviewMouseDown;
        Workspace.PreviewMouseMove += OnWorkspacePreviewMouseMove;
        Workspace.PreviewMouseUp += OnWorkspacePreviewMouseUp;
        Workspace.PreviewMouseWheel += OnWorkspacePreviewMouseWheel;
        Workspace.LostMouseCapture += OnWorkspaceLostMouseCapture;
        UpdateMaximizeGlyph();
        UpdateLayoutControlVisuals();
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
        CaptureInitialToolPositions();
        EnsureWorldCanvasInitialized();
        ApplyCameraProjection();
    }

    private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
    {
        ScheduleCameraProjection();
    }

    private void ScheduleCameraProjection()
    {
        if (!IsLoaded
            || _cameraUpdateScheduled
            || WindowState == WindowState.Minimized)
        {
            return;
        }

        _cameraUpdateScheduled = true;
        Dispatcher.BeginInvoke(
            DispatcherPriority.Render,
            new Action(ApplyCameraProjection));
    }

    private void ApplyCameraProjection()
    {
        _cameraUpdateScheduled = false;
        if (!IsLoaded
            || WindowState == WindowState.Minimized
            || Workspace.ActualWidth <= 0
            || Workspace.ActualHeight <= 0)
        {
            return;
        }

        EnsureToolBuffers();
        EnsureWorldCanvasInitialized();
        var viewportSize = GetWorkspaceSize();
        WorldCameraTransform.Matrix = _worldCanvas.GetCameraMatrix(viewportSize);
        _worldCanvas.EnsureExpanded(viewportSize);
    }

    private void EnsureToolBuffers()
    {
        var toolCount = 0;
        foreach (UIElement child in WorldLayer.Children)
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
        var toolIndex = 0;
        foreach (UIElement child in WorldLayer.Children)
        {
            if (child is Button toolButton)
            {
                _toolButtons[toolIndex++] = toolButton;
            }
        }
    }

    private void ValidateToolIdentityMapping()
    {
        var uniqueIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var definition in _toolDefinitionsByVisual.Values)
        {
            if (string.IsNullOrWhiteSpace(definition.Id))
            {
                throw new InvalidOperationException("Tool IDs must not be empty.");
            }

            if (!uniqueIds.Add(definition.Id))
            {
                throw new InvalidOperationException(
                    $"Tool ID '{definition.Id}' is duplicated.");
            }
        }

        var workspaceToolVisuals = new HashSet<FrameworkElement>();
        foreach (UIElement child in WorldLayer.Children)
        {
            if (child is Button toolButton)
            {
                workspaceToolVisuals.Add(toolButton);
            }
        }

        if (workspaceToolVisuals.Count != _toolDefinitionsByVisual.Count)
        {
            throw new InvalidOperationException(
                "Every workspace tool visual must have exactly one tool identity.");
        }

        foreach (var visual in workspaceToolVisuals)
        {
            if (!_toolDefinitionsByVisual.ContainsKey(visual))
            {
                throw new InvalidOperationException(
                    $"Workspace tool visual '{visual.Name}' has no tool identity.");
            }
        }
    }

    private void CaptureInitialToolPositions()
    {
        if (_hasInitialToolPositions)
        {
            return;
        }

        var viewportSize = GetWorkspaceSize();
        _defaultToolWorldPositions.Clear();
        _preferredToolWorldPositions.Clear();
        foreach (var toolButton in _toolButtons)
        {
            var toolId = GetToolDefinition(toolButton).Id;
            var screenPosition = new Point(
                GetFiniteCanvasCoordinate(Canvas.GetLeft(toolButton)),
                GetFiniteCanvasCoordinate(Canvas.GetTop(toolButton)));
            var worldPosition = _worldCanvas.ScreenToWorld(
                screenPosition,
                viewportSize);
            _defaultToolWorldPositions.Add(toolId, worldPosition);
            _preferredToolWorldPositions.Add(toolId, worldPosition);
            Canvas.SetLeft(toolButton, worldPosition.X);
            Canvas.SetTop(toolButton, worldPosition.Y);
        }

        _hasInitialToolPositions = true;
    }

    private void EnsureWorldCanvasInitialized()
    {
        if (_worldCanvas.IsInitialized)
        {
            return;
        }

        if (!_hasInitialToolPositions)
        {
            CaptureInitialToolPositions();
        }

        _worldCanvas.Initialize(
            GetPreferredToolWorldBounds(),
            GetWorkspaceSize());
    }

    private bool TryResetLayout()
    {
        EnsureToolBuffers();
        if (!_hasInitialToolPositions
            || _defaultToolWorldPositions.Count != _toolButtons.Length)
        {
            throw new InvalidOperationException("Default tool positions have not been captured.");
        }

        foreach (var toolButton in _toolButtons)
        {
            var toolId = GetToolDefinition(toolButton).Id;
            if (!_defaultToolWorldPositions.TryGetValue(toolId, out var defaultWorldPosition))
            {
                throw new InvalidOperationException(
                    $"Tool '{toolId}' has no Default world position.");
            }

            _preferredToolWorldPositions[toolId] = defaultWorldPosition;
            Canvas.SetLeft(toolButton, defaultWorldPosition.X);
            Canvas.SetTop(toolButton, defaultWorldPosition.Y);
        }

        _worldCanvas.Shrink(
            GetPreferredToolWorldBounds(),
            GetWorkspaceSize());
        ApplyCameraProjection();
        return true;
    }

    private void UpdatePreferredWorldPosition(Button toolButton)
    {
        var toolId = GetToolDefinition(toolButton).Id;
        _preferredToolWorldPositions[toolId] = new Point(
            GetFiniteCanvasCoordinate(Canvas.GetLeft(toolButton)),
            GetFiniteCanvasCoordinate(Canvas.GetTop(toolButton)));
    }

    private Rect[] GetPreferredToolWorldBounds()
    {
        var bounds = new Rect[_toolButtons.Length];
        for (var index = 0; index < _toolButtons.Length; index++)
        {
            var toolButton = _toolButtons[index];
            var toolId = GetToolDefinition(toolButton).Id;
            if (!_preferredToolWorldPositions.TryGetValue(toolId, out var worldPosition))
            {
                throw new InvalidOperationException(
                    $"Tool '{toolId}' has no Preferred world position.");
            }

            bounds[index] = new Rect(
                worldPosition,
                new Size(toolButton.ActualWidth, toolButton.ActualHeight));
        }

        return bounds;
    }

    private Size GetWorkspaceSize() => new(
        Workspace.ActualWidth,
        Workspace.ActualHeight);

    private ToolDefinition GetToolDefinition(FrameworkElement toolVisual)
    {
        return _toolDefinitionsByVisual.TryGetValue(toolVisual, out var definition)
            ? definition
            : throw new InvalidOperationException(
                $"Tool visual '{toolVisual.Name}' has no ToolDefinition mapping.");
    }

    private void OnWorkspacePreviewMouseDown(
        object sender,
        MouseButtonEventArgs e)
    {
        if (_isPanningWorkspace)
        {
            e.Handled = true;
            return;
        }

        if (e.ChangedButton != MouseButton.Middle || _isDragging)
        {
            return;
        }

        CancelToolPressCandidate();
        if (Mouse.Captured is not null || !Workspace.CaptureMouse())
        {
            return;
        }

        _workspacePanLastPoint = e.GetPosition(Workspace);
        _isPanningWorkspace = true;
        e.Handled = true;
    }

    private void OnWorkspacePreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanningWorkspace)
        {
            return;
        }

        if (e.MiddleButton != MouseButtonState.Pressed)
        {
            EndWorkspacePan();
            return;
        }

        var currentPoint = e.GetPosition(Workspace);
        var screenDelta = currentPoint - _workspacePanLastPoint;
        _workspacePanLastPoint = currentPoint;
        _worldCanvas.PanByScreenDelta(screenDelta);
        ApplyCameraProjection();
        e.Handled = true;
    }

    private void OnWorkspacePreviewMouseUp(
        object sender,
        MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle || !_isPanningWorkspace)
        {
            return;
        }

        EndWorkspacePan();
        e.Handled = true;
    }

    private void OnWorkspaceLostMouseCapture(object sender, MouseEventArgs e)
    {
        if (_isPanningWorkspace && Mouse.Captured != Workspace)
        {
            EndWorkspacePan();
        }
    }

    private void EndWorkspacePan()
    {
        if (!_isPanningWorkspace)
        {
            return;
        }

        _isPanningWorkspace = false;
        if (Mouse.Captured == Workspace)
        {
            Workspace.ReleaseMouseCapture();
        }

        _worldCanvas.Shrink(
            GetPreferredToolWorldBounds(),
            GetWorkspaceSize());
    }

    private void OnWorkspacePreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        e.Handled = true;
        if (_isDragging || _isPanningWorkspace || !_worldCanvas.IsInitialized)
        {
            return;
        }

        CancelToolPressCandidate();
        var viewportSize = GetWorkspaceSize();
        var newZoom = _worldCanvas.ZoomScale
            * Math.Pow(WorldCanvasState.ZoomFactorPerNotch, e.Delta / 120d);
        if (_worldCanvas.ZoomAtScreenPoint(e.GetPosition(Workspace), viewportSize, newZoom))
        {
            ClearBoundaryFeedback();
            ApplyCameraProjection();
        }
    }

    private void OnResetViewClick(object sender, RoutedEventArgs e)
    {
        EndWorkspacePan();
        CancelActiveToolInteraction();
        _worldCanvas.ResetView();
        ClearBoundaryFeedback();
        ApplyCameraProjection();
        _worldCanvas.Shrink(
            GetPreferredToolWorldBounds(),
            GetWorkspaceSize());
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
                EndDragging(toolButton, commitPosition: true);
                return;
            }

            MoveDraggedTool(toolButton, e.GetPosition(Workspace));
            e.Handled = true;
            return;
        }

        AnimatePointerPosition(toolButton, e.GetPosition(toolButton));

        if ((_isLongPressPending || _isToolClickCandidate)
            && ReferenceEquals(_pressedTool, toolButton))
        {
            var currentPoint = e.GetPosition(Workspace);
            var delta = currentPoint - _pressPointInWorkspace;
            if ((delta.X * delta.X) + (delta.Y * delta.Y)
                > LongPressMoveTolerance * LongPressMoveTolerance)
            {
                CancelToolPressCandidate();
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

        if ((_isLongPressPending || _isToolClickCandidate)
            && ReferenceEquals(_pressedTool, toolButton))
        {
            CancelToolPressCandidate();
        }

        AnimateToolOpacity(toolButton, 0, ToolLeaveMilliseconds);
        AnimateToolOffset(toolButton, 0, ToolLeaveMilliseconds);
    }

    private void OnToolButtonContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        e.Handled = true;
    }

    private void OnToolButtonPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var toolButton = (Button)sender;
        CancelToolPressCandidate();

        _pressedTool = toolButton;
        _pressPointInWorkspace = e.GetPosition(Workspace);
        _worldGrabOffset = _worldCanvas.ScreenToWorld(_pressPointInWorkspace, GetWorkspaceSize())
            - new Point(Canvas.GetLeft(toolButton), Canvas.GetTop(toolButton));
        _isLongPressPending = true;
        _isToolClickCandidate = true;
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
            EndDragging(toolButton, commitPosition: true);
        }
        else
        {
            ToolDefinition? toolToOpen = null;
            if (_isToolClickCandidate)
            {
                toolToOpen = _toolDefinitionsByVisual.TryGetValue(toolButton, out var toolDefinition)
                    ? toolDefinition
                    : throw new InvalidOperationException(
                        $"Tool visual '{toolButton.Name}' has no ToolDefinition mapping.");
            }

            CancelToolPressCandidate();
            if (toolToOpen is not null)
            {
                OpenTool(toolToOpen);
            }
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
            CancelToolPressCandidate();
        }
    }

    private void OnWindowDeactivated(object? sender, EventArgs e)
    {
        EndWorkspacePan();
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
            CancelToolPressCandidate();
        }
    }

    private void OnLongPressTimerTick(object? sender, EventArgs e)
    {
        _longPressTimer.Stop();
        if (IsLayoutLocked)
        {
            CancelToolPressCandidate();
            return;
        }

        if (!_isLongPressPending || _pressedTool is not { } toolButton)
        {
            return;
        }

        if (Mouse.LeftButton != MouseButtonState.Pressed)
        {
            CancelToolPressCandidate();
            return;
        }

        _isLongPressPending = false;
        _isToolClickCandidate = false;
        if (!toolButton.CaptureMouse())
        {
            CancelToolPressCandidate();
            return;
        }

        _isDragging = true;
        ClearBoundaryFeedback();
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

    private void CancelToolPressCandidate()
    {
        _longPressTimer.Stop();
        _isLongPressPending = false;
        _isToolClickCandidate = false;
        var toolButton = _pressedTool;
        _pressedTool = null;

        if (toolButton is not null)
        {
            if (Mouse.Captured == toolButton)
            {
                toolButton.ReleaseMouseCapture();
            }

            AnimateToolOffset(
                toolButton,
                toolButton.IsMouseOver ? ToolHoverOffset : 0,
                ToolPressedMilliseconds);
        }
    }

    private void OpenTool(ToolDefinition toolDefinition)
    {
        CancelActiveToolInteraction();
        ClearBoundaryFeedback();
        ClearCollisionContacts();

        if (Workspace.ContextMenu is { IsOpen: true } contextMenu)
        {
            contextMenu.IsOpen = false;
        }

        _activeTool = toolDefinition;
        ToolHostTitle.Text = _activeTool.DisplayName;
        WorkspaceView.IsHitTestVisible = false;
        WorkspaceView.Visibility = Visibility.Hidden;
        ToolHostView.Visibility = Visibility.Visible;
    }

    private void OnToolHostBackClick(object sender, RoutedEventArgs e)
    {
        _activeTool = null;
        ToolHostTitle.Text = string.Empty;
        ToolHostView.Visibility = Visibility.Hidden;
        WorkspaceView.Visibility = Visibility.Visible;
        WorkspaceView.IsHitTestVisible = true;
        ApplyCameraProjection();
    }

    private void MoveDraggedTool(Button toolButton, Point pointerPosition)
    {
        if (IsLayoutLocked)
        {
            return;
        }

        _nextCollisionContacts.Clear();
        _collisionForces.Clear();
        _collisionPressures.Clear();

        var viewportSize = GetWorkspaceSize();
        var toolWorldSize = new Size(toolButton.ActualWidth, toolButton.ActualHeight);
        var bounds = WorkspaceInteraction.GetWorldDragBounds(
            _worldCanvas.GetViewportWorldBounds(viewportSize),
            toolWorldSize,
            _worldCanvas.ZoomScale);
        var currentPosition = new Point(
            GetFiniteCanvasCoordinate(Canvas.GetLeft(toolButton)),
            GetFiniteCanvasCoordinate(Canvas.GetTop(toolButton)));
        var currentX = currentPosition.X;
        var currentY = currentPosition.Y;
        var rawDesiredPosition = _worldCanvas.ScreenToWorld(pointerPosition, viewportSize)
            - _worldGrabOffset;
        var desiredPosition = WorkspaceInteraction.ConstrainDragPosition(
            currentPosition,
            rawDesiredPosition,
            bounds);

        var resolvedX = ResolveHorizontalMovement(
            toolButton,
            currentX,
            currentY,
            desiredPosition.X);
        var resolvedY = ResolveVerticalMovement(
            toolButton,
            resolvedX,
            currentY,
            desiredPosition.Y);

        Canvas.SetLeft(toolButton, resolvedX);
        Canvas.SetTop(toolButton, resolvedY);
        var resolvedPosition = new Point(resolvedX, resolvedY);
        _worldCanvas.EnsureExpanded(
            viewportSize,
            new Rect(resolvedPosition, toolWorldSize));
        var screenBounds = WorkspaceInteraction.GetSoftBounds(
            viewportSize.Width,
            viewportSize.Height,
            toolWorldSize.Width * _worldCanvas.ZoomScale,
            toolWorldSize.Height * _worldCanvas.ZoomScale);
        UpdateBoundaryFeedback(
            toolButton,
            _worldCanvas.WorldToScreen(rawDesiredPosition, viewportSize),
            _worldCanvas.WorldToScreen(resolvedPosition, viewportSize),
            screenBounds);
        UpdateCollisionFeedback(
            toolButton,
            new Vector(
                desiredPosition.X - resolvedX,
                desiredPosition.Y - resolvedY));
    }

    private void EndDragging(Button toolButton, bool commitPosition = false)
    {
        if (!_isDragging || !ReferenceEquals(_pressedTool, toolButton))
        {
            return;
        }

        _longPressTimer.Stop();
        _isLongPressPending = false;
        _isDragging = false;
        _pressedTool = null;
        ClearBoundaryFeedback();
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

        if (commitPosition && !IsLayoutLocked)
        {
            UpdatePreferredWorldPosition(toolButton);
        }
        else
        {
            var preferred = _preferredToolWorldPositions[GetToolDefinition(toolButton).Id];
            Canvas.SetLeft(toolButton, preferred.X);
            Canvas.SetTop(toolButton, preferred.Y);
        }

        var viewportSize = GetWorkspaceSize();
        var toolId = GetToolDefinition(toolButton).Id;
        _worldCanvas.EnsureExpanded(
            viewportSize,
            new Rect(
                _preferredToolWorldPositions[toolId],
                new Size(toolButton.ActualWidth, toolButton.ActualHeight)));
        ApplyCameraProjection();
        _worldCanvas.Shrink(
            GetPreferredToolWorldBounds(),
            viewportSize);
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

        foreach (UIElement child in WorldLayer.Children)
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

        foreach (UIElement child in WorldLayer.Children)
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
        Point actualResolvedPosition,
        SoftWorkspaceBounds bounds)
    {
        var contacts = WorkspaceInteraction.GetPressedBoundaryContacts(
            actualResolvedPosition,
            rawDesiredPosition,
            bounds);
        var enteredContacts = contacts & ~_currentBoundaryContacts;
        var exitedContacts = _currentBoundaryContacts & ~contacts;

        PositionBoundaryHighlightIfPressed(toolButton, contacts, WorkspaceBoundary.Left);
        PositionBoundaryHighlightIfPressed(toolButton, contacts, WorkspaceBoundary.Top);
        PositionBoundaryHighlightIfPressed(toolButton, contacts, WorkspaceBoundary.Right);
        PositionBoundaryHighlightIfPressed(toolButton, contacts, WorkspaceBoundary.Bottom);

        TriggerBoundaryFeedbackIfEntered(toolButton, enteredContacts, WorkspaceBoundary.Left);
        TriggerBoundaryFeedbackIfEntered(toolButton, enteredContacts, WorkspaceBoundary.Top);
        TriggerBoundaryFeedbackIfEntered(toolButton, enteredContacts, WorkspaceBoundary.Right);
        TriggerBoundaryFeedbackIfEntered(toolButton, enteredContacts, WorkspaceBoundary.Bottom);

        ReleaseBoundaryHighlightIfExited(exitedContacts, WorkspaceBoundary.Left);
        ReleaseBoundaryHighlightIfExited(exitedContacts, WorkspaceBoundary.Top);
        ReleaseBoundaryHighlightIfExited(exitedContacts, WorkspaceBoundary.Right);
        ReleaseBoundaryHighlightIfExited(exitedContacts, WorkspaceBoundary.Bottom);

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
            Duration = TimeSpan.FromMilliseconds(BoundaryFeedbackMilliseconds),
            FillBehavior = FillBehavior.Stop
        };
        animation.KeyFrames.Add(new LinearDoubleKeyFrame(
            currentValue,
            KeyTime.FromTimeSpan(TimeSpan.Zero)));
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(
            impactValue,
            KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(BoundaryImpactMilliseconds)))
        {
            EasingFunction = CollisionImpactEasing
        });
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(
            returnValue,
            KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(BoundaryFeedbackMilliseconds)))
        {
            EasingFunction = CollisionReturnEasing
        });
        return animation;
    }

    private void AnimateBoundaryHighlight(WorkspaceBoundary boundary)
    {
        var highlight = GetBoundaryHighlight(boundary);
        var currentOpacity = highlight.Opacity;
        highlight.BeginAnimation(OpacityProperty, null);
        highlight.Opacity = BoundarySustainedOpacity;
        var animation = new DoubleAnimationUsingKeyFrames
        {
            Duration = TimeSpan.FromMilliseconds(BoundaryFeedbackMilliseconds),
            FillBehavior = FillBehavior.Stop
        };
        animation.KeyFrames.Add(new LinearDoubleKeyFrame(
            currentOpacity,
            KeyTime.FromTimeSpan(TimeSpan.Zero)));
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(
            BoundaryPeakOpacity,
            KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(BoundaryImpactMilliseconds)))
        {
            EasingFunction = CollisionImpactEasing
        });
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(
            BoundarySustainedOpacity,
            KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(BoundaryFeedbackMilliseconds)))
        {
            EasingFunction = CollisionReturnEasing
        });
        highlight.BeginAnimation(
            OpacityProperty,
            animation,
            HandoffBehavior.SnapshotAndReplace);
    }

    private void PositionBoundaryHighlightIfPressed(
        Button toolButton,
        WorkspaceBoundary contacts,
        WorkspaceBoundary boundary)
    {
        if ((contacts & boundary) == 0)
        {
            return;
        }

        PositionBoundaryHighlight(GetBoundaryHighlight(boundary), toolButton, boundary);
    }

    private void ReleaseBoundaryHighlightIfExited(
        WorkspaceBoundary exitedContacts,
        WorkspaceBoundary boundary)
    {
        if ((exitedContacts & boundary) == 0)
        {
            return;
        }

        var highlight = GetBoundaryHighlight(boundary);
        var currentOpacity = highlight.Opacity;
        highlight.BeginAnimation(OpacityProperty, null);
        highlight.Opacity = 0;
        if (currentOpacity <= 0.001)
        {
            return;
        }

        highlight.BeginAnimation(
            OpacityProperty,
            new DoubleAnimation
            {
                From = currentOpacity,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(BoundaryReleaseMilliseconds),
                EasingFunction = CollisionReturnEasing,
                FillBehavior = FillBehavior.Stop
            },
            HandoffBehavior.SnapshotAndReplace);
    }

    private Border GetBoundaryHighlight(WorkspaceBoundary boundary)
    {
        return boundary switch
        {
            WorkspaceBoundary.Left => LeftBoundaryHighlight,
            WorkspaceBoundary.Top => TopBoundaryHighlight,
            WorkspaceBoundary.Right => RightBoundaryHighlight,
            WorkspaceBoundary.Bottom => BottomBoundaryHighlight,
            _ => throw new ArgumentOutOfRangeException(nameof(boundary))
        };
    }

    private void ClearBoundaryFeedback()
    {
        _currentBoundaryContacts = WorkspaceBoundary.None;
        ClearBoundaryHighlight(LeftBoundaryHighlight);
        ClearBoundaryHighlight(TopBoundaryHighlight);
        ClearBoundaryHighlight(RightBoundaryHighlight);
        ClearBoundaryHighlight(BottomBoundaryHighlight);
    }

    private static void ClearBoundaryHighlight(Border highlight)
    {
        highlight.BeginAnimation(OpacityProperty, null);
        highlight.Opacity = 0;
    }

    private Rect GetToolScreenBounds(Button toolButton)
    {
        var position = _worldCanvas.WorldToScreen(
            new Point(Canvas.GetLeft(toolButton), Canvas.GetTop(toolButton)),
            GetWorkspaceSize());
        return new Rect(
            position,
            new Size(
                toolButton.ActualWidth * _worldCanvas.ZoomScale,
                toolButton.ActualHeight * _worldCanvas.ZoomScale));
    }

    private void PositionBoundaryHighlight(
        Border highlight,
        Button toolButton,
        WorkspaceBoundary boundary)
    {
        if (highlight.RenderTransform is not TranslateTransform offset)
        {
            throw new InvalidOperationException("Boundary highlight requires a TranslateTransform.");
        }

        var toolScreenBounds = GetToolScreenBounds(toolButton);
        var toolLeft = toolScreenBounds.Left;
        var toolTop = toolScreenBounds.Top;
        if (boundary is WorkspaceBoundary.Left or WorkspaceBoundary.Right)
        {
            var maximumOffset = Math.Max(0, Workspace.ActualHeight - highlight.Height);
            offset.X = 0;
            offset.Y = Math.Clamp(
                toolTop + (toolScreenBounds.Height / 2) - (highlight.Height / 2),
                0,
                maximumOffset);
            return;
        }

        var maximumHorizontalOffset = Math.Max(0, Workspace.ActualWidth - highlight.Width);
        offset.X = Math.Clamp(
            toolLeft + (toolScreenBounds.Width / 2) - (highlight.Width / 2),
            0,
            maximumHorizontalOffset);
        offset.Y = 0;
    }

    private void SpawnBoundaryParticles(Button toolButton, WorkspaceBoundary boundary)
    {
        var random = Random.Shared;
        var particleCount = random.Next(7, 15);
        var toolScreenBounds = GetToolScreenBounds(toolButton);
        var toolLeft = toolScreenBounds.Left;
        var toolTop = toolScreenBounds.Top;

        for (var index = 0; index < particleCount; index++)
        {
            var size = 2 + (random.NextDouble() * 2.5);
            var tangentFraction = 0.125 + (random.NextDouble() * 0.75);
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
                    startTop += toolScreenBounds.Height * tangentFraction - (size / 2);
                    driftX = normalDrift;
                    driftY = tangentDrift;
                    break;
                case WorkspaceBoundary.Top:
                    startLeft += toolScreenBounds.Width * tangentFraction - (size / 2);
                    startTop = WorkspaceInteraction.SoftBoundaryPadding - (size / 2);
                    driftX = tangentDrift;
                    driftY = normalDrift;
                    break;
                case WorkspaceBoundary.Right:
                    startLeft = Workspace.ActualWidth
                        - WorkspaceInteraction.SoftBoundaryPadding
                        - (size / 2);
                    startTop += toolScreenBounds.Height * tangentFraction - (size / 2);
                    driftX = -normalDrift;
                    driftY = tangentDrift;
                    break;
                case WorkspaceBoundary.Bottom:
                    startLeft += toolScreenBounds.Width * tangentFraction - (size / 2);
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
            WorldLayer.Children.Add(particle);

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
            fade.Completed += (_, _) => WorldLayer.Children.Remove(particle);
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
            WorldLayer.Children.Add(particle);

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
            fade.Completed += (_, _) => WorldLayer.Children.Remove(particle);
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

    private void OnLayoutLockClick(object sender, RoutedEventArgs e)
    {
        var shouldLock = !IsLayoutLocked;
        if (shouldLock)
        {
            CancelActiveToolInteraction();
        }

        IsLayoutLocked = shouldLock;
        UpdateLayoutControlVisuals();
    }

    private void OnResetLayoutClick(object sender, RoutedEventArgs e)
    {
        CancelActiveToolInteraction();
        TryResetLayout();
    }

    private void CancelActiveToolInteraction()
    {
        if (_isDragging && _pressedTool is { } draggedTool)
        {
            EndDragging(draggedTool);
            return;
        }

        if (_isLongPressPending || _isToolClickCandidate)
        {
            CancelToolPressCandidate();
            return;
        }

        _longPressTimer.Stop();
        _pressedTool = null;
    }

    private void UpdateLayoutControlVisuals()
    {
        LayoutLockGlyph.Text = IsLayoutLocked ? "\uE72E" : "\uE785";
        LayoutLockButton.ToolTip = IsLayoutLocked ? "解锁布局" : "锁定布局";
        WorkspaceLockMenuItem.IsChecked = IsLayoutLocked;
        AutomationProperties.SetName(
            LayoutLockButton,
            IsLayoutLocked ? "解锁布局" : "锁定布局");
        UpdateLayoutControlButtonAppearance(LayoutLockButton, IsLayoutLocked);
    }

    private static void UpdateLayoutControlButtonAppearance(Button button, bool isActive)
    {
        button.Background = isActive
            ? ActiveLayoutControlBackground
            : Brushes.Transparent;
        button.Foreground = isActive
            ? ActiveLayoutControlForeground
            : InactiveLayoutControlForeground;
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
        ScheduleCameraProjection();
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
        WindowInnerOutline.CornerRadius = innerRadius;
        WindowEdgeShade.CornerRadius = innerRadius;
    }
}
