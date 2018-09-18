﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eleon.Modding;

namespace EmpyrionModApi
{
    public class Faction : IEquatable<Faction>
    {
        public int Id { get; private set; }

        public byte Origin { get; private set; }

        public string Initials { get; private set; }

        public string Name { get; private set; }

        public Task SendMessage(MessagePriority priority, float time, string format, params object[] args)
        {
            string msg = string.Format(format, args);
            return _gameServerConnection.SendRequest(
                Eleon.Modding.CmdId.Request_InGameMessage_Faction,
                new Eleon.Modding.IdMsgPrio(Id, msg, (byte)priority, time));
        }

        #region Common overloads

        public override bool Equals(object obj)
        {
            // If parameter is null return false.
            if (obj == null)
            {
                return false;
            }

            if (!(obj is Faction))
            {
                return false;
            }

            Faction p = obj as Faction;

            // Return true if the Name fields match:
            return (Id == p.Id);
        }

        public bool Equals(Faction other)
        {
            return other != (Faction)null &&
                   Id == other.Id;
        }

        public override string ToString()
        {
            return Id.ToString();
        }

        public override int GetHashCode()
        {
            return 1075102471 + Id.GetHashCode();
        }

        public static bool operator ==(Faction lhs, Faction rhs)
        {
            return EqualityComparer<Faction>.Default.Equals(lhs, rhs);
        }

        public static bool operator !=(Faction lhs, Faction rhs)
        {
            return !(lhs == rhs);
        }

        public static bool operator ==(Faction lhs, int id)
        {
            return (lhs.Id == id);
        }

        public static bool operator !=(Faction lhs, int id)
        {
            return (lhs.Id != id);
        }

        #endregion

        internal Faction(IGameServerConnection gameServerConnection, FactionInfo factionInfo)
        {
            _gameServerConnection = gameServerConnection;
            Origin = factionInfo.origin;
            Id = factionInfo.factionId;
            Initials = factionInfo.abbrev;
            Name = factionInfo.name;
        }

        internal void UpdateInfo(FactionInfo factionInfo)
        {
            Origin = factionInfo.origin;
            Id = factionInfo.factionId;
            Initials = factionInfo.abbrev;
            Name = factionInfo.name;
        }

        private IGameServerConnection _gameServerConnection;
    }
}
