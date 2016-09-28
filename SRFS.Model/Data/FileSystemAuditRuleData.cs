using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.AccessControl;

namespace SRFS.Model.Data {

    public class FileSystemAuditRuleData {

        public FileSystemAuditRuleData(FileSystemObjectType type, int id, FileSystemAuditRule rule) {
            _type = type;
            _id = id;
            _auditRule = rule;
        }

        public FileSystemObjectType FileSystemObjectType => _type;
        public int ID => _id;
        public FileSystemAuditRule Rule => _auditRule;

        private readonly FileSystemObjectType _type;
        private readonly int _id;
        private readonly FileSystemAuditRule _auditRule;
    }
}
