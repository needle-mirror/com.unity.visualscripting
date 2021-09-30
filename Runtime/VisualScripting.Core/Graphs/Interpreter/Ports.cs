using System;
using UnityEngine.Assertions;

namespace Unity.VisualScripting.Interpreter
{
    public static class PortExtensions
    {
        public static Port SelectRawPort(this IPort port, uint index)
        {
            Assert.IsTrue(index < port.GetDataCount());
            return new Port { Index = port.GetPort().Index + index };
        }

        public static string SelectRawPortName(this IPort port, string portModelUniqueId, uint index)
        {
            Assert.IsTrue(index < port.GetDataCount());
            return port is IMultiPort ? $"{portModelUniqueId}_{index}" : portModelUniqueId;
        }

        public static PortDirection GetDirection(this IPort port) => (port is IOutputDataPort || port is IOutputTriggerPort) ? PortDirection.Output : PortDirection.Input;
        public static PortType GetPortType(this IPort port) => port.IsData() ? PortType.Data : PortType.Trigger;
        public static bool IsData(this IPort port) => port is IDataPort;
        public static bool IsTrigger(this IPort port) => port is ITriggerPort;
        public static uint GetLastIndex(this IPort port) => port is IMultiPort multiPort ? port.GetPort().Index + (uint)multiPort.GetDataCount() : port.GetPort().Index;

        public static uint GetSubPortIndex(this IMultiPort port, IPort subPort)
        {
            Assert.IsTrue(subPort.GetPort().Index >= port.GetPort().Index && subPort.GetPort().Index < port.GetPort().Index + port.GetDataCount());
            return subPort.GetPort().Index - port.GetPort().Index;
        }

        public static OutputDataPort SelectOutputDataPort(this IOutputDataPort port, uint index)
        {
            if (port is OutputDataMultiPort multiPort)
                return multiPort.SelectPort(index);
            Assert.AreEqual(0, index);
            return (OutputDataPort)port;
        }

        public static InputDataPort SelectInputDataPort(this IInputDataPort port, uint index)
        {
            if (port is InputDataMultiPort multiPort)
                return multiPort.SelectPort(index);
            Assert.AreEqual(0, index);
            return (InputDataPort)port;
        }
    }

    public interface IPort
    {
        Port GetPort();
        void SetPort(Port p);
        int GetDataCount();
    }

    public interface IDataPort : IPort
    {
    }

    public interface ITriggerPort : IPort
    {
    }

    public interface IMultiPort : IPort
    {
        void SetCount(int count);
    }

    public enum PortDirection
    {
        Input,
        Output
    }

    public enum PortType
    {
        Data,
        Trigger
    }

    public interface IInputPort : IPort { }
    public interface IOutputPort : IPort { }
    public interface IInputDataPort : IDataPort, IInputPort { }
    public interface IOutputDataPort : IDataPort, IOutputPort { }
    public interface IInputTriggerPort : ITriggerPort, IInputPort { }
    public interface IOutputTriggerPort : ITriggerPort, IOutputPort { }

    [Serializable]
    public struct InputDataPort : IInputDataPort, IEquatable<InputDataPort>
    {
        public Port Port;
        public Port GetPort() => Port;
        public int GetDataCount() => 1;
        public void SetPort(Port p) => Port = p;
        public override string ToString() => Port.Index.ToString();

        public bool Equals(InputDataPort other) => Port.Equals(other.Port);

        public override bool Equals(object obj) => obj is InputDataPort other && Equals(other);

        public override int GetHashCode() => Port.GetHashCode();

        public static bool operator ==(InputDataPort left, InputDataPort right) => left.Equals(right);

        public static bool operator !=(InputDataPort left, InputDataPort right) => !left.Equals(right);
    }

    [Serializable]
    public struct OutputDataPort : IOutputDataPort, IEquatable<OutputDataPort>
    {
        public Port Port;
        public Port GetPort() => Port;
        public int GetDataCount() => 1;
        public void SetPort(Port p) => Port = p;
        public override string ToString() => Port.Index.ToString();

        public bool Equals(OutputDataPort other) => Port.Equals(other.Port);

        public override bool Equals(object obj) => obj is OutputDataPort other && Equals(other);

        public override int GetHashCode() => Port.GetHashCode();

        public static bool operator ==(OutputDataPort left, OutputDataPort right) => left.Equals(right);

        public static bool operator !=(OutputDataPort left, OutputDataPort right) => !left.Equals(right);
    }

    [Serializable]
    public struct InputTriggerPort : IInputTriggerPort, IEquatable<InputTriggerPort>
    {
        public Port Port;
        public Port GetPort() => Port;
        public void SetPort(Port p) => Port = p;
        public int GetDataCount() => 1;
        public override string ToString() => Port.Index.ToString();

        public bool Equals(InputTriggerPort other) => Port.Equals(other.Port);

        public override bool Equals(object obj) => obj is InputTriggerPort other && Equals(other);

        public override int GetHashCode() => Port.GetHashCode();

        public static bool operator ==(InputTriggerPort left, InputTriggerPort right) => left.Equals(right);

        public static bool operator !=(InputTriggerPort left, InputTriggerPort right) => !left.Equals(right);
    }

    [Serializable]
    public struct OutputTriggerPort : IOutputTriggerPort, IEquatable<OutputTriggerPort>
    {
        public Port Port;
        public Port GetPort() => Port;
        public void SetPort(Port p) => Port = p;
        public int GetDataCount() => 1;
        public override string ToString() => Port.Index.ToString();

        public bool Equals(OutputTriggerPort other) => Port.Equals(other.Port);

        public override bool Equals(object obj) => obj is OutputTriggerPort other && Equals(other);

        public override int GetHashCode() => Port.GetHashCode();

        public static bool operator ==(OutputTriggerPort left, OutputTriggerPort right) => left.Equals(right);

        public static bool operator !=(OutputTriggerPort left, OutputTriggerPort right) => !left.Equals(right);
    }

    [Serializable]
    public struct InputDataMultiPort : IInputDataPort, IMultiPort, IEquatable<InputDataMultiPort>
    {
        public Port Port;
        public int DataCount;
        public Port GetPort() => Port;
        public void SetPort(Port p) => Port = p;
        public int GetDataCount() => DataCount;
        public void SetCount(int count) => DataCount = count;
        public InputDataPort SelectPort(uint index)
        {
            Assert.IsTrue(index < DataCount);
            return new InputDataPort { Port = new Port { Index = Port.Index + index } };
        }

        public bool Equals(InputDataMultiPort other) => Port.Equals(other.Port) && DataCount == other.DataCount;

        public override bool Equals(object obj) => obj is InputDataMultiPort other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Port.GetHashCode() * 397) ^ DataCount;
            }
        }

        public static bool operator ==(InputDataMultiPort left, InputDataMultiPort right) => left.Equals(right);

        public static bool operator !=(InputDataMultiPort left, InputDataMultiPort right) => !left.Equals(right);
    }

    [Serializable]
    public struct OutputDataMultiPort : IOutputDataPort, IMultiPort, IEquatable<OutputDataMultiPort>
    {
        public Port Port;
        public int DataCount;
        public Port GetPort() => Port;
        public void SetPort(Port p) => Port = p;
        public int GetDataCount() => DataCount;
        public void SetCount(int count) => DataCount = count;

        public OutputDataPort SelectPort(uint index)
        {
            Assert.IsTrue(index < DataCount);
            return new OutputDataPort { Port = new Port { Index = Port.Index + index } };
        }

        public bool Equals(OutputDataMultiPort other) => Port.Equals(other.Port) && DataCount == other.DataCount;

        public override bool Equals(object obj) => obj is OutputDataMultiPort other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Port.GetHashCode() * 397) ^ DataCount;
            }
        }

        public static bool operator ==(OutputDataMultiPort left, OutputDataMultiPort right) => left.Equals(right);

        public static bool operator !=(OutputDataMultiPort left, OutputDataMultiPort right) => !left.Equals(right);
    }

    [Serializable]
    public struct InputTriggerMultiPort : IInputTriggerPort, IMultiPort, IEquatable<InputTriggerMultiPort>
    {
        public Port Port;
        public int DataCount;
        public Port GetPort() => Port;
        public void SetPort(Port p) => Port = p;
        public int GetDataCount() => DataCount;
        public void SetCount(int count) => DataCount = count;
        public InputTriggerPort SelectPort(uint index)
        {
            Assert.IsTrue(index < DataCount);
            return new InputTriggerPort { Port = new Port { Index = Port.Index + index } };
        }

        public bool Equals(InputTriggerMultiPort other) => Port.Equals(other.Port) && DataCount == other.DataCount;

        public override bool Equals(object obj) => obj is InputTriggerMultiPort other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Port.GetHashCode() * 397) ^ DataCount;
            }
        }

        public static bool operator ==(InputTriggerMultiPort left, InputTriggerMultiPort right) => left.Equals(right);

        public static bool operator !=(InputTriggerMultiPort left, InputTriggerMultiPort right) => !left.Equals(right);
    }

    [Serializable]
    public struct OutputTriggerMultiPort : IOutputTriggerPort, IMultiPort, IEquatable<OutputTriggerMultiPort>
    {
        public Port Port;
        public int DataCount;
        public Port GetPort() => Port;
        public void SetPort(Port p) => Port = p;
        public int GetDataCount() => DataCount;
        public void SetCount(int count) => DataCount = count;
        public OutputTriggerPort SelectPort(uint index)
        {
            Assert.IsTrue(index < DataCount);
            return new OutputTriggerPort { Port = new Port { Index = Port.Index + index } };
        }

        public bool Equals(OutputTriggerMultiPort other) => Port.Equals(other.Port) && DataCount == other.DataCount;

        public override bool Equals(object obj) => obj is OutputTriggerMultiPort other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                return (Port.GetHashCode() * 397) ^ DataCount;
            }
        }

        public static bool operator ==(OutputTriggerMultiPort left, OutputTriggerMultiPort right) => left.Equals(right);

        public static bool operator !=(OutputTriggerMultiPort left, OutputTriggerMultiPort right) => !left.Equals(right);
    }
}
