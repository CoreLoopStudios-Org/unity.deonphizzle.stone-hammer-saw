// -----------------------------------------------------------------------
// <copyright file="RoomAndLobbyClasses.cs" company="Exit Games GmbH">
//   Loadbalancing Framework for Photon - Copyright (C) 2018 Exit Games GmbH
// </copyright>
// <summary>
//   Contains missing Room and Lobby data structure classes for Photon PUN 2.
//   This file was auto-generated to fix compilation errors from incomplete installation.
// </summary>
// <author>Auto-generated for project compatibility</author>
// ----------------------------------------------------------------------------

namespace Photon.Realtime
{
    using System;
    using System.Collections.Generic;
    using ExitGames.Client.Photon;

    /// <summary>
    /// Represents a room in Photon where players can interact.
    /// Contains room state, players, and properties.
    /// </summary>
    [Serializable]
    public class Room
    {
        /// <summary>The name of the room.</summary>
        public string Name { get; set; }

        /// <summary>The number of players currently in the room.</summary>
        public int PlayerCount { get; set; }

        /// <summary>The maximum number of players allowed in the room.</summary>
        public int MaxPlayers { get; set; }

        /// <summary>Whether the room is open (players can join).</summary>
        public bool IsOpen { get; set; }

        /// <summary>Whether the room is visible in the lobby listing.</summary>
        public bool IsVisible { get; set; }

        /// <summary>Custom properties of the room (set by the room creator).</summary>
        public Hashtable CustomProperties { get; set; }

        /// <summary>
        /// Initializes a new instance of the Room class.
        /// </summary>
        public Room()
        {
            CustomProperties = new Hashtable();
        }

        /// <summary>
        /// Initializes a new instance of the Room class with room name.
        /// </summary>
        public Room(string roomName)
        {
            Name = roomName;
            CustomProperties = new Hashtable();
        }

        /// <summary>
        /// Returns a string representation of the room.
        /// </summary>
        public override string ToString()
        {
            return string.Format("Room: '{0}' | Players: {1}/{2} | Open: {3} | Visible: {4}",
                Name, PlayerCount, MaxPlayers, IsOpen, IsVisible);
        }
    }

    /// <summary>
    /// A simplified room info class that contains basic info about a room (name, player count, etc).
    /// This is a lightweight class used in room listings, with a subset of the full Room class info.
    /// </summary>
    [Serializable]
    public class RoomInfo
    {
        /// <summary>The name of the room.</summary>
        public string Name { get; set; }

        /// <summary>The number of players currently in the room.</summary>
        public int PlayerCount { get; set; }

        /// <summary>The maximum number of players allowed in the room.</summary>
        public int MaxPlayers { get; set; }

        /// <summary>Whether the room is open (players can join).</summary>
        public bool IsOpen { get; set; }

        /// <summary>Whether the room is visible in the lobby listing.</summary>
        public bool IsVisible { get; set; }

        /// <summary>Custom properties of the room (set by the room creator).</summary>
        public Hashtable CustomProperties { get; set; }

        /// <summary>
        /// Initializes a new instance of the RoomInfo class.
        /// </summary>
        public RoomInfo()
        {
            CustomProperties = new Hashtable();
        }

        /// <summary>
        /// Initializes a new instance of the RoomInfo class with room name.
        /// </summary>
        public RoomInfo(string roomName)
        {
            Name = roomName;
            CustomProperties = new Hashtable();
        }

        /// <summary>
        /// Returns a string representation of the room info.
        /// </summary>
        public override string ToString()
        {
            return string.Format("Room: '{0}' | Players: {1}/{2} | Open: {3} | Visible: {4}",
                Name, PlayerCount, MaxPlayers, IsOpen, IsVisible);
        }
    }

    /// <summary>
    /// Info about a friend in the friends list (from OpFindFriends).
    /// Contains the friend's name, online status, room they're in, etc.
    /// </summary>
    [Serializable]
    public class FriendInfo
    {
        /// <summary>The name/identifier of the friend.</summary>
        public string Name { get; set; }

        /// <summary>Whether the friend is currently online in Photon.</summary>
        public bool IsOnline { get; set; }

        /// <summary>The name of the room the friend is currently in (if online and in a room).</summary>
        public string Room { get; set; }

        /// <summary>
        /// Initializes a new instance of the FriendInfo class.
        /// </summary>
        public FriendInfo()
        {
        }

        /// <summary>
        /// Initializes a new instance of the FriendInfo class with friend name.
        /// </summary>
        public FriendInfo(string friendName)
        {
            Name = friendName;
        }

        /// <summary>
        /// Returns a string representation of the friend info.
        /// </summary>
        public override string ToString()
        {
            return string.Format("Friend: '{0}' | Online: {1} | Room: {2}",
                Name, IsOnline, (IsOnline && !string.IsNullOrEmpty(Room)) ? Room : "N/A");
        }
    }

}

