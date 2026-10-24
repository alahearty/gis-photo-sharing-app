using System.Collections.Generic;

namespace gis_photo_sharing_app.Application.TodoLists.Queries.GetTodos
{
    public class TodoListDto
    {
        public TodoListDto()
        {
            Items = new List<TodoItemDto>();
        }

        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public IList<TodoItemDto> Items { get; set; }
    }
}
