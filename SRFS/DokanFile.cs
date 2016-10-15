using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DokanNet;
using FileShare = System.IO.FileShare;
using FileAttributes = System.IO.FileAttributes;
using System.Security.AccessControl;
using SRFS.Model;
using SRFS.Model.Data;
using System.Security.Principal;

namespace SRFS {

    public class DokanFile : IDokanFileSystemObject {

        public DokanFile(FileSystem fileSystem, File file) {
            _fileSystem = fileSystem;
            _file = file;
        }

        public bool IsOpen { get { lock (Lock) return _isOpen; } }

        private bool _isOpen = false;

        public FileHandle Open() {
            lock (Lock) {
                if (_isOpen) throw new SharingException();
                _fileIO = new Lazy<FileIO>(() => new FileIO(_fileSystem, _file));
                _isOpen = true;

                FileHandle h = Handles.CreateNewFileHandle(this);
                return h;
            }
        }

        public void Close() {
            lock (Lock) {
                if (!_isOpen) return;
                if (_fileIO.IsValueCreated) _fileIO.Value.Dispose();
                _fileIO = null;
                _isOpen = false;
            }
        }


        public int WriteFile(byte[] buffer, long offset) {
            lock (Lock) {
                if (!IsOpen) throw new InvalidOperationException();
                return _fileIO.Value.WriteFile(buffer, offset);
            }
        }

        public int ReadFile(byte[] buffer, long offset) {
            lock (Lock) {
                if (!IsOpen) throw new InvalidOperationException();
                return _fileIO.Value.ReadFile(buffer, offset);
            }
        }

        public void Flush() {
            lock (Lock) {
                if (!IsOpen) throw new InvalidOperationException();
                if (_fileIO.IsValueCreated) _fileIO.Value.Flush();
            }
        }

        public void SetTime(DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime) {
            lock (Lock) {

                if (creationTime.HasValue) _file.CreationTime = creationTime.Value;
                if (lastAccessTime.HasValue) _file.LastAccessTime = lastAccessTime.Value;
                if (lastWriteTime.HasValue) _file.LastWriteTime = lastWriteTime.Value;
            }
        }

        public void Delete() {
            lock (Lock) {
                _fileSystem.RemoveFile(_file);
            }
        }


        public void Move(Directory newDirectory, string newName) {
            lock (Lock) {
                _fileSystem.MoveFile(_file, newDirectory, newName);
            }
        }

        public string Name => _file.Name;

        public int ParentID => _file.ParentID;

        public FileSystemSecurity GetSecurity(AccessControlSections sections) {
            lock (Lock) {

                FileSecurity security = new FileSecurity();

                if ((sections & AccessControlSections.Owner) != 0) security.SetOwner(_file.Owner);
                if ((sections & AccessControlSections.Group) != 0) security.SetGroup(_file.Group);
                if ((sections & AccessControlSections.Access) != 0) {
                    foreach (var r in _fileSystem.GetAccessRules(_file)) security.AddAccessRule(
                        new FileSystemAccessRule(r.IdentityReference, r.FileSystemRights, r.InheritanceFlags, r.PropagationFlags, r.AccessControlType));
                }
                if ((sections & AccessControlSections.Audit) != 0) {
                    foreach (var r in _fileSystem.GetAuditRules(_file)) security.AddAuditRule(
                        new FileSystemAuditRule(r.IdentityReference, r.FileSystemRights, r.InheritanceFlags, r.PropagationFlags, r.AuditFlags));
                }

                return security;
            }
        }

        public void SetSecurity(FileSystemSecurity security, AccessControlSections sections) {
            lock (Lock) {

                if ((sections & AccessControlSections.Owner) != 0) _file.Owner = (SecurityIdentifier)security.GetOwner(typeof(SecurityIdentifier));
                if ((sections & AccessControlSections.Group) != 0) _file.Group = (SecurityIdentifier)security.GetGroup(typeof(SecurityIdentifier));
                if ((sections & AccessControlSections.Access) != 0) {
                    _fileSystem.RemoveAccessRules(_file);
                    foreach (var r in security.GetAccessRules(true, false, typeof(SecurityIdentifier))) {
                        _fileSystem.AddAccessRule(_file, (FileSystemAccessRule)r);
                    }
                }
                if ((sections & AccessControlSections.Audit) != 0) {
                    _fileSystem.RemoveAuditRules(_file);
                    foreach (var r in security.GetAuditRules(true, false, typeof(SecurityIdentifier))) {
                        _fileSystem.AddAuditRule(_file, (FileSystemAuditRule)r);
                    }
                }
            }
        }


        public void SetAttributes(FileAttributes attributes) {
            lock (Lock) {
                _file.Attributes = attributes;
            }
        }


        public FileInformation GetInformation() {
            lock (Lock) {

                return new FileInformation() {
                    Attributes = _file.Attributes,
                    LastAccessTime = _file.LastAccessTime,
                    LastWriteTime = _file.LastWriteTime,
                    CreationTime = _file.CreationTime,
                    Length = _file.Length,
                    FileName = _file.Name
                };
            }
        }

        public void SetEndOfFile(long position) {
            lock (Lock) {
                if (!IsOpen) throw new InvalidOperationException();
                _fileIO.Value.SetEndOfFile(position);
            }
        }

        private Lazy<FileIO> _fileIO = null;

        private FileSystem _fileSystem;
        private object Lock = new object();
        private File _file;
    }
}
