using System.Collections.Generic;
using System.Security.AccessControl;

namespace SRFS.Model.Data {

    public class DirectoryEntry : FileSystemEntry {

        public DirectoryEntry(FileSystem fileSystem, int id, string name) : base(fileSystem, id, name) { }

        public IDictionary<string,DirectoryEntry> SubDirectories => FileSystem.GetContainedDirectories(this);

        public IDictionary<string, FileEntry> Files => FileSystem.GetContainedFiles(this);
    }
}
