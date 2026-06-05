using System;
using System.Collections.Generic;

namespace Game.Core.Domain.Interaction
{
    public sealed class ChoiceSession
    {
        public ChoiceSession(string sessionId, int optionCount, string choiceKind = null)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                throw new ArgumentException("Choice session id cannot be empty.", nameof(sessionId));
            }

            if (optionCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(optionCount), "Choice session must expose at least one option.");
            }

            SessionId = sessionId;
            OptionCount = optionCount;
            ChoiceKind = choiceKind ?? string.Empty;
            SelectedOptionIndex = -1;
        }

        public string SessionId { get; }
        public int OptionCount { get; }
        public string ChoiceKind { get; }
        public bool IsResolved { get; private set; }
        public int SelectedOptionIndex { get; private set; }

        public bool IsValidOption(int optionIndex)
        {
            return optionIndex >= 0 && optionIndex < OptionCount;
        }

        public bool TryResolve(int optionIndex)
        {
            if (IsResolved || !IsValidOption(optionIndex))
            {
                return false;
            }

            IsResolved = true;
            SelectedOptionIndex = optionIndex;
            return true;
        }

        internal void RestoreResolution(bool isResolved, int selectedOptionIndex)
        {
            IsResolved = isResolved;
            SelectedOptionIndex = isResolved ? selectedOptionIndex : -1;
        }
    }

    public sealed class ChoiceSessionStore
    {
        private readonly Dictionary<string, ChoiceSession> _sessions = new Dictionary<string, ChoiceSession>();

        public IReadOnlyCollection<ChoiceSession> Sessions
        {
            get { return _sessions.Values; }
        }

        public ChoiceSession Open(string sessionId, int optionCount, string choiceKind = null)
        {
            ChoiceSession session = new ChoiceSession(sessionId, optionCount, choiceKind);
            _sessions[session.SessionId] = session;
            return session;
        }

        public ChoiceSession Restore(string sessionId, int optionCount, string choiceKind, bool isResolved, int selectedOptionIndex)
        {
            ChoiceSession session = new ChoiceSession(sessionId, optionCount, choiceKind);
            session.RestoreResolution(isResolved, selectedOptionIndex);
            _sessions[session.SessionId] = session;
            return session;
        }

        public bool TryGet(string sessionId, out ChoiceSession session)
        {
            return _sessions.TryGetValue(sessionId ?? string.Empty, out session);
        }

        public bool TryResolve(string sessionId, int optionIndex, out ChoiceSession session)
        {
            if (!TryGet(sessionId, out session))
            {
                return false;
            }

            return session.TryResolve(optionIndex);
        }

        public void Clear()
        {
            _sessions.Clear();
        }
    }
}
