using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.IO;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Diagnostics;
using System.Text;

namespace SRFS.Model.Data {

    public class FileSystemObject : INotifyPropertyChanged, INotifyChanged {

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
        public event ChangedEventHandler Changed;

        public int ID => _id;

        public string Name {
            get {
                lock (_lock) return _name;
            }
            set {
                if (value == null) throw new ArgumentNullException(nameof(value));
                if (value.Length > Constants.MaximumNameLength) throw new ArgumentException();
                _name = value;
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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            Changed?.Invoke(this);
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

        private const int IDOffset = 0;
        private const int IDLength = sizeof(int);

        private const int NameLengthOffset = IDOffset + IDLength;
        private const int NameLengthLength = sizeof(byte);

        private const int NameOffset = NameLengthOffset + NameLengthLength;
        private const int NameLength = Constants.MaximumNameLength * sizeof(char);

        private const int ParentIDOffset = NameOffset + NameLength;
        private const int ParentIDLength = sizeof(int);

        private const int AttributesOffset = ParentIDOffset + ParentIDLength;
        private const int AttributesLength = sizeof(int);

        private const int LastWriteTimeOffset = AttributesOffset + AttributesLength;
        private const int LastWriteTimeLength = sizeof(long);

        public virtual void Write(BinaryWriter writer) {
            writer.Write(ID);
            writer.WriteSrfsString(Name);
            writer.Write(ParentID);
            writer.Write(Attributes);
            writer.Write(LastWriteTime);
            writer.Write(CreationTime);
            writer.Write(LastAccessTime);
            writer.Write(Owner);
            writer.Write(Group);
        }

        protected FileSystemObject(BinaryReader reader) {
            _id = reader.ReadInt32();
            _name = reader.ReadSrfsString();
            _parentID = reader.ReadInt32();
            _attributes = reader.ReadFileAttributes();
            _lastWriteTime = reader.ReadDateTime();
            _creationTime = reader.ReadDateTime();
            _lastAccessTime = reader.ReadDateTime();
            _owner = reader.ReadSecurityIdentifier();
            _group = reader.ReadSecurityIdentifier();
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