using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using gis_photo_sharing_app.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace gis_photo_sharing_app.Application.TodoLists.Queries.ExportTodos
{
    public class ExportTodosQuery : IRequest<ExportTodosVm>
    {
        public int ListId { get; set; }
    }

    public class ExportTodosQueryHandler : IRequestHandler<ExportTodosQuery, ExportTodosVm>
    {
        private readonly IApplicationDbContext _context;
        private readonly ICsvFileBuilder _fileBuilder;

        public ExportTodosQueryHandler(IApplicationDbContext context, ICsvFileBuilder fileBuilder)
        {
            _context = context;
            _fileBuilder = fileBuilder;
        }

        public async Task<ExportTodosVm> Handle(ExportTodosQuery request, CancellationToken cancellationToken)
        {
            var records = await _context.TodoItems
                .Where(t => t.ListId == request.ListId)
                .Select(t => new TodoItemRecord { Title = t.Title, Done = t.Done })
                .ToListAsync(cancellationToken);

            return new ExportTodosVm
            {
                Content = _fileBuilder.BuildTodoItemsFile(records),
                ContentType = "text/csv",
                FileName = "TodoItems.csv",
            };
        }
    }
}
