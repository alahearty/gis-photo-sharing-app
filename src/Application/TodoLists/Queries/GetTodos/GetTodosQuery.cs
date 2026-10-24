using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using gis_photo_sharing_app.Application.Common.Interfaces;
using gis_photo_sharing_app.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace gis_photo_sharing_app.Application.TodoLists.Queries.GetTodos
{
    public class GetTodosQuery : IRequest<TodosVm>
    {
    }

    public class GetTodosQueryHandler : IRequestHandler<GetTodosQuery, TodosVm>
    {
        private readonly IApplicationDbContext _context;

        public GetTodosQueryHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<TodosVm> Handle(GetTodosQuery request, CancellationToken cancellationToken)
        {
            return new TodosVm
            {
                PriorityLevels = Enum.GetValues(typeof(PriorityLevel))
                    .Cast<PriorityLevel>()
                    .Select(p => new PriorityLevelDto { Value = (int)p, Name = p.ToString() })
                    .ToList(),

                Lists = await _context.TodoLists
                    .OrderBy(t => t.Title)
                    .Select(t => new TodoListDto
                    {
                        Id = t.Id,
                        Title = t.Title,
                        Items = t.Items
                            .OrderBy(i => i.Id)
                            .Select(i => new TodoItemDto
                            {
                                Id = i.Id,
                                ListId = i.ListId,
                                Title = i.Title,
                                Done = i.Done,
                                Priority = (int)i.Priority,
                                Note = i.Note,
                            })
                            .ToList(),
                    })
                    .ToListAsync(cancellationToken),
            };
        }
    }
}
