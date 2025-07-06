#if GODOT && !GODOT4_0_OR_GREATER
using Godot;
using NECS.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

public partial class InputEx : Node
{
    public Vector2 MousePosition => GetViewport().GetMousePosition();
    public Vector2[] TouchpadPositions => TouchpadPositionsCache.Values.Where(x => x.Sync).Select(x => x.Position).ToArray();
    private IDictionary<int, TouchRecord> TouchpadPositionsCache = new OrderedDictionary<int, TouchRecord>();
    private IDictionary<Type, List<HandlerRecord>> eventHandlers = new ConcurrentDictionary<Type, List<HandlerRecord>>();
    private IDictionary<string, InputObjectState> InputMapState = new ConcurrentDictionary<string, InputObjectState>();
    public bool LockInput;

    public static InputEx Init(Node parentNode)
    {
        var node = new InputEx();
        parentNode.AddChild(node);
        return node;
    }

    public override void _Ready()
    {
        base._Ready();
    }

    public override void _Process(float delta)
    {
        foreach(var inputState in InputMapState)
        {
            switch(inputState.Value)
            {
                case InputObjectState.NoSyncEntered:
                    InputMapState[inputState.Key] = InputObjectState.Entered;
                    break;
                case InputObjectState.Entered:
                    InputMapState[inputState.Key] = InputObjectState.Pressed;
                    break;
                case InputObjectState.NoSyncReleased:
                    InputMapState[inputState.Key] = InputObjectState.Released;
                    break;
                case InputObjectState.Released:
                    InputMapState[inputState.Key] = InputObjectState.Quiet;
                    break;
            }
        }
        foreach(var touch in TouchpadPositionsCache)
        {
            if(!touch.Value.Sync)
            {
                touch.Value.Sync = true;
            }
            else
            {
                TouchpadPositionsCache.Remove(touch);
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if(eventHandlers.TryGetValue(@event.GetType(), out var handlers))
        {
            foreach(var handler in handlers)
            {
                handler.Handler(@event);
            }
        }

        if(@event is InputEventScreenTouch && false)//need fix
        {
            TouchpadPositionsCache[(@event as InputEventScreenTouch).Index] = new TouchRecord()
            {
                Position = (@event as InputEventScreenTouch).Position,
                Sync = false
            };
        }

        if (@event is InputEventScreenDrag && false)//need fix
        {
            TouchpadPositionsCache[(@event as InputEventScreenDrag).Index] = new TouchRecord()
            {
                Position = (@event as InputEventScreenDrag).Position,
                Sync = false
            };
        }

        if (@event.IsPressed() && !(@event is InputEventMouseMotion) && !(@event is InputEventScreenDrag))
        {
            switch (@event.GetType())
            {
                case var type when type == typeof(InputEventKey):
                    InputMapState[@event.GetType().ToString() + ((@event as InputEventKey).PhysicalScancode).ToString()] = InputObjectState.NoSyncEntered;
                    break;
                case var type when type == typeof(InputEventMouseButton):
                    InputMapState[@event.GetType().ToString() + (@event as InputEventMouseButton).ButtonIndex.ToString()] = InputObjectState.NoSyncEntered;
                    break;
                case var type when type == typeof(InputEventScreenTouch):
                    break;//need fix
                    InputMapState[@event.GetType().ToString() + (@event as InputEventScreenTouch).Index.ToString()] = InputObjectState.NoSyncEntered;
                    break;
                default:
                    InputMapState[@event.GetType().ToString()] = InputObjectState.NoSyncEntered;
                    break;
            }
        }
        if(@event.IsReleased() && !(@event is InputEventMouseMotion) && !(@event is InputEventScreenDrag))
        {
            switch (@event.GetType())
            {
                case var type when type == typeof(InputEventKey):
                    InputMapState[@event.GetType().ToString() + ((@event as InputEventKey).PhysicalScancode).ToString()] = InputObjectState.NoSyncReleased;
                    break;
                case var type when type == typeof(InputEventMouseButton):
                    InputMapState[@event.GetType().ToString() + (@event as InputEventMouseButton).ButtonIndex.ToString()] = InputObjectState.NoSyncReleased;
                    break;
                case var type when type == typeof(InputEventScreenTouch):
                    break;//need fix
                    InputMapState[@event.GetType().ToString() + (@event as InputEventScreenTouch).Index.ToString()] = InputObjectState.NoSyncReleased;
                    break;
                default:
                    InputMapState[@event.GetType().ToString()] = InputObjectState.NoSyncReleased;
                    break;
            }
        }
    }

    public void AddHandler<T>(Action<InputEvent> handler, string tag = "") where T : InputEvent
    {
        List<HandlerRecord> list = null;
        if(!eventHandlers.TryGetValue(typeof(T), out list))
        {
            eventHandlers[typeof(T)] = new List<HandlerRecord>();
            list = eventHandlers[typeof(T)];
        }

        list.Add(new HandlerRecord() { Handler = handler, Tag = tag });
    }

    public void RemoveHandlerByType<T>() where T : InputEvent
    {
        List<HandlerRecord> list = null;
        if (!eventHandlers.TryGetValue(typeof(T), out list))
        {
            eventHandlers[typeof(T)] = new List<HandlerRecord>();
            list = eventHandlers[typeof(T)];
        }
        eventHandlers.Remove(typeof(T));
    }

    public void RemoveHandlerByTag(string tag)
    {
        for(int i = 0; i < eventHandlers.Values.Count; i++) 
        {
            var list = eventHandlers.Values.ElementAt(i);
            for (int j = 0; j < list.Count; j++ )
            {
                if (list[j].Tag == tag)
                {
                    list.RemoveAt(j);
                    j--;
                }
            }
        }
    }

    public void ClearHandlers()
    {
        eventHandlers.Clear();
    }
#region isInputBlock
    public InputObjectState GetKeyState(Godot.KeyList key)
    {
        string keyId = typeof(InputEventKey).ToString() + ((int)key).ToString();
        
        // Добавляем двойную проверку - и в нашем состоянии, и физическое состояние
        bool isPhysicallyPressed = Input.IsPhysicalKeyPressed((int)key);
        
        if (isPhysicallyPressed)
        {
            // Если клавиша физически нажата, но в нашей системе она не отмечена как нажатая
            if (!InputMapState.TryGetValue(keyId, out var state) || 
                (state != InputObjectState.Pressed && state != InputObjectState.Entered))
            {
                // Динамически обновляем состояние
                InputMapState[keyId] = InputObjectState.Pressed;
                return InputObjectState.Pressed;
            }
        }
        else
        {
            // Если клавиша физически не нажата, но в нашей системе она отмечена как нажатая
            if (InputMapState.TryGetValue(keyId, out var state) && 
                (state == InputObjectState.Pressed || state == InputObjectState.Entered))
            {
                // Динамически обновляем состояние
                InputMapState[keyId] = InputObjectState.Released;
                return InputObjectState.Released;
            }
        }
        
        // Стандартная логика проверки состояния
        if (InputMapState.TryGetValue(keyId, out var lastState))
        {
            return NonSyncReplacer(lastState);
        }
        
        // Если клавиша физически нажата, но еще не в нашей системе
        if (isPhysicallyPressed)
        {
            InputMapState[keyId] = InputObjectState.Pressed;
            return InputObjectState.Pressed;
        }
        
        return InputObjectState.Quiet;
    }

    // Измененный метод GetKeyStateF
    public float GetKeyStateF(Godot.KeyList key)
    {
        // Двойная проверка - и физическое нажатие, и состояние в системе
        bool isPhysicallyPressed = Input.IsPhysicalKeyPressed((int)key);
        
        if (isPhysicallyPressed)
        {
            return 1f;
        }
        
        InputObjectState state = GetKeyState(key);
        if (state == InputObjectState.Entered || state == InputObjectState.Pressed)
        {
            return 1f;
        }
        
        return 0f;
    }

    public InputObjectState GetMouseState(ButtonList button)
    {
        var buttonn = typeof(InputEventMouseButton).ToString() + ((int)button).ToString();
        if (InputMapState.TryGetValue(buttonn, out var lastState))
        {
            return NonSyncReplacer(lastState);
        }
        return InputObjectState.Quiet;
    }

    public InputObjectState GetTouchState(int index = 0)
    {
        if (InputMapState.TryGetValue(typeof(InputEventScreenTouch).ToString() + index.ToString(), out var lastState))
        {
            return NonSyncReplacer(lastState);
        }
        return InputObjectState.Quiet;
    }

    private InputObjectState NonSyncReplacer(InputObjectState state)
    {
        switch(state)
        {
            case InputObjectState.NoSyncEntered:
                return InputObjectState.Quiet;
            case InputObjectState.NoSyncReleased:
                return InputObjectState.Pressed;
        }
        return state;
    }
    #endregion
    public struct HandlerRecord
    {
        public string Tag;
        public Action<InputEvent> Handler;
    }

    public class TouchRecord
    {
        public bool Sync;
        public Vector2 Position;
    }
}
/// <summary>
/// NoSync state - non stabilized state with Process iteration and commonly non usable in wild
/// </summary>
public enum InputObjectState
{
    Quiet,
    Entered,
    NoSyncEntered,
    Pressed,
    Released,
    NoSyncReleased
}
#endif
#if GODOT4_0_OR_GREATER
using Godot;
using NECS.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

public partial class InputEx : Node
{
    public Vector2 MousePosition => GetViewport().GetMousePosition();
    public Vector2[] TouchpadPositions => TouchpadPositionsCache.Values.Where(x => x.Sync).Select(x => x.Position).ToArray();
    private IDictionary<int, TouchRecord> TouchpadPositionsCache = new OrderedDictionary<int, TouchRecord>();
    private IDictionary<Type, List<HandlerRecord>> eventHandlers = new ConcurrentDictionary<Type, List<HandlerRecord>>();
    private IDictionary<string, InputObjectState> InputMapState = new ConcurrentDictionary<string, InputObjectState>();
    public bool LockInput;

    public static InputEx Init(Node parentNode)
    {
        var node = new InputEx();
        parentNode.AddChild(node);
        return node;
    }

    public override void _Ready()
    {
        base._Ready();
    }

    public override void _Process(double delta)
    {
        foreach(var inputState in InputMapState)
        {
            switch(inputState.Value)
            {
                case InputObjectState.NoSyncEntered:
                    InputMapState[inputState.Key] = InputObjectState.Entered;
                    break;
                case InputObjectState.Entered:
                    InputMapState[inputState.Key] = InputObjectState.Pressed;
                    break;
                case InputObjectState.NoSyncReleased:
                    InputMapState[inputState.Key] = InputObjectState.Released;
                    break;
                case InputObjectState.Released:
                    InputMapState[inputState.Key] = InputObjectState.Quiet;
                    break;
            }
        }
        foreach(var touch in TouchpadPositionsCache)
        {
            if(!touch.Value.Sync)
            {
                touch.Value.Sync = true;
            }
            else
            {
                TouchpadPositionsCache.Remove(touch);
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if(eventHandlers.TryGetValue(@event.GetType(), out var handlers))
        {
            foreach(var handler in handlers)
            {
                handler.Handler(@event);
            }
        }

        if(@event is InputEventScreenTouch)
        {
            TouchpadPositionsCache[(@event as InputEventScreenTouch).Index] = new TouchRecord()
            {
                Position = (@event as InputEventScreenTouch).Position,
                Sync = false
            };
        }

        if (@event is InputEventScreenDrag)
        {
            TouchpadPositionsCache[(@event as InputEventScreenDrag).Index] = new TouchRecord()
            {
                Position = (@event as InputEventScreenDrag).Position,
                Sync = false
            };
        }

        if (@event.IsPressed() && !(@event is InputEventMouseMotion) && !(@event is InputEventScreenDrag))
        {
            switch (@event.GetType())
            {
                case var type when type == typeof(InputEventKey):
                    InputMapState[@event.GetType().ToString() + ((@event as InputEventKey).PhysicalKeycode).ToString()] = InputObjectState.NoSyncEntered;
                    break;
                case var type when type == typeof(InputEventMouseButton):
                    InputMapState[@event.GetType().ToString() + (@event as InputEventMouseButton).ButtonIndex.ToString()] = InputObjectState.NoSyncEntered;
                    break;
                case var type when type == typeof(InputEventScreenTouch):
                    InputMapState[@event.GetType().ToString() + (@event as InputEventScreenTouch).Index.ToString()] = InputObjectState.NoSyncEntered;
                    break;
                default:
                    InputMapState[@event.GetType().ToString()] = InputObjectState.NoSyncEntered;
                    break;
            }
        }
        if(@event.IsReleased() && !(@event is InputEventMouseMotion) && !(@event is InputEventScreenDrag))
        {
            switch (@event.GetType())
            {
                case var type when type == typeof(InputEventKey):
                    InputMapState[@event.GetType().ToString() + ((@event as InputEventKey).PhysicalKeycode).ToString()] = InputObjectState.NoSyncReleased;
                    break;
                case var type when type == typeof(InputEventMouseButton):
                    InputMapState[@event.GetType().ToString() + (@event as InputEventMouseButton).ButtonIndex.ToString()] = InputObjectState.NoSyncReleased;
                    break;
                case var type when type == typeof(InputEventScreenTouch):
                    InputMapState[@event.GetType().ToString() + (@event as InputEventScreenTouch).Index.ToString()] = InputObjectState.NoSyncReleased;
                    break;
                default:
                    InputMapState[@event.GetType().ToString()] = InputObjectState.NoSyncReleased;
                    break;
            }
        }
    }

    public void AddHandler<T>(Action<InputEvent> handler, string tag = "") where T : InputEvent
    {
        List<HandlerRecord> list = null;
        if(!eventHandlers.TryGetValue(typeof(T), out list))
        {
            eventHandlers[typeof(T)] = new List<HandlerRecord>();
            list = eventHandlers[typeof(T)];
        }

        list.Add(new HandlerRecord() { Handler = handler, Tag = tag });
    }

    public void RemoveHandlerByType<T>() where T : InputEvent
    {
        List<HandlerRecord> list = null;
        if (!eventHandlers.TryGetValue(typeof(T), out list))
        {
            eventHandlers[typeof(T)] = new List<HandlerRecord>();
            list = eventHandlers[typeof(T)];
        }
        eventHandlers.Remove(typeof(T));
    }

    public void RemoveHandlerByTag(string tag)
    {
        for(int i = 0; i < eventHandlers.Values.Count; i++) 
        {
            var list = eventHandlers.Values.ElementAt(i);
            for (int j = 0; j < list.Count; j++ )
            {
                if (list[j].Tag == tag)
                {
                    list.RemoveAt(j);
                    j--;
                }
            }
        }
    }

    public void ClearHandlers()
    {
        eventHandlers.Clear();
    }
#region isInputBlock
    public InputObjectState GetKeyState(Godot.Key key)
    {
        if(InputMapState.TryGetValue(typeof(InputEventKey).ToString() + key.ToString(), out var lastState))
        {
            return NonSyncReplacer(lastState);
        }
        return InputObjectState.Quiet;
    }

    public InputObjectState GetMouseState(Godot.MouseButton button)
    {
        if (InputMapState.TryGetValue(typeof(InputEventMouseButton).ToString() + button.ToString(), out var lastState))
        {
            return NonSyncReplacer(lastState);
        }
        return InputObjectState.Quiet;
    }

    public InputObjectState GetTouchState(int index = 0)
    {
        if (InputMapState.TryGetValue(typeof(InputEventScreenTouch).ToString() + index.ToString(), out var lastState))
        {
            return NonSyncReplacer(lastState);
        }
        return InputObjectState.Quiet;
    }

    private InputObjectState NonSyncReplacer(InputObjectState state)
    {
        switch(state)
        {
            case InputObjectState.NoSyncEntered:
                return InputObjectState.Quiet;
            case InputObjectState.NoSyncReleased:
                return InputObjectState.Pressed;
        }
        return state;
    }
    #endregion
    public struct HandlerRecord
    {
        public string Tag;
        public Action<InputEvent> Handler;
    }

    public class TouchRecord
    {
        public bool Sync;
        public Vector2 Position;
    }
}
/// <summary>
/// NoSync state - non stabilized state with Process iteration and commonly non usable in wild
/// </summary>
public enum InputObjectState
{
    Quiet,
    Entered,
    NoSyncEntered,
    Pressed,
    Released,
    NoSyncReleased
}
#endif
