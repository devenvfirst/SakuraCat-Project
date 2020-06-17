// Copyright 2017-2020 Elringus (Artyom Sovetnikov). All Rights Reserved.

using System;
using System.Collections.Generic;

namespace Naninovel.UI
{
    /// <summary>
    /// Represents a set of UI elements used for managing backlog messages.
    /// </summary>
    public interface IBacklogUI : IManagedUI
    {
        /// <summary>
        /// Adds message to the log.
        /// </summary>
        /// <param name="message">Text of the message. Formatting (rich text) tags supported.</param>
        /// <param name="actorId">ID of the actor to which the message belongs or null.</param>
        /// <param name="voiceClipName">Name of the voice clip associated with the message or null.</param>
        void AddMessage (string message, string actorId = null, string voiceClipName = null);
        /// <summary>
        /// Appends message to the last message of the log (if exists).
        /// </summary>
        void AppendMessage (string message, string voiceClipName = null);
        /// <summary>
        /// Adds a selected choice summary.
        /// </summary>
        /// <param name="summaryMap">Collection of [choice summary] -> [selected] items.</param>
        void AddChoice (List<Tuple<string, bool>> summary);
        /// <summary>
        /// Removes all the messages from the backlog.
        /// </summary>
        void Clear ();
    }
}
