using System;
using gis_photo_sharing_app.Application.Common.Exceptions;
using gis_photo_sharing_app.Application.TodoLists.Commands.CreateTodoList;
using gis_photo_sharing_app.Application.TodoLists.Commands.DeleteTodoList;
using gis_photo_sharing_app.Domain.Entities;
using FluentAssertions;
using NUnit.Framework;
using System.Threading.Tasks;

namespace gis_photo_sharing_app.Application.IntegrationTests.TodoLists.Commands
{
    using static Testing;

    public class DeleteTodoListTests : TestBase
    {
        [Test]
        public async Task ShouldRequireValidTodoListId()
        {
            var command = new DeleteTodoListCommand { Id = 99 };

            Func<Task> act = () => SendAsync(command);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Test]
        public async Task ShouldDeleteTodoList()
        {
            var listId = await SendAsync(new CreateTodoListCommand
            {
                Title = "New List"
            });

            await SendAsync(new DeleteTodoListCommand 
            { 
                Id = listId 
            });

            var list = await FindAsync<TodoList>(listId);

            list.Should().BeNull();
        }
    }
}
