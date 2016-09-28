using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.AccessControl;

namespace SRFS.Model.Data {

    public class FileSystemAccessRuleData {

        public FileSystemAccessRuleData(FileSystemObjectType type, int id, FileSystemAccessRule rule) {
            _type = type;
            _id = id;
            _accessRule = rule;
        }

        public FileSystemObjectType FileSystemObjectType => _type;
        public int ID => _id;
        public FileSystemAccessRule Rule => _accessRule;

        private readonly FileSystemObjectType _type;
        private readonly int _id;
        private readonly FileSystemAccessRule _accessRule;
    }
}
