using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.IO;
using System.Collections.Generic;
using System.Security.AccessControl;

namespace SRFS.Model.Data {

    public class FileSystemEntry : INotifyPropertyChanged {

        protected FileSystemEntry(FileSystem fileSystem, int id, string name) {
            _fileSystem = fileSystem;
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

        public FileAttributes Attributes {
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

        public IEnumerable<FileSystemAccessRule> AccessRules => _fileSystem.GetAccessRules(this);

        public IEnumerable<FileSystemAuditRule> AuditRules => _fileSystem.GetAuditRules(this);

        public void AddAccessRule(FileSystemAccessRule rule) {

        }

        protected void NotifyPropertyChanged([CallerMemberName] string name = "") {
            if (PropertyChanged == null) return;
            PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        protected FileSystem FileSystem => _fileSystem;
        private FileSystem _fileSystem;

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
    }
}
