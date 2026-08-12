using ExamProctoring.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExamProctoring.Application.Common.Interfaces
{
 public interface IPermissionRepository
 {
 Task<Permission> GetByNameAsync(string permissionName);
 Task<IList<Permission>> GetByNamesAsync(IEnumerable<string> permissionNames);

 /// <summary>Every permission the system defines, for the roles screen to list.</summary>
 Task<IList<Permission>> GetAllAsync();
 }
}
