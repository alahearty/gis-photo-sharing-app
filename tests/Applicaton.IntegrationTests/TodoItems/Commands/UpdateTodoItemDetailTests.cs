using gis_photo_sharing_app.Application.Common.Exceptions;
using gis_photo_sharing_app.Application.TodoItems.Commands.CreateTodoItem;
using gis_photo_sharing_app.Application.TodoItems.Commands.UpdateTodoItem;
using gis_photo_sharing_app.Application.TodoItems.Commands.UpdateTodoItemDetail;
using gis_photo_sharing_app.Application.TodoLists.Commands.CreateTodoList;
using gis_photo_sharing_app.Domain.Entities;
using gis_photo_sharing_app.Domain.Enums;
using FluentAssertions;
using System.Threading.Tasks;
using NUnit.Framework;
using System;

namespace gis_photo_sharing_app.Application.IntegrationTests.TodoItems.Commands
{
    using static Testing;

    public class UpdateTodoItemDetailTests : TestBase
    {
        [Test]
        public async Task ShouldRequireValidTodoItemId()
        {
            var command = new UpdateTodoItemDetailCommand
            {
                Id = 99,
                ListId = 1,
                Note = "Note",
                Priority = PriorityLevel.None
            };

            Func<Task> act = () => SendAsync(command);
            await act.Should().ThrowAsync<NotFoundException>();
        }

        [Test]
        public async Task ShouldUpdateTodoItem()
        {
            var userId = await RunAsDefaultUserAsync();

            var listId = await SendAsync(new CreateTodoListCommand
            {
                Title = "New List"
            });

            var itemId = await SendAsync(new CreateTodoItemCommand
            {
                ListId = listId,
                Title = "New Item"
            });

            var command = new UpdateTodoItemDetailCommand
            {
                Id = itemId,
                ListId = listId,
                Note = "This is the note.",
                Priority = PriorityLevel.High
            };

            await SendAsync(command);

            var item = await FindAsync<TodoItem>(itemId);

            item.ListId.Should().Be(command.ListId);
            item.Note.Should().Be(command.Note);
            item.Priority.Should().Be(command.Priority);
            item.LastModifiedBy.Should().NotBeNull();
            item.LastModifiedBy.Should().Be(userId);
            item.LastModified.Should().NotBeNull();
            item!.LastModified!.Value.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(10));
        }
    }
}
