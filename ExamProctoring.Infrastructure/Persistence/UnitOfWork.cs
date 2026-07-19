using ExamProctoring.Application.Common.Interfaces;
using ExamProctoring.Infrastructure.Data;
using System.Threading.Tasks;

namespace ExamProctoring.Infrastructure.Persistence
{
 public class UnitOfWork : IUnitOfWork
 {
 private readonly AppDbContext _context;

 public UnitOfWork(AppDbContext context)
 {
 _context = context;
 }

 public Task<int> SaveChangesAsync()
 {
 return _context.SaveChangesAsync();
 }
 }
}
