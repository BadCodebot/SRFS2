using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DokanNet;
using FileShare = System.IO.FileShare;
using System.Security.AccessControl;
using SRFS.Model.Data;
using SRFS.Model;
using FileAttributes = System.IO.FileAttributes;
using System.Security.Principal;

namespace SRFS {

    public class DokanDirectory : IDokanFileSystemObject {

        private const FileAccess READ_ACCESS = FileAccess.GenericRead | FileAccess.ReadAttributes | FileAccess.ReadData | FileAccess.ReadExtendedAttributes | FileAccess.ReadPermissions;
        private const FileAccess WRITE_ACCESS = FileAccess.GenericWrite | FileAccess.WriteAttributes | FileAccess.WriteData | FileAccess.WriteExtendedAttributes | FileAccess.AppendData |
            FileAccess.ChangePermissions | FileAccess.SetOwnership;

        public DokanDirectory(FileSystem fileSystem, Directory dir) {
            _fileSystem = fileSystem;
            _directory = dir;
        }

        public void Move(Directory newDirectory, string newName) {
            _fileSystem.MoveDirectory(_directory, newDirectory, newName);
        }

        public string Name => _directory.Name;

        public int ParentID => _directory.ParentID;

        public DirectoryHandle Open(FileAccess access, FileShare share) {

            lock (Lock) {
                if ((access & READ_ACCESS) != 0 && _openShares.Values.Any(s => (s & FileShare.Read) == 0)) throw new SharingException();
                if ((access & WRITE_ACCESS) != 0 && _openShares.Values.Any(s => (s & FileShare.Write) == 0)) throw new SharingException();
                if ((access & FileAccess.Delete) != 0 && _openShares.Values.Any(s => (s & FileShare.Delete) == 0)) throw new SharingException();

                FileAccess totalAccess = 0;
                foreach (var a in _openAccesses.Values) totalAccess |= a;

                if ((totalAccess & READ_ACCESS) != 0 && (share & FileShare.Read) == 0) throw new SharingException();
                if ((totalAccess & WRITE_ACCESS) != 0 && (share & FileShare.Write) == 0) throw new SharingException();
                if ((totalAccess & FileAccess.Delete) != 0 && (share & FileShare.Delete) == 0) throw new SharingException();

                DirectoryHandle h = Handles.CreateNewDirectoryHandle(this, access);
                _openAccesses.Add(h.ID, access);
                _openShares.Add(h.ID, share);

                return h;
            }
        }

        public void Delete() {
            _fileSystem.RemoveDirectory(_directory);
        }

        public FileSystemSecurity GetSecurity(AccessControlSections sections) {
            lock (Lock) {

                DirectorySecurity security = new DirectorySecurity();

                if ((sections & AccessControlSections.Owner) != 0) security.SetOwner(_directory.Owner);
                if ((sections & AccessControlSections.Group) != 0) security.SetGroup(_directory.Group);
                if ((sections & AccessControlSections.Access) != 0) {
                    foreach (var r in _fileSystem.GetAccessRules(_directory)) security.AddAccessRule(
                        new FileSystemAccessRule(r.IdentityReference, r.FileSystemRights, r.InheritanceFlags, r.PropagationFlags, r.AccessControlType));
                }
                if ((sections & AccessControlSections.Audit) != 0) {
                    foreach (var r in _fileSystem.GetAuditRules(_directory)) security.AddAuditRule(
                        new FileSystemAuditRule(r.IdentityReference, r.FileSystemRights, r.InheritanceFlags, r.PropagationFlags, r.AuditFlags));
                }

                return security;
            }
        }

        public void SetSecurity(FileSystemSecurity security, AccessControlSections sections) {
            lock (Lock) {

                if ((sections & AccessControlSections.Owner) != 0) _directory.Owner = (SecurityIdentifier)security.GetOwner(typeof(SecurityIdentifier));
                if ((sections & AccessControlSections.Group) != 0) _directory.Group = (SecurityIdentifier)security.GetGroup(typeof(SecurityIdentifier));
                if ((sections & AccessControlSections.Access) != 0) {
                    _fileSystem.RemoveAccessRules(_directory);
                    foreach (var r in security.GetAccessRules(true, false, typeof(SecurityIdentifier))) {
                        _fileSystem.AddAccessRule(_directory, (FileSystemAccessRule)r);
                    }
                }
                if ((sections & AccessControlSections.Audit) != 0) {
                    _fileSystem.RemoveAuditRules(_directory);
                    foreach (var r in security.GetAuditRules(true, false, typeof(SecurityIdentifier))) {
                        _fileSystem.AddAuditRule(_directory, (FileSystemAuditRule)r);
                    }
                }
            }
        }

        public void SetTime(DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime) {
            lock (Lock) {

                if (creationTime.HasValue) _directory.CreationTime = creationTime.Value;
                if (lastAccessTime.HasValue) _directory.LastAccessTime = lastAccessTime.Value;
                if (lastWriteTime.HasValue) _directory.LastWriteTime = lastWriteTime.Value;
            }
        }


        public void SetAttributes(FileAttributes attributes) {
            lock (Lock) {
                _directory.Attributes = attributes;
            }
        }

        public FileInformation GetInformation() {
            lock (Lock) {
                return new FileInformation() {
                    Attributes = _directory.Attributes,
                    LastAccessTime = _directory.LastAccessTime,
                    LastWriteTime = _directory.LastWriteTime,
                    CreationTime = _directory.CreationTime,
                    Length = 0,
                    FileName = _directory.Name
                };
            }
        }


        public bool IsOpen {
            get {
                lock (Lock) return _openAccesses.Count > 0;
            }
        }

        public void Close(int id) {
            lock (Lock) {
                _openAccesses.Remove(id);
                _openShares.Remove(id);
            }
        }

        public IDictionary<string, Directory> Subdirectories => _fileSystem.GetContainedDirectories(_directory);
        public IDictionary<string, File> Files => _fileSystem.GetContainedFiles(_directory);

        private object Lock = new object();
        private FileSystem _fileSystem;
        private Directory _directory;

        private SortedList<int, FileAccess> _openAccesses = new SortedList<int, FileAccess>();
        private SortedList<int, FileShare> _openShares = new SortedList<int, FileShare>();
    }
}
