using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.IO;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Diagnostics;

namespace SRFS.Model.Data {

    public class FileSystemObject : INotifyPropertyChanged {

        protected FileSystemObject(int id, string name) {
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

        protected bool Equals(FileSystemObject other) {
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

        public virtual void Save(DataBlock dataBlock, int offset) {
            dataBlock.Set(offset, ID);
            offset += sizeof(int);

            Debug.Assert(Name.Length <= byte.MaxValue);
            dataBlock.Set(offset, (byte)Name.Length);
            offset += sizeof(byte);

            dataBlock.Set(offset, Name);
            dataBlock.Clear(offset + Name.Length * sizeof(char), (Constants.MaximumNameLength - Name.Length) * sizeof(char));
            offset += Constants.MaximumNameLength * sizeof(char);

            dataBlock.Set(offset, ParentID);
            offset += sizeof(int);

            dataBlock.Set(offset, (int)Attributes);
            offset += sizeof(int);

            dataBlock.Set(offset, LastWriteTime.Ticks);
            offset += sizeof(long);

            dataBlock.Set(offset, CreationTime.Ticks);
            offset += sizeof(long);

            dataBlock.Set(offset, LastAccessTime.Ticks);
            offset += sizeof(long);

            dataBlock.Set(offset, Owner);
            offset += Constants.SecurityIdentifierLength;

            dataBlock.Set(offset, Group);
        }

        protected FileSystemObject(DataBlock dataBlock, int offset) {
            int id = dataBlock.ToInt32(offset);
            offset += sizeof(int);

            int nameLength = dataBlock.ToByte(offset);
            offset += sizeof(byte);

            string name = dataBlock.ToString(offset, nameLength);
            offset += Constants.MaximumNameLength * sizeof(char);

            int parentID = dataBlock.ToInt32(offset);
            offset += sizeof(int);

            FileAttributes attributes = (FileAttributes)dataBlock.ToInt32(offset);
            offset += sizeof(int);

            DateTime lastWriteTime = new DateTime(dataBlock.ToInt64(offset));
            offset += sizeof(long);

            DateTime creationTime = new DateTime(dataBlock.ToInt64(offset));
            offset += sizeof(long);

            DateTime lastAccessTime = new DateTime(dataBlock.ToInt64(offset));
            offset += sizeof(long);

            SecurityIdentifier owner = dataBlock.ToSecurityIdentifier(offset);
            offset += Constants.SecurityIdentifierLength;

            SecurityIdentifier group = dataBlock.ToSecurityIdentifier(offset);
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