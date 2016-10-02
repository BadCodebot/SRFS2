using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.IO;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Diagnostics;

namespace SRFS.Model.Data {

    public class FileSystemEntry : INotifyPropertyChanged {

        protected FileSystemEntry(int id, string name) {
            _id = id;
            _name = name;
            _parentID = Constants.NoID;
            _attributes = FileAttributes.Normal;
            DateTime now = DateTime.UtcNow;
            _lastWriteTime = now;
            _creationTime = now;
            _lastAccessTime = now;
            _owner = WindowsIdentity.GetCurrent().User;
            _group = new SecurityIdentifier(WellKnownSidType.NullSid, null);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public int ID => _id;

        public string Name {
            get {
                lock (_lock) return _name;
            }
            set {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (value.Length > Constants.MaximumNameLength) throw new ArgumentException();
                lock (_lock) _name = value;
                NotifyPropertyChanged();
            }
        }

        public int ParentID {
            get {
                lock (_lock) return _parentID;
            }
            set {
                lock (_lock) _parentID = value;
                NotifyPropertyChanged();
            }
        }

        public virtual FileAttributes Attributes {
            get {
                lock (_lock) return _attributes;
            }
            set {
                lock (_lock) _attributes = value;
                NotifyPropertyChanged();
            }
        }

        public DateTime LastWriteTime {
            get {
                lock (_lock) return _lastWriteTime;
            }
            set {
                lock (_lock) _lastWriteTime = value;
                NotifyPropertyChanged();
            }
        }

        public DateTime CreationTime {
            get {
                lock (_lock) return _creationTime;
            }
            set {
                lock (_lock) _creationTime = value;
                NotifyPropertyChanged();
            }
        }

        public DateTime LastAccessTime {
            get {
                lock (_lock) return _lastAccessTime;
            }
            set {
                lock (_lock) _lastAccessTime = value;
                NotifyPropertyChanged();
            }
        }

        public SecurityIdentifier Owner {
            get {
                lock (_lock) return _owner;
            }
            set {
                if (value == null) throw new ArgumentNullException(nameof(value));
                lock (_lock) _owner = value;
                NotifyPropertyChanged();
            }
        }

        public SecurityIdentifier Group {
            get {
                lock (_lock) return _group;
            }
            set {
                if (value == null) throw new ArgumentNullException(nameof(value));
                lock (_lock) _group = value;
                NotifyPropertyChanged();
            }
        }

        protected void NotifyPropertyChanged([CallerMemberName] string name = "") {
            if (PropertyChanged == null) return;
            PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        protected bool Equals(FileSystemEntry other) {
            return _id == other._id
                && StringComparer.OrdinalIgnoreCase.Compare(_name, other._name) == 0
                && _parentID == other._parentID
                && _attributes == other._attributes
                && _lastWriteTime == other._lastWriteTime
                && _creationTime == other._creationTime
                && _lastAccessTime == other._lastAccessTime
                && _owner.Equals(other._owner)
                && _group.Equals(other._group);
        }

        protected object Lock => _lock;
        private object _lock = new object();

        private readonly int _id;
        private string _name;
        private int _parentID;
        private FileAttributes _attributes;
        private DateTime _lastWriteTime;
        private DateTime _creationTime;
        private DateTime _lastAccessTime;
        private SecurityIdentifier _owner;
        private SecurityIdentifier _group;

        public virtual void Save(ByteBlock byteBlock, int offset) {
            byteBlock.Set(offset, ID);
            offset += sizeof(int);

            Debug.Assert(Name.Length <= byte.MaxValue);
            byteBlock.Set(offset, (byte)Name.Length);
            offset += sizeof(byte);

            byteBlock.Set(offset, Name);
            byteBlock.Clear(offset + Name.Length * sizeof(char), (Constants.MaximumNameLength - Name.Length) * sizeof(char));
            offset += Constants.MaximumNameLength * sizeof(char);

            byteBlock.Set(offset, ParentID);
            offset += sizeof(int);

            byteBlock.Set(offset, (int)Attributes);
            offset += sizeof(int);

            byteBlock.Set(offset, LastWriteTime.Ticks);
            offset += sizeof(long);

            byteBlock.Set(offset, CreationTime.Ticks);
            offset += sizeof(long);

            byteBlock.Set(offset, LastAccessTime.Ticks);
            offset += sizeof(long);

            byteBlock.Set(offset, Owner);
            offset += Constants.SecurityIdentifierLength;

            byteBlock.Set(offset, Group);
        }

        protected FileSystemEntry(ByteBlock byteBlock, int offset) {
            int id = byteBlock.ToInt32(offset);
            offset += sizeof(int);

            int nameLength = byteBlock.ToByte(offset);
            offset += sizeof(byte);

            string name = byteBlock.ToString(offset, nameLength);
            offset += Constants.MaximumNameLength * sizeof(char);

            int parentID = byteBlock.ToInt32(offset);
            offset += sizeof(int);

            FileAttributes attributes = (FileAttributes)byteBlock.ToInt32(offset);
            offset += sizeof(int);

            DateTime lastWriteTime = new DateTime(byteBlock.ToInt64(offset));
            offset += sizeof(long);

            DateTime creationTime = new DateTime(byteBlock.ToInt64(offset));
            offset += sizeof(long);

            DateTime lastAccessTime = new DateTime(byteBlock.ToInt64(offset));
            offset += sizeof(long);

            SecurityIdentifier owner = byteBlock.ToSecurityIdentifier(offset);
            offset += Constants.SecurityIdentifierLength;

            SecurityIdentifier group = byteBlock.ToSecurityIdentifier(offset);
            offset += Constants.SecurityIdentifierLength;

            _id = id;
            _name = name;
            _parentID = parentID;
            _attributes = attributes;
            _lastWriteTime = lastWriteTime;
            _creationTime = creationTime;
            _lastAccessTime = lastAccessTime;
            _owner = owner;
            _group = group;
        }

        protected const int FileSystemEntryStorageLength =
            sizeof(int) + // ID
            sizeof(byte) + // name length
            Constants.MaximumNameLength * sizeof(char) + // name
            sizeof(int) + // parent ID
            sizeof(int) + // attributes
            sizeof(long) + // last write time
            sizeof(long) + // creation time
            sizeof(long) + // last access time
            Constants.SecurityIdentifierLength + // owner
            Constants.SecurityIdentifierLength; // group
    }
}