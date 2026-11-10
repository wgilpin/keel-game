using Godot;
using Keel.Sim.Flight;
using SimVector2 = System.Numerics.Vector2;
using SimVector3 = System.Numerics.Vector3;

namespace Keel.Game.Input;

public sealed class ShipInputReader
{
    private SimVector2 _accumulatedMouseDelta;
    private bool _pendingCaptureToggle;
    private bool _pendingRecaptureClick;
    private bool _pendingDampenerToggle;
    private bool _pendingInteractToggle;
    private int? _pendingSlotSelection;

    public void HandleInputEvent(InputEvent @event, bool isMouseCaptured)
    {
        if (@event is InputEventKey { Pressed: true, Echo: false } keyEvent)
        {
            Key key = keyEvent.PhysicalKeycode != Key.None ? keyEvent.PhysicalKeycode : keyEvent.Keycode;
            if (key == Key.Escape)
            {
                _pendingCaptureToggle = true;
            }
            else if (key == Key.Z)
            {
                _pendingDampenerToggle = true;
            }
            else if (key == Key.F)
            {
                _pendingInteractToggle = true;
            }
            else if (key is Key.Key1 or Key.Kp1)
            {
                _pendingSlotSelection = 1;
            }
            else if (key is Key.Key2 or Key.Kp2)
            {
                _pendingSlotSelection = 2;
            }
            else if (key is Key.Key3 or Key.Kp3)
            {
                _pendingSlotSelection = 3;
            }
            else if (key is Key.Key4 or Key.Kp4)
            {
                _pendingSlotSelection = 4;
            }
            else if (key is Key.Key5 or Key.Kp5)
            {
                _pendingSlotSelection = 5;
            }
        }
        else if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } && !isMouseCaptured)
        {
            _pendingRecaptureClick = true;
        }
        else if (@event is InputEventMouseMotion motion && isMouseCaptured)
        {
            _accumulatedMouseDelta += new SimVector2(motion.Relative.X, motion.Relative.Y);
        }
    }

    public bool ConsumeMouseCaptureToggle()
    {
        bool toggled = _pendingCaptureToggle;
        _pendingCaptureToggle = false;
        return toggled;
    }

    public bool ConsumeMouseRecaptureClick()
    {
        bool clicked = _pendingRecaptureClick;
        _pendingRecaptureClick = false;
        return clicked;
    }

    public bool ConsumeDampenerToggle()
    {
        bool toggled = _pendingDampenerToggle;
        _pendingDampenerToggle = false;
        return toggled;
    }

    public bool ConsumeInteractToggle()
    {
        bool toggled = _pendingInteractToggle;
        _pendingInteractToggle = false;
        return toggled;
    }

    public int? ConsumeSlotSelection()
    {
        int? slot = _pendingSlotSelection;
        _pendingSlotSelection = null;
        return slot;
    }

    public bool IsFireHeld(bool isMouseCaptured) =>
        isMouseCaptured && Godot.Input.IsMouseButtonPressed(MouseButton.Left);

    public FlightInput ConsumeFlightInput(bool isMouseCaptured)
    {
        SimVector2 mouseDelta = isMouseCaptured ? _accumulatedMouseDelta : SimVector2.Zero;
        _accumulatedMouseDelta = SimVector2.Zero;

        float strafeX = Axis(Key.A, Key.D);
        float verticalY = Axis(Key.Ctrl, Key.Space);
        float foreAftZ = Axis(Key.W, Key.S); // Godot local forward is -Z (W = -1, S = +1)
        float roll = Axis(Key.E, Key.Q);     // Right-hand rule around +Z: Q (+1) rolls left, E (-1) rolls right

        return new FlightInput(
            new SimVector3(strafeX, verticalY, foreAftZ),
            mouseDelta,
            roll);
    }

    private static float Axis(Key negative, Key positive)
    {
        float neg = IsPressed(negative) ? 1f : 0f;
        float pos = IsPressed(positive) ? 1f : 0f;
        return pos - neg;
    }

    private static bool IsPressed(Key key) =>
        Godot.Input.IsPhysicalKeyPressed(key) || Godot.Input.IsKeyPressed(key);
}
