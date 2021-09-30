using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting.Interpreter;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Port = UnityEditor.Experimental.GraphView.Port;

namespace Unity.VisualScripting
{
    [Serializable]
    internal class RuntimeGraphViewer : EditorWindow
    {
        class RGGraphView : GraphView
        {
            private readonly RuntimeGraphViewer _viewer;

            public RGGraphView(RuntimeGraphViewer viewer)
            {
                _viewer = viewer;
                SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
                focusable = true;
                this.AddManipulator(new ContentDragger());
                var m_SelectionDragger = new SelectionDragger();
                this.AddManipulator(m_SelectionDragger);
                this.AddManipulator(new RectangleSelector());
                this.AddManipulator(new FreehandSelector());
                Insert(0, new GridBackground());
            }

            internal void ClearGraph()
            {
                List<GraphElement> elements = graphElements.ToList();
                foreach (var element in elements)
                {
                    RemoveElement(element);
                }
            }

            public override void AddToSelection(ISelectable selectable)
            {
                base.AddToSelection(selectable);
                _viewer.RuntimeSelectionChanged(selection);
            }

            public override void RemoveFromSelection(ISelectable selectable)
            {
                base.RemoveFromSelection(selectable);
                _viewer.RuntimeSelectionChanged(selection);
            }

            public override void ClearSelection()
            {
                base.ClearSelection();
                _viewer.RuntimeSelectionChanged(selection);
            }

            public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
            {
                return ports.ToList().Where(nap =>
                        nap.direction != startPort.direction &&
                        nap.node != startPort.node)
                    .ToList();
            }
        }

        class Node : UnityEditor.Experimental.GraphView.Node
        {
            public override Rect GetPosition() => new Rect(style.left.value.value, style.top.value.value, layout.width, layout.height);

            internal void AddAnnotations(List<RuntimeGraphDebugData.NodeAnnotation> annotations)
            {
                foreach (var nodeAnnotation in annotations)
                {
                    var label = new Label(nodeAnnotation.Label);
                    label.style.backgroundColor = nodeAnnotation.Color;
                    label.style.color = Contrast(nodeAnnotation.Color);
                    this.Q("contents").Add(label);
                }

            }
        }


        private RGGraphView m_GraphView;
        private Dictionary<uint, Port> _portsUi;
        private Dictionary<NodeId, Node> _nodesUi;
        private HashSet<(NodeId fromNodeIndex, NodeId toNodeIndex)> _edgesUi;
        private RuntimeGraphAsset _loadedRuntimeGraphAsset;
        private static readonly Type _triggertype = typeof(Vector3);
        private static readonly Type _datatype = typeof(float);
        private ulong _loadedHash;
        private GraphSelection _currentSelection;
        private IGraph _currentAuthoringGraph;
        [DoNotSerialize]
        private bool _selectionEventGuard;

        private const string Key = nameof(RuntimeGraphViewer) + "_ShowAnnotations";

        private static bool ShowAnnotations
        {
            get => EditorPrefs.GetBool(Key);
            set => EditorPrefs.SetBool(Key, value);
        }

        [MenuItem("internal:Visual Scripting/Show runtime graph vizualizer")]
        public static void Open()
        {
            GetWindow<RuntimeGraphViewer>().Show();
        }

        private void OnEnable()
        {
            var rgGraphView = new RGGraphView(this);

            rootVisualElement.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.unity.visualscripting/Editor/VisualScripting.Flow/Interpreter/RuntimeGraphViewer.uss"));

            rgGraphView.AddManipulator(new ShortcutHandler(new Dictionary<Event, ShortcutDelegate>
            {
                {Event.KeyboardEvent("F2"), SelectAsset},
                {Event.KeyboardEvent("F4"), RecLayout},
            }));
            m_GraphView = rgGraphView;
            m_GraphView.StretchToParentSize();
            rootVisualElement.Add(m_GraphView);

            var toolbar = new Toolbar();
            rootVisualElement.Add(toolbar);

            var toggleAnnotations = new ToolbarToggle() { text = "Show Debug Annotations" };
            toggleAnnotations.SetValueWithoutNotify(ShowAnnotations);
            toggleAnnotations.RegisterValueChangedCallback(e =>
            {
                ShowAnnotations = e.newValue;
                OnSelectionChange();
            });
            toolbar.Add(toggleAnnotations);

            toolbar.Add(new ToolbarButton(() => SelectAsset()) { text = "Select Asset (F2)" });
            toolbar.Add(new ToolbarButton(() => RecLayout()) { text = "Layout (F4)" });

            Selection.selectionChanged += OnSelectionChange;
            EditorApplication.delayCall += OnSelectionChange;
        }

        private void RuntimeSelectionChanged(List<ISelectable> selection)
        {
            if (_currentSelection == null)
                return;
            if (_selectionEventGuard)
                return;
            _selectionEventGuard = true;
            _currentSelection.Clear();
            foreach (var selectable in selection)
            {
                if (selectable is Node n)
                {
                    var nodeId = (NodeId)n.userData;
                    if (_currentAuthoringGraph.elements.TryGetValue(_loadedRuntimeGraphAsset.DebugData.GetNodeGuid(nodeId), out var graphElement))
                        _currentSelection.Add(graphElement);
                }
            }
            _selectionEventGuard = false;
        }

        private EventPropagation SelectAsset()
        {
            Selection.activeObject = _loadedRuntimeGraphAsset;
            return EventPropagation.Stop;
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChange;
        }

        private void OnSelectionChange()
        {
            if (_currentSelection != null)
                _currentSelection.changed -= AuthoringNodeSelectionChanged;
            _currentAuthoringGraph = null;

            var w = GetWindow<GraphWindow>();

            if (Selection.activeObject is RuntimeGraphAsset rga)
                CreateGraph(rga);
            else if (Selection.activeObject is ScriptGraphAsset flowGraphAsset)
                CreateGraph(flowGraphAsset.graph?.RuntimeGraphAsset);
            else if (Selection.activeGameObject?.GetComponent<ScriptMachine>() is ScriptMachine flowMachine)
                CreateGraph(flowMachine.graph?.RuntimeGraphAsset);
            else if (w && w.context?.graph is FlowGraph flowGraph)
                CreateGraph(flowGraph.RuntimeGraphAsset);
            if (w && w.context?.selection != null)
            {
                _currentSelection = w.context.selection;
                _currentSelection.changed += AuthoringNodeSelectionChanged;
                _currentAuthoringGraph = w.context.graph;
            }
        }


        private void AuthoringNodeSelectionChanged()
        {
            if (_currentSelection == null)
                return;
            if (_selectionEventGuard)
                return;

            _selectionEventGuard = true;
            var allIds = _currentSelection.SelectMany(x => _loadedRuntimeGraphAsset.DebugData.GetNodeIds(x.guid)).ToHashSetPooled();
            m_GraphView.nodes.ForEach(n => n.EnableInClassList("highlighted-node", false));
            foreach (var nodeId in allIds)
            {
                if (_nodesUi.TryGetValue(nodeId, out var uiNode))
                {
                    uiNode.EnableInClassList("highlighted-node", true);
                }
            }
            allIds.Free();
            _selectionEventGuard = false;
        }

        private void Update()
        {
            if (_loadedRuntimeGraphAsset != null && _loadedRuntimeGraphAsset.Hash != _loadedHash)
                CreateGraph(_loadedRuntimeGraphAsset);
        }

        static Color Contrast(Color32 color, int threshold = 128)
        {
            var rgbToYIQ = (color.r * 299 + color.g * 587 + color.b * 114) / 1000;
            return rgbToYIQ >= threshold ? Color.black : Color.white;
        }

        private void CreateGraph(RuntimeGraphAsset rga)
        {
            _selectionEventGuard = false;
            m_GraphView.ClearGraph();
            _loadedRuntimeGraphAsset = rga;
            _portsUi = new Dictionary<uint, Port>();
            _nodesUi = new Dictionary<NodeId, Node>();
            _edgesUi = new HashSet<(NodeId fromNodeIndex, NodeId toNodeIndex)>();

            var def = rga?.GraphDefinition;
            _loadedHash = rga?.Hash ?? 0;
            if (def == null)
            {
                return;
            }
            for (var i = 0; i < def.NodeTable.Length; i++)
            {
                var node = def.NodeTable[i];
                NodeId id = new NodeId((uint)i);
                var title = (node?.ToString() ?? "<??>");
                const string reflectedSuffix = " (REFLECTED)";
                if (node is IConstantNode)
                    title += " " + node.GetType().GetField("Value")?.GetValue(node);
                else if (node is InvokeMemberReflectionNode invokeMemberReflectionNode)
                    title = def.ReflectedMembers[(int)invokeMemberReflectionNode.ReflectedMemberIndex] + reflectedSuffix;
                else if (node is GetMemberReflectionNode getMemberReflectionNode)
                    title = def.ReflectedMembers[(int)getMemberReflectionNode.ReflectedMemberIndex] + reflectedSuffix;
                else if (node is SetMemberReflectionNode setMemberReflectionNode)
                    title = def.ReflectedMembers[(int)setMemberReflectionNode.ReflectedMemberIndex] + reflectedSuffix;

                var uinode = new Node() { title = $"{id}: {title}", userData = id };
                if (ShowAnnotations && rga.DebugData != null && rga.DebugData.Annotations.TryGetAnnotations(id, out var annotations))
                    uinode.AddAnnotations(annotations);
                var styleBackgroundColor = GetNodeColor(node);
                uinode.titleContainer.style.backgroundColor = styleBackgroundColor;
                uinode.titleContainer.Q<Label>().style.color = Contrast(styleBackgroundColor);
                _nodesUi.Add(id, uinode);
                if (node != null)
                {
                    foreach (var fieldInfo in FlowGraphTranslator.GetNodePorts(node.GetType()))
                    {
                        var port = fieldInfo.GetValue(node) as IPort;

                        for (uint j = 0; j < port.GetDataCount(); j++)
                        {
                            bool isInput = port is IInputPort;

                            var uiPort = uinode.InstantiatePort(Orientation.Horizontal,
                                isInput ? Direction.Input : Direction.Output,
                                Port.Capacity.Multi, port.IsData() ? _datatype : _triggertype);
                            uiPort.portName = fieldInfo.Name;
                            var index = port.GetPort().Index + j;
                            string dataIndex = $"/{def.PortInfoTable[index].DataOrTriggerIndex}";
                            if (isInput)
                                uiPort.portName = $"({index}{dataIndex}) {uiPort.portName}";
                            else
                                uiPort.portName = $"{uiPort.portName} ({index}{dataIndex})";
                            (isInput ? uinode.inputContainer : uinode.outputContainer).Add(uiPort);
                            uiPort.userData = fieldInfo;
                            if (index != 0)
                                _portsUi.Add(index, uiPort);
                        }
                    }
                }

                m_GraphView.AddElement(uinode);
                uinode.SetPosition(new Rect((i / 5) * 200 + 10, (i % 5) * 100 + 10, 0, 0));
            }

            var dataIndexLookup = def.PortInfoTable
                .Select((info, i) => (info, i))
                .Skip(1)
                .Where(p => p.info.IsDataPort && !p.info.IsOutputPort)
                .ToLookup(p => p.info.DataOrTriggerIndex);
            for (var portIndex = 1; portIndex < def.PortInfoTable.Length; portIndex++)
            {
                var portInfo = def.PortInfoTable[portIndex];
                if (!portInfo.IsDataPort && portInfo.IsOutputPort)
                {
                    // Assert.IsTrue(portIndex < m_Definition.PortInfoTable.Count);
                    uint triggerIndex = def.PortInfoTable[portIndex].DataOrTriggerIndex;
                    CreateEdge((uint)portIndex, triggerIndex, portInfo.NodeId, def.PortInfoTable[(int)triggerIndex].NodeId);
                }
                else if (portInfo.IsDataPort && portInfo.IsOutputPort)
                {
                    var dataIndex = def.PortInfoTable[portIndex].DataOrTriggerIndex;
                    if (dataIndex == 0)
                        continue;
                    foreach (var connected in dataIndexLookup[dataIndex])
                    {
                        CreateEdge((uint)portIndex, (uint)connected.i, portInfo.NodeId, connected.info.NodeId);
                    }
                }
            }

            rootVisualElement.schedule.Execute(() =>
            {
                RecLayout();
                rootVisualElement.schedule.Execute(() => m_GraphView.FrameAll()).StartingIn(1);
            }).StartingIn(100);
        }

        private static Color GetNodeColor(INode node)
        {
            Color Parse(string s) => UnityEngine.ColorUtility.TryParseHtmlString("#" + s, out var c) ? c : Color.clear;
            switch (node)
            {
                case GetMemberReflectionNode _:
                case SetMemberReflectionNode _:
                case InvokeMemberReflectionNode _:
                    return Parse("F25F5C");
                case IConstantNode _:
                    return Parse("009FB7");
                case IDataNode _:
                    return Parse("70C1B3");
                case IFlowNode _:
                    return Parse("FFE066");
                case IEntryPointNode _:
                    return Parse("60A561");
            }
            return Color.clear;
        }

        private void CreateEdge(uint fromPortIndex, uint toPortIndex, NodeId fromNodeIndex, NodeId toNodeIndex)
        {
            if (!_portsUi.TryGetValue(fromPortIndex, out var fromPort))
                // Debug.Log($"Unknown port {fromPortIndex}");
                return;
            if (!_portsUi.TryGetValue(toPortIndex, out var toPort))
                // Debug.Log($"Unknown port {toPortIndex}");
                return;
            var edge = fromPort.ConnectTo(toPort);
            _edgesUi.Add((fromNodeIndex, toNodeIndex));
            m_GraphView.AddElement(edge);
        }

        private EventPropagation RecLayout()
        {
            if (!_loadedRuntimeGraphAsset || _loadedRuntimeGraphAsset.GraphDefinition.NodeTable == null)
                return EventPropagation.Stop;
            const float fx = 400;
            const float fy = 100;

            HashSet<NodeId> visited = new HashSet<NodeId>();
            Dictionary<Vector2Int, Node> grid = new Dictionary<Vector2Int, Node>();
            var v = Vector2Int.zero;
            for (var index = 0; index < _loadedRuntimeGraphAsset.GraphDefinition.NodeTable.Length; index++)
            {
                var id = new NodeId((uint)index);
                var node = _loadedRuntimeGraphAsset.GraphDefinition.NodeTable[index];
                if (node is IEntryPointNode entryPointNode)
                {
                    if (ToGrid(ref v, id))
                        Rec(_nodesUi[id], v);
                    v.y = grid.Any() ? grid.Max(x => x.Key.y) + 1 : 0;
                }
            }

            // var d = grid.GroupBy(x => x.Key.x).ToDictionary(x => x.Key, x => x.Max(n => n.Value.layout.width));

            foreach (var item in grid)
            {
                // can be null when a node is too big and occupies multiple cells
                item.Value?.SetPosition(new Rect(item.Key.x * fx, item.Key.y * fy, 0, 0));
            }

            return EventPropagation.Stop;

            bool ToGrid(ref Vector2Int candidate, NodeId id)
            {
                if (!visited.Add(id))
                    return false;

                var uiNode = _nodesUi[id];
                var cellCountY = 1 + ((int)uiNode.layout.height + 1) / (int)fy;
                // Debug.Log(cellCountY);

                bool found = false;
                while (!found)
                {
                    while (grid.ContainsKey(candidate))
                        candidate += Vector2Int.up;
                    found = true;
                    for (int i = 1; i < cellCountY && found; i++)
                    {
                        if (grid.ContainsKey(candidate + i * Vector2Int.up))
                        {
                            found = false;
                            candidate += i * Vector2Int.up;
                            break;
                        }
                    }
                }

                for (int i = 0; i < cellCountY; i++)
                {
                    grid.Add(candidate + i * Vector2Int.up, i == 0 ? uiNode : null);
                }
                return true;
            }

            void Rec(Node uiNode, Vector2Int p)
            {
                Vector2Int vv;
                vv = p + Vector2Int.left;
                uiNode.inputContainer.Query<Port>().ForEach(port =>
                {
                    if (!port.connected)
                        return;
                    if (port.portType != _triggertype)
                    {
                        // should be Single() but ATM the viewer connects all get/set variable nodes as they use the same data index for the variable storage
                        var target = port.connections.First().output.node;
                        if (ToGrid(ref vv, (NodeId)target.userData))
                            Rec((Node)target, vv);
                    }
                });

                vv = p + Vector2Int.right * 2;
                uiNode.outputContainer.Query<Port>().ForEach(port =>
                {
                    if (!port.connected)
                        return;
                    if (port.portType == _triggertype)
                    {
                        var target = port.connections.Single().input.node;
                        if (ToGrid(ref vv, (NodeId)target.userData))
                            Rec((Node)target, vv);
                    }
                });
            }
        }
    }
}
