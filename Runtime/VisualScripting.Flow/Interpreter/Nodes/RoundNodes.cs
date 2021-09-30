using Unity.VisualScripting;
using UnityEngine;

namespace Unity.VisualScripting.Interpreter
{
    [NodeDescription(typeof(ScalarRound), SpecializationOf = ScalarRound.Rounding.Floor)]
    public struct ScalarRoundFloor : IDataNode
    {
        public InputDataPort Input;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance => ctx.Write(Output, Mathf.FloorToInt(ctx.ReadFloat(Input)));
    }

    [NodeDescription(typeof(ScalarRound), SpecializationOf = ScalarRound.Rounding.AwayFromZero)]
    public struct ScalarRoundRound : IDataNode
    {
        public InputDataPort Input;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance => ctx.Write(Output, Mathf.RoundToInt(ctx.ReadFloat(Input)));
    }

    [NodeDescription(typeof(ScalarRound), SpecializationOf = ScalarRound.Rounding.Ceiling)]
    public struct ScalarRoundCeiling : IDataNode
    {
        public InputDataPort Input;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance => ctx.Write(Output, Mathf.CeilToInt(ctx.ReadFloat(Input)));
    }

    [NodeDescription(typeof(Vector2Round), SpecializationOf = Vector2Round.Rounding.Floor)]
    public struct Vector2RoundFloor : IDataNode
    {
        public InputDataPort Input;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var input = ctx.ReadVector2(Input);
            ctx.Write(Output, new Vector2(Mathf.FloorToInt(input.x), Mathf.FloorToInt(input.y)));
        }
    }

    [NodeDescription(typeof(Vector2Round), SpecializationOf = Vector2Round.Rounding.AwayFromZero)]
    public struct Vector2RoundRound : IDataNode
    {
        public InputDataPort Input;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var input = ctx.ReadVector2(Input);
            ctx.Write(Output, new Vector2(Mathf.RoundToInt(input.x), Mathf.RoundToInt(input.y)));
        }
    }

    [NodeDescription(typeof(Vector2Round), SpecializationOf = Vector2Round.Rounding.Ceiling)]
    public struct Vector2RoundCeiling : IDataNode
    {
        public InputDataPort Input;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var input = ctx.ReadVector2(Input);
            ctx.Write(Output, new Vector2(Mathf.CeilToInt(input.x), Mathf.CeilToInt(input.y)));
        }
    }

    [NodeDescription(typeof(Vector3Round), SpecializationOf = Vector3Round.Rounding.Floor)]
    public struct Vector3RoundFloor : IDataNode
    {
        public InputDataPort Input;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var input = ctx.ReadVector3(Input);
            ctx.Write(Output, new Vector3(Mathf.FloorToInt(input.x), Mathf.FloorToInt(input.y), Mathf.FloorToInt(input.z)));
        }
    }

    [NodeDescription(typeof(Vector3Round), SpecializationOf = Vector3Round.Rounding.AwayFromZero)]
    public struct Vector3RoundRound : IDataNode
    {
        public InputDataPort Input;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var input = ctx.ReadVector3(Input);
            ctx.Write(Output, new Vector3(Mathf.RoundToInt(input.x), Mathf.RoundToInt(input.y), Mathf.RoundToInt(input.z)));
        }
    }

    [NodeDescription(typeof(Vector3Round), SpecializationOf = Vector3Round.Rounding.Ceiling)]
    public struct Vector3RoundCeiling : IDataNode
    {
        public InputDataPort Input;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var input = ctx.ReadVector3(Input);
            ctx.Write(Output, new Vector3(Mathf.CeilToInt(input.x), Mathf.CeilToInt(input.y), Mathf.CeilToInt(input.z)));
        }
    }

    [NodeDescription(typeof(Vector4Round), SpecializationOf = Vector4Round.Rounding.Floor)]
    public struct Vector4RoundFloor : IDataNode
    {
        public InputDataPort Input;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var input = ctx.ReadVector4(Input);
            ctx.Write(Output, new Vector4(Mathf.FloorToInt(input.x), Mathf.FloorToInt(input.y), Mathf.FloorToInt(input.z), Mathf.FloorToInt(input.w)));
        }
    }

    [NodeDescription(typeof(Vector4Round), SpecializationOf = Vector4Round.Rounding.AwayFromZero)]
    public struct Vector4RoundRound : IDataNode
    {
        public InputDataPort Input;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var input = ctx.ReadVector4(Input);
            ctx.Write(Output, new Vector4(Mathf.RoundToInt(input.x), Mathf.RoundToInt(input.y), Mathf.RoundToInt(input.z), Mathf.RoundToInt(input.w)));
        }
    }

    [NodeDescription(typeof(Vector4Round), SpecializationOf = Vector4Round.Rounding.Ceiling)]
    public struct Vector4RoundCeiling : IDataNode
    {
        public InputDataPort Input;
        public OutputDataPort Output;

        public void Execute<TCtx>(TCtx ctx) where TCtx : IGraphInstance
        {
            var input = ctx.ReadVector4(Input);
            ctx.Write(Output, new Vector4(Mathf.CeilToInt(input.x), Mathf.CeilToInt(input.y), Mathf.CeilToInt(input.z), Mathf.CeilToInt(input.w)));
        }
    }
}
