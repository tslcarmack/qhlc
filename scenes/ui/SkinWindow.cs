using Godot;
using Qhlc.Core.Public;

namespace Qhlc.Game;

public partial class SkinWindow : Control
{
    [Export] public float TitleBarHeight { get; set; } = 28f;
    [Export] public bool EnableDrag { get; set; } = true;
    [Export] public bool EnableClose { get; set; } = true;

    [Signal]
    public delegate void ClosedEventHandler();

    private bool _dragging;
    private Vector2 _dragOffset;

    public override void _Ready()
    {
        var title = GetNode<Control>("TitleBarHit");
        title.GuiInput += OnTitleGuiInput;
        title.CustomMinimumSize = new Vector2(0, TitleBarHeight);
        title.Size = new Vector2(Size.X, TitleBarHeight);

        var close = GetNodeOrNull<Button>("CloseButton");
        if (close is not null)
        {
            close.Visible = EnableClose;
            close.Pressed += () =>
            {
                Hide();
                EmitSignal(SignalName.Closed);
            };
        }
    }

    public override void _Input(InputEvent e)
    {
        if (!_dragging || !EnableDrag)
            return;

        if (e is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left && !mb.Pressed)
        {
            _dragging = false;
            return;
        }

        if (e is InputEventMouseMotion)
            ApplyDragPosition();
    }

    private void OnTitleGuiInput(InputEvent e)
    {
        if (!EnableDrag)
            return;

        if (e is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Left)
        {
            if (mb.Pressed)
            {
                _dragging = true;
                _dragOffset = GetGlobalMousePosition() - GlobalPosition;
                MoveToFront();
            }
            else
                _dragging = false;
        }
        else if (e is InputEventMouseMotion && _dragging)
            ApplyDragPosition();
    }

    private void ApplyDragPosition()
    {
        var parent = GetParent() as Control;
        var parentSize = parent?.Size ?? GetViewport().GetVisibleRect().Size;
        var desiredGlobal = GetGlobalMousePosition() - _dragOffset;
        var local = parent is null
            ? desiredGlobal
            : parent.GetGlobalTransformWithCanvas().AffineInverse() * desiredGlobal;
        var (cx, cy) = SkinUiMath.ClampPosition(
            local.X, local.Y, Size.X, Size.Y, parentSize.X, parentSize.Y);
        Position = new Vector2(cx, cy);
    }
}
