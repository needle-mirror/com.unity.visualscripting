using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Unity.VisualScripting.Dependencies.NCalc;
using Unity.VisualScripting.Interpreter;
using UnityEngine.Assertions;
using NCalc = Unity.VisualScripting.Dependencies.NCalc.Expression;

namespace Unity.VisualScripting
{
    [UsedImplicitly]
    internal class FormulaTranslator : NodeTranslator<Formula>
    {
        protected override INode Translate(GraphBuilder builder, Formula unit, PortMapper mapping)
        {
            Assert.IsFalse(unit.cacheArguments, "Argument caching is not supported yet.");
            var parsedExpression = NCalc.Compile(unit.formula, true);
            PassthroughNode passthroughNode = default;

            // special case: formula is juste one id (eg. "a")
            if (parsedExpression is IdentifierExpression identifierExpression)
            {
                if (Visitor.IsFormulaInput(identifierExpression, out var idIndex, unit))
                {
                    passthroughNode = new PassthroughNode
                    { Input = { DataCount = 1 }, Output = { DataCount = 1 } };
                    mapping.AddMultiPortIndexed(builder, _ => unit.valueInputs[idIndex], ref passthroughNode.Input);
                    mapping.AddMultiPortIndexed(builder, _ => unit.result, ref passthroughNode.Output);
                    builder.AddNodeFromModel(unit, passthroughNode, mapping);
                    return passthroughNode;
                }
            }

            if (unit.inputCount > 0)
            {
                passthroughNode = new PassthroughNode
                { Input = { DataCount = unit.inputCount }, Output = { DataCount = unit.inputCount } };

                mapping.AddMultiPortIndexed(builder, i => unit.valueInputs[i], ref passthroughNode.Input);
                mapping.AddMultiPort(builder, null, ref passthroughNode.Output);
                builder.AddNodeFromModel(unit, passthroughNode, mapping);
            }

            OutputDataPort[] varsToPort =
                unit.valueInputs.Select((input, i) => passthroughNode.Output.SelectPort((uint)i)).ToArray();

            var visitor = new Visitor(builder, unit, varsToPort);
            parsedExpression.Accept(visitor);

            return unit.inputCount > 0 ? passthroughNode : visitor.Root.node;
        }

        internal class Visitor : LogicalExpressionVisitor
        {
            internal struct NodePort
            {
                public INode node;
                public IOutputDataPort port;

                public NodePort(INode node, IOutputDataPort port)
                {
                    this.node = node;
                    this.port = port;
                }
            }
            private GraphBuilder _builder;
            private Formula _unit;
            private readonly OutputDataPort[] _varsToPort;
            private bool _isRoot;

            internal NodePort Root;

            public Visitor(GraphBuilder builder, Formula unit, OutputDataPort[] varsToPort)
            {
                _builder = builder;
                _unit = unit;
                _varsToPort = varsToPort;
                _isRoot = true;
            }

            private void UpdateState(out bool wasRoot)
            {
                wasRoot = _isRoot;
                _isRoot = false;
            }

            public override void Visit(TernaryExpression ternary)
            {
                UpdateState(out var wasRoot);
                ternary.LeftExpression.Accept(this);
                var cond = Root;
                ternary.MiddleExpression.Accept(this);
                var ifTrue = Root;
                ternary.RightExpression.Accept(this);
                var ifFalse = Root;

                var n = new SelectNode();
                var portMapper = new PortMapper();
                var output = FlowGraphTranslator.GetNodeOutputPorts(n).Single();
                n = (SelectNode)_builder.AutoAssignPortIndicesAndMapPorts(n, portMapper, wasRoot ? new Dictionary<IUnitPort, FieldInfo> { [_unit.result] = output.Item1 } : null);
                _builder.AddNodeFromModel(_unit, n, portMapper);

                _builder.CreateEdge(cond.port, n.Condition);
                _builder.CreateEdge(ifTrue.port, n.IfTrue);
                _builder.CreateEdge(ifFalse.port, n.IfFalse);

                Root = new NodePort(n, n.Selection);
            }

            public override void Visit(BinaryExpression binary)
            {
                UpdateState(out var wasRoot);
                binary.LeftExpression.Accept(this);
                var left = Root;
                binary.RightExpression.Accept(this);
                var right = Root;

                switch (binary.Type)
                {
                    case BinaryExpressionType.And:
                        CreateOp(new AndNode(), n => n.Result, n => n.A, n => n.B);
                        break;
                    case BinaryExpressionType.Or:
                        CreateOp(new OrNode(), n => n.Result, n => n.A, n => n.B);
                        break;
                    case BinaryExpressionType.NotEqual:
                        CreateOp(new NotEqualNode(), n => n.Comparison, n => n.A, n => n.B);
                        break;
                    case BinaryExpressionType.LesserOrEqual:
                        CreateOp(new LessOrEqualNode(), n => n.Comparison, n => n.A, n => n.B);
                        break;
                    case BinaryExpressionType.GreaterOrEqual:
                        CreateOp(new GreaterOrEqualNode(), n => n.Comparison, n => n.A, n => n.B);
                        break;
                    case BinaryExpressionType.Lesser:
                        CreateOp(new LessNode(), n => n.Comparison, n => n.A, n => n.B);
                        break;
                    case BinaryExpressionType.Greater:
                        CreateOp(new GreaterNode(), n => n.Comparison, n => n.A, n => n.B);

                        break;
                    case BinaryExpressionType.Equal:
                        CreateOp(new EqualNode(), n => n.Comparison, n => n.A, n => n.B);
                        break;
                    case BinaryExpressionType.Minus:
                        CreateOp(new GenericSubtractNode(), n => n.Difference, n => n.Minuend, n => n.Subtrahend);
                        break;
                    case BinaryExpressionType.Plus:
                        CreateOp(new GenericSumNode { Inputs = { DataCount = 2 } }, n => n.Sum, n => n.Inputs.SelectPort(0), n => n.Inputs.SelectPort(1));
                        break;
                    case BinaryExpressionType.Modulo:
                        CreateOp(new GenericModuloNode(), n => n.Remainder, n => n.Dividend, n => n.Divisor);
                        break;
                    case BinaryExpressionType.Div:
                        CreateOp(new GenericDivideNode(), n => n.Quotient, n => n.Dividend, n => n.Divisor);
                        break;
                    case BinaryExpressionType.Times:
                        CreateOp(new GenericMultiplyNode(), n => n.Product, n => n.A, n => n.B);
                        break;
                    // case BinaryExpressionType.BitwiseOr:
                    //     break;
                    // case BinaryExpressionType.BitwiseAnd:
                    //     break;
                    // case BinaryExpressionType.BitwiseXOr:
                    //     break;
                    // case BinaryExpressionType.LeftShift:
                    //     break;
                    // case BinaryExpressionType.RightShift:
                    //     break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                T CreateOp<T>(T n, Func<T, OutputDataPort> outputPort, Func<T, InputDataPort> leftInput, Func<T, InputDataPort> rightInput) where T : struct, INode
                {
                    var portMapper = new PortMapper();
                    var output = FlowGraphTranslator.GetNodeOutputPorts(n).Single();
                    var portModelToRuntimeField = wasRoot ? new Dictionary<IUnitPort, FieldInfo> { [_unit.result] = output.Item1 } : null;
                    n = (T)_builder.AutoAssignPortIndicesAndMapPorts(n, portMapper, portModelToRuntimeField);
                    _builder.CreateEdge(left.port, leftInput(n));
                    _builder.CreateEdge(right.port, rightInput(n));
                    _builder.AddNodeFromModel(_unit, n, portMapper);
                    Root = new NodePort(n, outputPort(n));
                    return n;
                }
            }

            public override void Visit(UnaryExpression unary)
            {
                UpdateState(out var wasRoot);
                unary.Expression.Accept(this);
                var left = Root;
                switch (unary.Type)
                {
                    case UnaryExpressionType.Not:
                        CreateOp(new NegateNode(), n => n.Output, n => n.Input);
                        break;
                    case UnaryExpressionType.Negate:
                        CreateOp(new NegateNumericNode(), n => n.Output, n => n.Input);
                        break;
                    case UnaryExpressionType.BitwiseNot:
                        CreateOp(new NegateBitwiseNode(), n => n.Output, n => n.Input);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                T CreateOp<T>(T n, Func<T, OutputDataPort> outputPort, Func<T, InputDataPort> leftInput) where T : struct, INode
                {
                    var portMapper = new PortMapper();
                    var output = FlowGraphTranslator.GetNodeOutputPorts(n).Single();
                    var portModelToRuntimeField = wasRoot ? new Dictionary<IUnitPort, FieldInfo> { [_unit.result] = output.Item1 } : null;
                    n = (T)_builder.AutoAssignPortIndicesAndMapPorts(n, portMapper, portModelToRuntimeField);
                    _builder.CreateEdge(left.port, leftInput(n));
                    _builder.AddNodeFromModel(_unit, n, portMapper);
                    Root = new NodePort(n, outputPort(n));
                    return n;
                }
            }

            public override void Visit(ValueExpression value)
            {
                UpdateState(out var wasRoot);
                FlowGraphTranslator.TranslateConstant(_builder, out var constNode, out var mapping,
                    value.Value.GetType(), value.Value, wasRoot ? _unit.result : null, out var valuePort);

                Root = new NodePort(constNode, valuePort);
                _builder.AddNodeFromModel(_unit, constNode, mapping);
            }

            public override void Visit(FunctionExpression function)
            {
                UpdateState(out var wasRoot);
                INode n;
                int inputCount = 0;
                switch (function.Identifier.Name.ToLowerInvariant())
                {
                    case "v2":
                        n = new UnityEngineVector2CtorXY();
                        inputCount = 2;
                        break;
                    case "v3":
                        n = new UnityEngineVector3CtorXYZ();
                        inputCount = 3;
                        break;
                    case "v4":
                        n = new UnityEngineVector4CtorXYZW();
                        inputCount = 4;
                        break;
                    default:
                        throw new InvalidDataException("Unknown function: " + function.Identifier.Name);
                }

                var portMapper = new PortMapper();
                var output = FlowGraphTranslator.GetNodeOutputPorts(n).Single();
                var portModelToRuntimeField = wasRoot ? new Dictionary<IUnitPort, FieldInfo> { [_unit.result] = output.Item1 } : null;
                n = _builder.AutoAssignPortIndicesAndMapPorts(n, portMapper, portModelToRuntimeField);

                _builder.AddNodeFromModel(_unit, n, portMapper);

                Assert.AreEqual(inputCount, function.Expressions.Length);

                var inputs = FlowGraphTranslator.GetNodeInputPorts(n).ToArray();
                for (var index = 0; index < function.Expressions.Length; index++)
                {
                    var functionExpression = function.Expressions[index];
                    functionExpression.Accept(this);
                    _builder.CreateEdge(Root.port, inputs[index].Item2);
                }
                Root = new NodePort(n, (IOutputDataPort)output.Item1.GetValue(n));
            }

            public override void Visit(IdentifierExpression identifier)
            {
                UpdateState(out var wasRoot);

                var name = identifier.Name;

                if (IsFormulaInput(identifier, out int idIndex, _unit))
                {
                    var varOutputPort = _varsToPort[idIndex];
                    Root = new NodePort(null, varOutputPort);
                    return;
                }


                if (name.Contains('.'))
                {
                    throw new InvalidOperationException(
                        $"Accessor are not implemented yet, in expression: '{name}'");
                }
                throw new InvalidOperationException(
                    $"Unknown name in expression: '{name}'");
            }

            internal static bool IsFormulaInput(IdentifierExpression identifier, out int idIndex, Formula unit)
            {
                if (identifier.Name.Length == 1) // formula input
                {
                    idIndex = char.ToLowerInvariant(identifier.Name[0]) - 'a';
                    if (idIndex < unit.inputCount)
                    {
                        return true;
                    }
                }

                idIndex = -1;
                return false;
            }
        }
    }
}
