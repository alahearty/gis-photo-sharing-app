using System.Threading;
using System.Threading.Tasks;
using gis_photo_sharing_app.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace gis_photo_sharing_app.Application.Common.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<TodoList> TodoLists { get; }
        DbSet<TodoItem> TodoItems { get; }
        DbSet<Photo> Photos { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
