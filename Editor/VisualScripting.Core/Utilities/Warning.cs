using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.VisualScripting
{
    public sealed class Warning
    {
        internal Warning(WarningLevel level, string message, string buttonTitle, Action onClick) : this(level, message)
        {
            _buttonTitle = buttonTitle;
            this._onClick = onClick;
        }

        public Warning(WarningLevel level, string message)
        {
            Ensure.That(nameof(message)).IsNotNull(message);

            this.level = level;
            this.message = message;
        }

        public Warning(Exception exception, string buttonTitle, Action onClick) : this(exception)
        {
            _buttonTitle = buttonTitle;
            this._onClick = onClick;
        }
        public Warning(Exception exception)
        {
            Ensure.That(nameof(exception)).IsNotNull(exception);

            this.level = WarningLevel.Error;
            this.exception = exception;
            this.message = exception.DisplayName() + ": " + exception.Message;
        }

        private readonly string _buttonTitle;
        private readonly Action _onClick;
        public WarningLevel level { get; }
        public string message { get; }
        public Exception exception { get; }

        public MessageType messageType
        {
            get
            {
                switch (level)
                {
                    case WarningLevel.Info:
                        return MessageType.Info;

                    case WarningLevel.Caution:
                    case WarningLevel.Severe:
                        return MessageType.Warning;

                    case WarningLevel.Error:
                        return MessageType.Error;
                    case WarningLevel.Important:
                        return MessageType.Info;

                    default:
                        return MessageType.None;
                }
            }
        }

        internal static LogType WarningLevelToLogType(WarningLevel l)
        {
            switch (l)
            {
                case WarningLevel.Info:
                case WarningLevel.Important:
                    return LogType.Log;
                case WarningLevel.Caution:
                case WarningLevel.Severe:
                    return LogType.Warning;
                default:
                    return LogType.Error;
            }
        }

        public override int GetHashCode()
        {
            return HashUtility.GetHashCode(level, message);
        }

        public override bool Equals(object obj)
        {
            var other = obj as Warning;

            if (other == null)
            {
                return false;
            }

            return level == other.level &&
                message == other.message;
        }

        public static Warning Info(string message)
        {
            return new Warning(WarningLevel.Info, message);
        }

        public static Warning Caution(string message)
        {
            return new Warning(WarningLevel.Caution, message);
        }

        public static Warning Severe(string message)
        {
            return new Warning(WarningLevel.Severe, message);
        }

        public static Warning Error(string message)
        {
            return new Warning(WarningLevel.Error, message);
        }

        public static Warning Exception(Exception exception)
        {
            return new Warning(exception);
        }

        public static WarningLevel MostSevere(params WarningLevel[] warnings)
        {
            return MostSevere((IEnumerable<WarningLevel>)warnings);
        }

        public static WarningLevel MostSevere(IEnumerable<WarningLevel> warnings)
        {
            return (WarningLevel)warnings.Select(w => (int)w).Max();
        }

        public static WarningLevel MostSevere(WarningLevel a, WarningLevel b)
        {
            return (WarningLevel)Mathf.Max((int)a, (int)b);
        }

        public static WarningLevel MostSevereLevel(List<Warning> warnings) // No alloc version
        {
            WarningLevel mostSevereWarningLevel = WarningLevel.Info;

            for (int i = 0; i < warnings.Count; i++)
            {
                var warning = warnings[i];

                if (warning.level > mostSevereWarningLevel)
                {
                    mostSevereWarningLevel = warning.level;
                }
            }

            return mostSevereWarningLevel;
        }

        public float GetHeight(float width)
        {
            return LudiqGUIUtility.GetHelpBoxHeight(message, messageType, width) + (_onClick == null ? 0 : (EditorGUIUtility.singleLineHeight + 2));
        }

        public void OnGUI(Rect position)
        {
            EditorGUI.HelpBox(position, message, messageType);

            if (exception != null && GUI.Button(position, GUIContent.none, GUIStyle.none))
            {
                Debug.LogException(exception);
            }

            if (_onClick != null)
            {
                var guiContent = new GUIContent(_buttonTitle);
                var style = EditorStyles.miniButton;
                var width = style.CalcSize(guiContent).x;
                // var rect = new Rect(position.x + 4, position.yMax - EditorGUIUtility.singleLineHeight - 2, EditorStyles.miniButton.CalcSize(guiContent).x,
                var rect = new Rect(position.xMax - width - 2, position.yMax - EditorGUIUtility.singleLineHeight - 2, width,
                    EditorGUIUtility.singleLineHeight);
                if (GUI.Button(rect, guiContent, style))
                {
                    _onClick();
                }
            }
        }
    }
}
