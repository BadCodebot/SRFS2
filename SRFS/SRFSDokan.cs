using DokanNet;
using SRFS.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using FileAttributes = System.IO.FileAttributes;
using FileMode = System.IO.FileMode;
using FileOptions = System.IO.FileOptions;
using FileShare = System.IO.FileShare;
using System.Security.Principal;
using SRFS.Model.Data;

namespace SRFS {

    public class SRFSDokan : IDokanOperations {

        public SRFSDokan(FileSystem fs) {
            _fileSystem = fs;
        }

        public void Cleanup(string fileName, DokanFileInfo info) {
            (info.Context as IHandle).Messages.Add($"  Cleanup");

            IHandle h = info.Context as IHandle;
            if (info.DeleteOnClose) h.FileSystemObject.Delete();
            h.Close();
            Console.WriteLine($"HANDLE {h.ID}");
            foreach (var s in h.Messages) Console.WriteLine(s);
            Handles.DeleteHandle(h);
            info.Context = null;
        }

        public void CloseFile(string fileName, DokanFileInfo info) {
            IHandle h = info.Context as IHandle;
            if (h != null) {
                Console.WriteLine($"HANDLE {h.ID} WAS NOT CLEANED UP");
                h.Close();
                Console.WriteLine($"HANDLE {h.ID}");
                foreach (var s in h.Messages) Console.WriteLine(s);
                Handles.DeleteHandle(h);
            }
            info.Context = null;
        }

        private Dictionary<int, DokanDirectory> _openDirectories = new Dictionary<int, DokanDirectory>();
        private DirectoryHandle Open(Directory dir, FileAccess access, FileShare share) {
            DokanDirectory dd;
            if (!_openDirectories.TryGetValue(dir.ID, out dd)) {
                dd = new DokanDirectory(_fileSystem, dir);
                _openDirectories.Add(dir.ID, dd);
            }
            return dd.Open(access, share);
        }

        private Dictionary<int, DokanFile> _openFiles = new Dictionary<int, DokanFile>();
        private FileHandle Open(File file) {
            DokanFile df;
            if (!_openFiles.TryGetValue(file.ID, out df)) {
                df = new DokanFile(_fileSystem, file);
                _openFiles.Add(file.ID, df);
            }
            return df.Open();
        }

        private object createFileLock = new object();

        public NtStatus CreateFile(string fileName, DokanNet.FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes,
            DokanFileInfo info) {
            lock (createFileLock) {

                try {
                    FileHandle fileHandle = null;
                    bool alreadyExists = false;

                    string name = null;
                    Directory parentDirectory = null;
                    FileSystemObject fso = _fileSystem.GetFileSystemObject(fileName);
                    if (fso == null) {
                        parentDirectory = _fileSystem.GetParentDirectory(fileName, out name);
                        if (parentDirectory == null) {
                            Console.WriteLine($"PATH NOT FOUND CreateFile(\"{fileName}\", {access}, {share}, {mode})");
                            return DokanResult.PathNotFound;
                        }
                    }

                    if (fso is Directory) info.IsDirectory = true;

                    if (info.IsDirectory) {
                        Directory directory = null;
                        DirectoryHandle handle = null;

                        switch (mode) {
                            case FileMode.CreateNew:
                                if (fso != null) {
                                    return DokanResult.FileExists;
                                }

                                DirectoryHandle parent = null;
                                try {
                                    parent = Open(parentDirectory, FileAccess.GenericWrite, FileShare.ReadWrite);
                                    directory = _fileSystem.CreateDirectory(parentDirectory, name);
                                    handle = Open(directory, access, share);

                                    directory.Owner = WindowsIdentity.GetCurrent().User;
                                    directory.Group = WindowsIdentity.GetCurrent().User;

                                } finally {
                                    if (parent != null) {
                                        parent.Close();
                                        Handles.DeleteHandle(parent);
                                    }
                                }
                                break;

                            case FileMode.Open:
                                directory = fso as Directory;
                                if (directory == null) {
                                    Console.WriteLine($"FILE NOT FOUND CreateFile(\"{fileName}\", {access}, {share}, {mode})");
                                    return DokanResult.FileNotFound;
                                }
                                handle = Open(directory, access, share);
                                break;

                            default:
                                throw new System.IO.IOException($"Unsupported FileMode ({mode}) for a directory.");
                        }

                        // Assign the handle
                        info.Context = handle;

                    } else {
                        DirectoryHandle parentHandle = null;
                        File file;
                        switch (mode) {
                            case FileMode.Append:
                            case FileMode.Truncate:
                            case FileMode.Open:
                                file = fso as File;
                                if (file == null) {
                                    Console.WriteLine($"FILE NOT FOUND CreateFile(\"{fileName}\", {access}, {share}, {mode})");
                                    return DokanResult.FileNotFound;
                                }
                                fileHandle = Open(file);
                                break;

                            case FileMode.OpenOrCreate:
                            case FileMode.Create:
                                if (fso == null) {
                                    try {
                                        parentHandle = Open(parentDirectory, FileAccess.GenericWrite, FileShare.ReadWrite);
                                        file = _fileSystem.CreateFile(parentDirectory, name);
                                        fileHandle = Open(file);
                                    } finally {
                                        if (parentHandle != null) {
                                            parentHandle.Close();
                                            Handles.DeleteHandle(parentHandle);
                                        }
                                    }
                                } else {
                                    alreadyExists = true;
                                }
                                break;

                            case FileMode.CreateNew:
                                if (fso != null) {
                                    Console.WriteLine($"FILE EXISTS CreateFile(\"{fileName}\", {access}, {share}, {mode})");
                                    return DokanResult.FileExists;
                                }
                                try {
                                    parentHandle = Open(parentDirectory, FileAccess.GenericWrite, FileShare.ReadWrite);
                                    file = _fileSystem.CreateFile(parentDirectory, name);
                                    fileHandle = Open(file);
                                } finally {
                                    if (parentHandle != null) {
                                        parentHandle.Close();
                                        Handles.DeleteHandle(parentHandle);
                                    }
                                }
                                break;
                        }

                        // Assign the handle
                        info.Context = fileHandle;
                    }

                    if (fileHandle != null && (mode == FileMode.Create || mode == FileMode.Truncate)) fileHandle.File.SetEndOfFile(0);

                    if (access == FileAccess.Synchronize) {
                        ((IHandle)info.Context).IsSynchronize = true;
                    }

                    if (info.Context is DirectoryHandle) info.IsDirectory = true;
                    else info.IsDirectory = false;

                    Console.WriteLine($"**CreateFile(\"{fileName}\", {access}, {share}, {mode}): {(info.Context as IHandle).ID}");
                    (info.Context as IHandle).Messages.Add($"  CreateFile(\"{fileName}\", {access}, {share}, {mode})");


                    if (alreadyExists) return DokanResult.AlreadyExists;
                    else return DokanResult.Success;

                } catch (SharingException) {
                    Console.WriteLine($"SHARING VIOLATION CreateFile(\"{fileName}\", {access}, {share}, {mode})");
                    return DokanResult.SharingViolation;
                } catch (Exception e) {
                    Console.WriteLine($"ERROR CreateFile(\"{fileName}\", {access}, {share}, {mode})");
                    Console.WriteLine($"{e.Message}");
                    Console.WriteLine(e.StackTrace);
                    return DokanResult.Error;
                }
            }
        }

        public NtStatus DeleteDirectory(string fileName, DokanFileInfo info) {
            try {
                DirectoryHandle dh = info.Context as DirectoryHandle;
                if (dh.Directory.Subdirectories.Count != 0 || dh.Directory.Files.Count != 0) return DokanResult.DirectoryNotEmpty;
                return DokanResult.Success;

            } catch (InvalidHandleException) {
                return DokanResult.InvalidHandle;
            } catch (Exception) {
                return DokanResult.Error;
            }
        }

        public NtStatus DeleteFile(string fileName, DokanFileInfo info) {
            (info.Context as IHandle).Messages.Add($"  DeleteFile");

            try {
                return DokanResult.Success;

            } catch (InvalidHandleException) {
                return DokanResult.InvalidHandle;
            } catch (Exception) {
                return DokanResult.Error;
            }
        }

        public NtStatus FindFiles(string fileName, out IList<FileInformation> files, DokanFileInfo info) {

            try {
                DokanDirectory dir = ((DirectoryHandle)info.Context).Directory;
                var f = (from x in dir.Subdirectories.Values select (FileSystemObject)x).Union(
                    from x in dir.Files.Values select (FileSystemObject)x);
                files = f.Select(x => new FileInformation() {
                    FileName = x.Name,
                    Length = x is File file ? file.Length : 0,
                    LastWriteTime = x.LastWriteTime,
                    LastAccessTime = x.LastAccessTime,
                    CreationTime = x.CreationTime,
                    Attributes = (x is Directory ? x.Attributes | FileAttributes.Directory : x.Attributes)
                }).ToList();

                (info.Context as IHandle).Messages.Add($"  FindFiles: {files.Count}");

                foreach (var x in files) {
                    (info.Context as IHandle).Messages.Add($"    {x.FileName} {x.Length} {x.Attributes} {x.CreationTime} {x.LastWriteTime} {x.LastAccessTime}");
                }
                return DokanResult.Success;

            } catch (InvalidHandleException) {
                (info.Context as IHandle).Messages.Add("  FindFiles: INVALID HANDLE");
                files = null;
                return DokanResult.InvalidHandle;
            } catch (Exception) {
                (info.Context as IHandle).Messages.Add("  FindFiles: ERROR");
                files = null;
                return DokanResult.Error;
            }
        }

        public NtStatus FindStreams(string fileName, out IList<FileInformation> streams, DokanFileInfo info) {
            streams = null;
            return DokanResult.NotImplemented;
        }

        public NtStatus FlushFileBuffers(string fileName, DokanFileInfo info) {
            (info.Context as IHandle).Messages.Add($"  FlushFileBuffers");
            try {
                if (!info.IsDirectory) ((FileHandle)info.Context).File.Flush();
                return DokanResult.Success;

            } catch (InvalidHandleException) {
                (info.Context as IHandle).Messages.Add($"  FlushFileBuffers: INVALID HANDLE");
                return DokanResult.InvalidHandle;
            } catch (Exception) {
                (info.Context as IHandle).Messages.Add($"  FlushFileBuffers: ERROR");
                return DokanResult.Error;
            }
        }

        public NtStatus GetDiskFreeSpace(out long freeBytesAvailable, out long totalNumberOfBytes, out long totalNumberOfFreeBytes, DokanFileInfo info) {
            
            freeBytesAvailable = _fileSystem.TotalNumberOfFreeBytes;
            totalNumberOfBytes = _fileSystem.TotalNumberOfBytes;
            totalNumberOfFreeBytes = _fileSystem.TotalNumberOfFreeBytes;

            Console.WriteLine($"GETDISKFREESPACE: {freeBytesAvailable} {totalNumberOfBytes} {totalNumberOfFreeBytes}");
            return DokanResult.Success;
        }

        public NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, DokanFileInfo info) {

            try {
                IHandle h = info.Context as IHandle;
                fileInfo = h.FileSystemObject.GetInformation();

                (info.Context as IHandle).Messages.Add($"  GetFileInformation: {fileInfo.Length}, [{fileInfo.Attributes}], {fileInfo.CreationTime}, {fileInfo.LastWriteTime}, {fileInfo.LastAccessTime}");
                return DokanResult.Success;

            } catch (InvalidHandleException) {
                (info.Context as IHandle).Messages.Add("  GetFileInformation: INVALID HANDLE");
                fileInfo = new FileInformation();
                return DokanResult.InvalidHandle;
            } catch (Exception) {
                (info.Context as IHandle).Messages.Add("  GetFileInformation: ERROR");
                fileInfo = new FileInformation();
                return DokanResult.Error;
            }
        }

        public NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features, out string fileSystemName, DokanFileInfo info) {
            //Console.WriteLine($"GetVolumeInformation()");

            volumeLabel = Configuration.VolumeName;
            features = FileSystemFeatures.CasePreservedNames | FileSystemFeatures.CaseSensitiveSearch | FileSystemFeatures.PersistentAcls |
                FileSystemFeatures.UnicodeOnDisk | FileSystemFeatures.SupportsRemoteStorage;
            fileSystemName = "SRFS";

            return DokanResult.Success;
        }

        public NtStatus LockFile(string fileName, long offset, long length, DokanFileInfo info) {
            return DokanResult.NotImplemented;
        }

        public NtStatus Mounted(DokanFileInfo info) {
            Console.WriteLine("MOUNTED");
            return DokanResult.Success;
        }

        public NtStatus MoveFile(string oldName, string newName, bool replace, DokanFileInfo info) {
            DirectoryHandle newParentHandle = null;
            DirectoryHandle oldParentHandle = null;

            try {
                IHandle handle = info.Context as IHandle;

                string name = null;
                Directory newParentDirectory = _fileSystem.GetParentDirectory(newName, out name);
                newParentHandle = Open(newParentDirectory, FileAccess.GenericWrite | FileAccess.GenericRead, FileShare.ReadWrite);

                int parentID = handle.FileSystemObject.ParentID;
                Directory oldParentDirectory = _fileSystem.GetDirectory(parentID);
                oldParentHandle = Open(oldParentDirectory, FileAccess.GenericRead | FileAccess.GenericWrite, FileShare.ReadWrite);

                if (newParentHandle.Directory.Subdirectories.ContainsKey(name)) return DokanResult.AlreadyExists;
                if (newParentHandle.Directory.Files.ContainsKey(name)) return DokanResult.AlreadyExists;

                handle.FileSystemObject.Move(newParentDirectory, name);

                return DokanResult.Success;

            } catch (InvalidHandleException) {
                return DokanResult.InvalidHandle;
            } catch (Exception) {
                return DokanResult.Error;
            } finally {
                if (newParentHandle != null) {
                    newParentHandle.Close();
                    Handles.DeleteHandle(newParentHandle);
                }
                if (oldParentHandle != null) {
                    oldParentHandle.Close();
                    Handles.DeleteHandle(oldParentHandle);
                }
            }
        }

        public NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, DokanFileInfo info) {

            try {
                bytesRead = ((FileHandle)info.Context).File.ReadFile(buffer, offset);
                //(info.Context as IHandle).Messages.Add($"  ReadFile({buffer.Length}, {offset}): {bytesRead}");
                return DokanResult.Success;

            } catch (InvalidHandleException) {
                (info.Context as IHandle).Messages.Add("  ReadFile: INVALID HANDLE");
                bytesRead = 0;
                return DokanResult.InvalidHandle;
            } catch (Exception) {
                (info.Context as IHandle).Messages.Add("  ReadFile: ERROR");
                bytesRead = 0;
                return DokanResult.Error;
            }
        }

        public NtStatus SetAllocationSize(string fileName, long length, DokanFileInfo info) {
            (info.Context as IHandle).Messages.Add($"  SetAllocationSize({length})");

            try {
                FileHandle h = (FileHandle)info.Context;
                h.File.SetEndOfFile(length);
                return DokanResult.Success;
            } catch (InvalidHandleException) {
                return DokanResult.InvalidHandle;
            } catch (Exception) {
                return DokanResult.Error;
            }
        }

        public NtStatus SetEndOfFile(string fileName, long length, DokanFileInfo info) {
            (info.Context as IHandle).Messages.Add($"  SetEndOfFile({length})");

            try {
                FileHandle h = (FileHandle)info.Context;
                h.File.SetEndOfFile(length);
                return DokanResult.Success;
            } catch (InvalidHandleException) {
                return DokanResult.InvalidHandle;
            } catch (Exception) {
                return DokanResult.Error;
            }
        }

        public NtStatus SetFileAttributes(string fileName, FileAttributes attributes, DokanFileInfo info) {
            (info.Context as IHandle).Messages.Add($"  SetFileAttributes([{attributes}])");

            try {
                IHandle h = info.Context as IHandle;
                h.FileSystemObject.SetAttributes(attributes);
                return DokanResult.Success;

            } catch (InvalidHandleException) {
                return DokanResult.InvalidHandle;
            } catch (Exception) {
                return DokanResult.Error;
            }
        }

        public NtStatus GetFileSecurity(string fileName, out FileSystemSecurity security, AccessControlSections sections, DokanFileInfo info) {
            (info.Context as IHandle).Messages.Add($"  GetFileSecurity([{sections}])");

            try {
                IHandle h = info.Context as IHandle;
                security = h.FileSystemObject.GetSecurity(sections);

                if ((sections & AccessControlSections.Owner) != 0) h.Messages.Add($"    Owner: {security.GetOwner(typeof(NTAccount))}");
                if ((sections & AccessControlSections.Group) != 0) h.Messages.Add($"    Group: {security.GetGroup(typeof(NTAccount))}");
                if ((sections & AccessControlSections.Access) != 0) {
                    foreach (var r in security.GetAccessRules(true, false, typeof(NTAccount))) {
                        FileSystemAccessRule rule = (FileSystemAccessRule)r;
                        h.Messages.Add($"    Access: {rule.AccessControlType} {rule.IdentityReference} [{rule.FileSystemRights}] {(rule.IsInherited ? "Inherited" : "Explicit")} [{rule.InheritanceFlags}] [{rule.PropagationFlags}]");
                    }
                }

                return DokanResult.Success;

            } catch (InvalidHandleException) {
                (info.Context as IHandle).Messages.Add("    INVALID HANDLE");
                security = null;
                return DokanResult.InvalidHandle;
            } catch (Exception) {
                (info.Context as IHandle).Messages.Add("    ERROR");
                security = null;
                return DokanResult.Error;
            }
        }

        public NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections, DokanFileInfo info) {
            (info.Context as IHandle).Messages.Add($"  SetFileSecurity([{sections}])");

            try {
                IHandle h = info.Context as IHandle;

                if ((sections & AccessControlSections.Owner) != 0) h.Messages.Add($"    Owner: {security.GetOwner(typeof(NTAccount))}");
                if ((sections & AccessControlSections.Group) != 0) h.Messages.Add($"    Group: {security.GetGroup(typeof(NTAccount))}");
                if ((sections & AccessControlSections.Access) != 0) {
                    foreach (var r in security.GetAccessRules(true, false, typeof(NTAccount))) {
                        FileSystemAccessRule rule = (FileSystemAccessRule)r;
                        h.Messages.Add($"    Access: {rule.AccessControlType} {rule.IdentityReference} [{rule.FileSystemRights}] {(rule.IsInherited ? "Inherited" : "Explicit")} [{rule.InheritanceFlags}] [{rule.PropagationFlags}]");
                    }
                }

                h.FileSystemObject.SetSecurity(security, sections);
                return DokanResult.Success;

            } catch (InvalidHandleException) {
                return DokanResult.InvalidHandle;
            } catch (Exception) {
                return DokanResult.Error;
            }
        }

        public NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, DokanFileInfo info) {
            (info.Context as IHandle).Messages.Add($"  SetFileTime({(creationTime.HasValue ? creationTime.Value.ToString() : "null")}, {(lastAccessTime.HasValue ? lastAccessTime.Value.ToString() : "null")}, {(lastWriteTime.HasValue ? lastWriteTime.Value.ToString() : "null")})");

            try {
                IHandle h = info.Context as IHandle;
                h.FileSystemObject.SetTime(creationTime, lastAccessTime, lastWriteTime);
                return DokanResult.Success;

            } catch (InvalidHandleException) {
                return DokanResult.InvalidHandle;
            } catch (Exception) {
                return DokanResult.Error;
            }
        }

        public NtStatus UnlockFile(string fileName, long offset, long length, DokanFileInfo info) {
            return DokanResult.NotImplemented;
        }

        public NtStatus Unmounted(DokanFileInfo info) {
            // TODO: Flush and close all file handles
            return DokanResult.Success;
        }

        public NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, DokanFileInfo info) {

            try {
                bytesWritten = ((FileHandle)info.Context).File.WriteFile(buffer, offset);
                //(info.Context as IHandle).Messages.Add($"  WriteFile({buffer.Length}, {offset}): {bytesWritten}");
                return DokanResult.Success;

            } catch (InvalidHandleException) {
                bytesWritten = 0;
                return DokanResult.InvalidHandle;
            } catch (Exception e) {
                Console.WriteLine($"ERROR in WriteFile({fileName}, {buffer.Length}, {offset})");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                bytesWritten = 0;
                return DokanResult.Error;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------

        //private static void Debug(string message, [CallerMemberName] string caller = null) {
        //    Log.Debug($"{caller} called: {message}");
        //}

        //private static void Debug(DokanFileInfo info, [CallerMemberName] string caller = null) {
        //    Log.Debug($"{caller} called, info{{context={info?.Context}{(info?.IsDirectory ?? false ? ", DIRECTORY" : "")}{(info?.DeleteOnClose ?? false ? ", DELETEONCLOSE" : "")}}}");
        //}

        //private static void Debug2(DokanFileInfo info, string message, [CallerMemberName] string caller = null) {
        //    Log.Debug($"{caller} called, info{{context={info?.Context}{(info?.IsDirectory ?? false ? ", DIRECTORY" : "")}{(info?.DeleteOnClose ?? false ? ", DELETEONCLOSE" : "")}}}: {message}");
        //}

        //private static void Error(Exception e, [CallerMemberName] string caller = null) {
        //    Log.Error($"Error in {caller}: {e.Message}");
        //    Console.WriteLine(e.StackTrace);
        //}

        //private static void Error(string message, [CallerMemberName] string callerName = null, [CallerFilePath] string sourcePath = null, [CallerLineNumber] int sourceLineNumber = 0) {
        //    Log.Error($"Error in {callerName}: {message} ({sourcePath}:{sourceLineNumber})");
        //}

        public NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files, DokanFileInfo info) {
            files = new List<FileInformation>();
            return DokanResult.NotImplemented;
        }

        private FileSystem _fileSystem;
    }
}
