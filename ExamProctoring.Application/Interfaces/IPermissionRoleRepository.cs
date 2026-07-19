using ExamProctoring.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Interfaces
{
    public interface IPermissionRoleRepository
    {
        Task AddAsync(PermissionRole permissionRole);
        Task RemoveByRoleIdAsync(int roleId);
        Task RemoveAsync(int roleId, int permissionId);
    }
}
