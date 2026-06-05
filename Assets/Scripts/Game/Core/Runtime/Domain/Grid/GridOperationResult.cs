using System;
using System.Collections.Generic;
using Game.Core.Actions;
using Game.Core.Domain.Events;

namespace Game.Core.Domain.Grid
{
    public sealed class GridOperationResult
    {
        private static readonly IReadOnlyList<DomainEvent> EmptyEvents = Array.Empty<DomainEvent>();
        private static readonly IReadOnlyList<GameAction> EmptyActions = Array.Empty<GameAction>();

        private GridOperationResult(bool succeeded, string failureCode, IReadOnlyList<DomainEvent> events, IReadOnlyList<GameAction> followUpActions)
        {
            Succeeded = succeeded;
            FailureCode = failureCode ?? string.Empty;
            Events = events ?? EmptyEvents;
            FollowUpActions = followUpActions ?? EmptyActions;
        }

        public bool Succeeded { get; }
        public string FailureCode { get; }
        public IReadOnlyList<DomainEvent> Events { get; }
        public IReadOnlyList<GameAction> FollowUpActions { get; }

        public static GridOperationResult Success(IReadOnlyList<DomainEvent> events)
        {
            return new GridOperationResult(true, string.Empty, events, EmptyActions);
        }

        public static GridOperationResult Success(IReadOnlyList<DomainEvent> events, IReadOnlyList<GameAction> followUpActions)
        {
            return new GridOperationResult(true, string.Empty, events, followUpActions);
        }

        public static GridOperationResult Failure(string failureCode)
        {
            return new GridOperationResult(false, failureCode, EmptyEvents, EmptyActions);
        }
    }

    public enum RemoveReason
    {
        None,
        Defeated,
        Destroyed,
        Collected,
        Consumed,
        Replaced,
        Scripted
    }

    public enum FlipReason
    {
        None,
        PlayerAdjacentReveal,
        RevealAfterTopRemoved,
        Manual,
        Scripted
    }

    public enum MoveReason
    {
        None,
        PlayerMove,
        HookRope,
        Teleport,
        Scripted
    }
}
