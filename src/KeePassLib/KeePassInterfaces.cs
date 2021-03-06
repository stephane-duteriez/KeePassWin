﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;

namespace KeePass
{
    public interface IKeePassDatabase
    {
        KeePassId Id { get; }

        string Name { get; }

        IKeePassGroup Root { get; }

        bool Modified { get; }

        Task SaveAsync();
    }

    public interface IKeePassId : INotifyPropertyChanged
    {
        KeePassId Id { get; set; }
    }

    public interface IKeePassGroup : IKeePassId
    {
        string Name { get; set; }

        string Notes { get; }

        IKeePassGroup Parent { get; }

        IList<IKeePassEntry> Entries { get; }

        IList<IKeePassGroup> Groups { get; }

        IKeePassEntry CreateEntry(string title);

        IKeePassGroup CreateGroup(string name);

        void Remove();
    }

    public interface IKeePassEntry : IKeePassId
    {
        string UserName { get; set; }

        string Password { get; set; }

        string Title { get; set; }

        string Notes { get; set; }

        // IList<IKeePassAttachment> Attachment { get; }
        string Url { get; set; }

        byte[] Icon { get; set; }

        IKeePassGroup Group { get; set; }

        void Remove();
    }

    public interface IKeePassAttachment : IKeePassId
    {
    }
}
