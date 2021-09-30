using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.VisualScripting.Interpreter
{
    [ConstantNode(typeof(int))]
    public struct ConstantInt : IConstantNode
    {
        public int Value;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Output, Value);
        }
    }

    [ConstantNode(typeof(float))]
    public struct ConstantFloat : IConstantNode
    {
        public float Value;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Output, Value);
        }
    }

    [ConstantNode(typeof(Object))]
    public struct ConstantUnityObject : IConstantNode
    {
        public Object Value;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Output, Interpreter.Value.FromObject(Value));
        }
    }

    [ConstantNode(typeof(string))]
    public struct ConstantString : IConstantNode
    {
        public string Value;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Output, Interpreter.Value.FromObject(Value));
        }
    }

    [ConstantNode(typeof(Vector2))]
    public struct ConstantVector2 : IConstantNode
    {
        public Vector2 Value;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Output, Value);
        }
    }

    [ConstantNode(typeof(Vector3))]
    public struct ConstantVector3 : IConstantNode
    {
        public Vector3 Value;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Output, Value);
        }
    }

    [ConstantNode(typeof(Vector4))]
    public struct ConstantVector4 : IConstantNode
    {
        public Vector4 Value;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Output, Value);
        }
    }

    [ConstantNode(typeof(Quaternion))]
    public struct ConstantQuaternion : IConstantNode
    {
        public Quaternion Value;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Output, Value);
        }
    }

    [ConstantNode(typeof(Color))]
    public struct ConstantColor : IConstantNode
    {
        public Color Value;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Output, Value);
        }
    }

    [ConstantNode(typeof(bool))]
    public struct ConstantBoolean : IConstantNode
    {
        public bool Value;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Output, Value);
        }
    }

    [ConstantNode(typeof(Value))]
    public struct ConstantValue : IConstantNode
    {
        public Value Value;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Output, Value);
        }
    }

    [ConstantNode(typeof(Enum))]
    public struct ConstantEnum : IConstantNode
    {
        [SerializeReference]
        public Enum Value;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var v = new Value();
            if (Value != null)
                v.SetEnumValue(Value);
            ctx.Write(Output, v);
        }
    }

    [ConstantNode(typeof(Ray))]
    public struct ConstantRay : IConstantNode
    {
        public Vector3 Origin;
        public Vector3 Direction;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Output, Value.FromObject(new Ray(Origin, Direction)));
        }
    }

    [ConstantNode(typeof(Type))]
    public struct ConstantType : IConstantNode
    {
        public SerializableType Type;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Output, Value.FromObject(System.Type.GetType(Type.Identification)));
        }
    }

    [ConstantNode(typeof(Ray2D))]
    public struct ConstantRay2D : IConstantNode
    {
        public Vector2 Origin;
        public Vector2 Direction;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Output, Value.FromObject(new Ray2D(Origin, Direction)));
        }
    }

    [ConstantNode(typeof(Rect))]
    public struct ConstantRect : IConstantNode
    {
        public Rect Value;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Output, Interpreter.Value.FromObject(Value));
        }
    }

    [ConstantNode(typeof(Bounds))]
    public struct ConstantBounds : IConstantNode
    {
        public Bounds Value;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Output, Interpreter.Value.FromObject(Value));
        }
    }

    [ConstantNode(typeof(LayerMask))]
    public struct ConstantLayerMask : IConstantNode
    {
        public LayerMask Value;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Output, Interpreter.Value.FromObject(Value));
        }
    }

    [ConstantNode(typeof(AnimationCurve))]
    public struct ConstantAnimationCurve : IConstantNode
    {
        public AnimationCurve Value;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            ctx.Write(Output, Interpreter.Value.FromObject(Value));
        }
    }
}
