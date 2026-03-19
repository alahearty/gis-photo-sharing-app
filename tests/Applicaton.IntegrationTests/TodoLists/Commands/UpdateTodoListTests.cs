using System;
using System.Threading.Tasks;
using gis_photo_sharing_app.Application.Common.Exceptions;
using gis_photo_sharing_app.Application.TodoLists.Commands.CreateTodoList;
using gis_photo_sharing_app.Application.TodoLists.Commands.UpdateTodoList;
using gis_photo_sharing_app.Domain.Entities;
using FluentAssertions;
using NUnit.Framework;

namespace gis_photo_sharing_app.Application.IntegrationTests.TodoLists.Commands
{
    using static Testing;

    public class UpdateTodoListTests : TestBase
    {
        [Test]
        public async Task ShouldRequireValidTodoListId()
        {
            var command = new UpdateTodoListCommand
            {
                Id = 99,
                Title = "New Title"
            };

            Func<Task> act = () => SendAsync(command);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Test]
        public async Task ShouldRequireUniqueTitle()
        {
            var listId = await SendAsync(new CreateTodoListCommand
            {
                Title = "New List"
            });

            await SendAsync(new CreateTodoListCommand
            {
                Title = "Other List"
            });

            var command = new UpdateTodoListCommand
            {
                Id = listId,
                Title = "Other List"
            };

            Func<Task> act = () => SendAsync(command);
            var ex = await act.Should().ThrowAsync<ValidationException>();
            ex.Which.Errors.Should().ContainKey("Title");
            ex.Which.Errors["Title"].Should().Contain("The specified title already exists.");
        }

        [Test]
        public async Task ShouldUpdateTodoList()
        {
            var userId = await RunAsDefaultUserAsync();

            var listId = await SendAsync(new CreateTodoListCommand
            {
                Title = "New List"
            });

            var command = new UpdateTodoListCommand
            {
                Id = listId,
                Title = "Updated List Title"
            };

            await SendAsync(command);

            var list = await FindAsync<TodoList>(listId);

            list.Title.Should().Be(command.Title);
            list.LastModifiedBy.Should().NotBeNull();
            list.LastModifiedBy.Should().Be(userId);
            list.LastModified.Should().NotBeNull();
            list!.LastModified!.Value.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(10));
        }
    }
}
