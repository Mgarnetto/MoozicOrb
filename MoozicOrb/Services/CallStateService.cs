using System.Collections.Concurrent;

namespace MoozicOrb.Services
{
    public class CallStateService
    {
        // Key: CallId, Value: (CallerId, CalleeId)
        private readonly ConcurrentDictionary<string, (int CallerId, int CalleeId)> _activeCalls = new();

        // Key: UserId, Value: CallId (Fast lookup for "Is Busy")
        private readonly ConcurrentDictionary<int, string> _userBusyLookup = new();

        public bool IsUserBusy(int userId)
        {
            return _userBusyLookup.ContainsKey(userId);
        }

        public void RegisterCall(string callId, int callerId, int calleeId)
        {
            _activeCalls[callId] = (callerId, calleeId);
            _userBusyLookup[callerId] = callId;
            _userBusyLookup[calleeId] = callId;
        }

        public (int CallerId, int CalleeId)? GetCallParticipants(string callId)
        {
            if (_activeCalls.TryGetValue(callId, out var participants))
                return participants;
            return null;
        }

        public void EndCall(string callId)
        {
            if (_activeCalls.TryRemove(callId, out var p))
            {
                // Cleanup busy status
                // Note: In production, verify the user isn't in ANOTHER call before removing
                _userBusyLookup.TryRemove(p.CallerId, out _);
                _userBusyLookup.TryRemove(p.CalleeId, out _);
            }
        }
    }
}