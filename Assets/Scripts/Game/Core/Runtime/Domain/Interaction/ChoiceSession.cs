using System;
using System.Collections.Generic;
using Game.Core.Domain.Cards;

namespace Game.Core.Domain.Interaction
{
    public sealed class ChoiceSession
    {
        private readonly List<string> _optionKeys;

        public ChoiceSession(string sessionId, int optionCount, string choiceKind = null, CardInstanceId sourceCardId = default, IEnumerable<string> optionKeys = null)
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
            SourceCardId = sourceCardId;
            SelectedOptionIndex = -1;
            _optionKeys = optionKeys != null
                ? new List<string>(optionKeys)
                : new List<string>(optionCount);

            while (_optionKeys.Count < optionCount)
            {
                _optionKeys.Add(string.Empty);
            }
        }

        public string SessionId { get; }
        public int OptionCount { get; }
        public string ChoiceKind { get; }
        public CardInstanceId SourceCardId { get; }
        public bool IsResolved { get; private set; }
        public int SelectedOptionIndex { get; private set; }
        public IReadOnlyList<string> OptionKeys
        {
            get { return _optionKeys; }
        }

        public bool IsValidOption(int optionIndex)
        {
            return optionIndex >= 0 && optionIndex < OptionCount;
        }

        public string GetOptionKey(int optionIndex)
        {
            return IsValidOption(optionIndex) ? _optionKeys[optionIndex] : string.Empty;
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

        public ChoiceSession Open(string sessionId, int optionCount, string choiceKind = null, CardInstanceId sourceCardId = default, IEnumerable<string> optionKeys = null)
        {
            ChoiceSession session = new ChoiceSession(sessionId, optionCount, choiceKind, sourceCardId, optionKeys);
            _sessions[session.SessionId] = session;
            return session;
        }

        public ChoiceSession Restore(string sessionId, int optionCount, string choiceKind, bool isResolved, int selectedOptionIndex, CardInstanceId sourceCardId = default, IEnumerable<string> optionKeys = null)
        {
            ChoiceSession session = new ChoiceSession(sessionId, optionCount, choiceKind, sourceCardId, optionKeys);
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
