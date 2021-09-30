using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.Assertions;

namespace Unity.VisualScripting.Interpreter
{
    /// <summary>
    /// For initialization of runtime ports (they need to have a graph-wide unique index) and optionally mapping them to an authoring unit port
    /// The mapping enables debugging to display the recorded values on the right authoring ports and highlight the right authoring nodes
    /// Given that node translation is not 1:1, this is required:
    /// - 1 authoring node could get translated to N runtime nodes
    /// - 1 port might be translated to 0 or 1 ports
    /// </summary>
    public class PortMapper
    {
        public struct MappedPort
        {
            public IPort Port;
            public PortType Type;
            public uint PortIndex;
            public readonly Value? DefaultValue;
            public readonly string PortName;

            public MappedPort(IPort port, PortType type, uint portIndex, Value? defaultValue, string portName)
            {
                Port = port;
                Type = type;
                PortIndex = portIndex;
                DefaultValue = defaultValue;
                PortName = portName;
            }
        }
        /// <summary>
        /// ports with a matching port model
        /// </summary>
        private Dictionary<IUnitPort, MappedPort> m_MappedPortModels;
        /// <summary>
        /// internal/hidden ports with no port model
        /// </summary>
        private List<MappedPort> m_MappedPorts;

        private HashSet<uint> m_UsedPortIndices;

        private void InitPort<TPort>(GraphBuilder graphBuilder, ref TPort port)
            where TPort : IPort
        {
            Assert.AreEqual(0u, port.GetPort().Index);
            graphBuilder.AssignPortIndex(ref port);

            NUnit.Framework.Assert.AreNotEqual(0, port.GetPort().Index,
                $"Port has an invalid index. Call graphBuilder.SetupPort() to assign a valid index");

            if (m_MappedPortModels == null)
            {
                m_MappedPortModels = new Dictionary<IUnitPort, MappedPort>();
                m_MappedPorts = new List<MappedPort>();
                m_UsedPortIndices = new HashSet<uint>();
            }
        }

        public void AddMultiPort<TMultiPort>(GraphBuilder graphBuilder, List<IUnitPort> portModels, ref TMultiPort port) where TMultiPort : IMultiPort
        {
            if (portModels != null)
                Assert.AreEqual(port.GetDataCount(), portModels.Count);
            AddMultiPortIndexed(graphBuilder, i => portModels?[i], ref port);
        }

        public void AddMultiPortIndexed<TMultiPort>(GraphBuilder graphBuilder, Func<int, IUnitPort> portModels, ref TMultiPort port,
            Func<uint, Value?> defaultValue = null) where TMultiPort : IMultiPort
        {
            if (port.GetDataCount() == 0)
                return;
            InitPort(graphBuilder, ref port);

            for (uint i = 0; i < port.GetDataCount(); i++)
            {
                var selectRawPort = port.SelectRawPort(i);
                // Debug.Log($"Add port {selectRawPort.Index} {portModelUniqueId}");

                var portModel = portModels((int)i);

                var mappedPort = new MappedPort(port, port.GetPortType(), selectRawPort.Index, defaultValue?.Invoke(i),
                    portModel?.key ?? port.ToString());

                Assert.IsTrue(m_UsedPortIndices.Add(selectRawPort.Index), $"Port {mappedPort.PortName} registered with an id already in use: {selectRawPort.Index}");

                if (portModel != null)
                    m_MappedPortModels.Add(portModel, mappedPort);
                else
                    m_MappedPorts.Add(mappedPort);
            }
        }

        public void AddSinglePort<TPort>(GraphBuilder graphBuilder, IUnitPort portModel, ref TPort port, Value? defaultValue = null) where TPort : IPort
        {
            InitPort(graphBuilder, ref port);

            Assert.IsFalse(port is IMultiPort, $"MultiPorts must be added with {nameof(AddMultiPort)}");

            var mappedPort = new MappedPort(port, port.GetPortType(), port.GetPort().Index, defaultValue,
                portModel?.key);
            if (portModel != null)
                m_MappedPortModels.Add(portModel, mappedPort);
            else
                m_MappedPorts.Add(mappedPort);
        }

        public bool TryGetPortIndexOfPortModel(IUnitPort portModel, out IPort port)
        {
            port = default;
            if (m_MappedPortModels == null || !m_MappedPortModels.TryGetValue(portModel, out MappedPort res))
                return false;

            // if we have a multiport with index 3 and count 2:
            // - there's 2 MappedPorts in the mapping: one with portIndex 3, port.Index 3, and one with portIndex 4, port.Index 3
            // - the "multiPortIndices" will be 0 and 1 respectively
            var multiPortIndex = res.PortIndex - res.Port.GetPort().Index;
            switch (res.Port)
            {
                case InputDataMultiPort idm: port = idm.SelectPort(multiPortIndex); return true;
                case OutputDataMultiPort idm: port = idm.SelectPort(multiPortIndex); return true;
                case InputTriggerMultiPort idm: port = idm.SelectPort(multiPortIndex); return true;
                case OutputTriggerMultiPort idm: port = idm.SelectPort(multiPortIndex); return true;
                default: port = res.Port; return true;
            }
        }

        public IPort GetPortIndexOfPortModel(IUnitPort portModel)
        {
            if (!TryGetPortIndexOfPortModel(portModel, out var port))
                throw new InvalidDataException($"Unknown port '{portModel.key}' of unit {portModel.unit}");
            return port;
        }

        /// <summary>
        /// used to fill the <see cref="GraphDefinition.PortInfoTable"/>
        /// </summary>
        public IEnumerable<KeyValuePair<IUnitPort, MappedPort>> AllPorts =>
            (m_MappedPortModels ?? Enumerable.Empty<KeyValuePair<IUnitPort, MappedPort>>()).Concat(
                m_MappedPorts?.Select(x => new KeyValuePair<IUnitPort, MappedPort>(null, x)) ??
                Enumerable.Empty<KeyValuePair<IUnitPort, MappedPort>>())
                .OrderBy(x => x.Value.PortIndex);

        public Dictionary<IUnitPort, MappedPort> MappedPortModels => m_MappedPortModels;

        public void Merge(PortMapper portToOffsetMapping)
        {
            m_MappedPorts.AddRange(portToOffsetMapping.m_MappedPorts);
            m_MappedPortModels.AddRange(portToOffsetMapping.m_MappedPortModels);
            var count = m_UsedPortIndices.Count;
            m_UsedPortIndices.AddRange(portToOffsetMapping.m_UsedPortIndices);
            Assert.AreEqual(count + portToOffsetMapping.m_UsedPortIndices.Count, m_UsedPortIndices.Count);
        }
    }
}
