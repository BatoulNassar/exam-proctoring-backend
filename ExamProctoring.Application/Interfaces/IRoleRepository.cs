using ExamProctoring.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Common.Interfaces
{
    public interface IRoleRepository
    {
        Task<IEnumerable<Role>> GetAllWithPermissionsAsync();
        Task<Role> GetByIdWithPermissionsAsync(int roleId);
        Task<IEnumerable<Role>> GetByIdsAsync(List<int> roleIds);
        Task<Role> GetByIdAsync(int id);
    }
}
