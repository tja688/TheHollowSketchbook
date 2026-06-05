using System;
using System.Collections.Generic;
using Game.Core.Domain.Cards;
using Game.Core.Domain.Grid;

namespace Game.Core.Domain.Interaction
{
    public sealed class IntentPreview
    {
        public IntentPreview(bool isValid, IntentKind kind, string invalidReasonKey, IReadOnlyList<GridCoord> highlightCells, IReadOnlyList<CardInstanceId> highlightCards)
        {
            IsValid = isValid;
            Kind = kind;
            InvalidReasonKey = invalidReasonKey ?? string.Empty;
            HighlightCells = highlightCells ?? Array.Empty<GridCoord>();
            HighlightCards = highlightCards ?? Array.Empty<CardInstanceId>();
        }

        public bool IsValid { get; }
        public IntentKind Kind { get; }
        public string InvalidReasonKey { get; }
        public IReadOnlyList<GridCoord> HighlightCells { get; }
        public IReadOnlyList<CardInstanceId> HighlightCards { get; }
    }

    public sealed class IntentValidationResult
    {
        private IntentValidationResult(bool isValid, string failureCode)
        {
            IsValid = isValid;
            FailureCode = failureCode ?? string.Empty;
        }

        public bool IsValid { get; }
        public string FailureCode { get; }

        public static IntentValidationResult Valid()
        {
            return new IntentValidationResult(true, string.Empty);
        }

        public static IntentValidationResult Invalid(string failureCode)
        {
            return new IntentValidationResult(false, failureCode);
        }
    }
}
