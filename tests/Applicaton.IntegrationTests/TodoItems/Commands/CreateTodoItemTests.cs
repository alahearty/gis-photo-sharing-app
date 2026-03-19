using gis_photo_sharing_app.Application.Common.Exceptions;
using gis_photo_sharing_app.Application.TodoItems.Commands.CreateTodoItem;
using gis_photo_sharing_app.Application.TodoLists.Commands.CreateTodoList;
using gis_photo_sharing_app.Domain.Entities;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace gis_photo_sharing_app.Application.IntegrationTests.TodoItems.Commands
{
    using static Testing;

    public class CreateTodoItemTests : TestBase
    {
        [Test]
        public async Task ShouldRequireMinimumFields()
        {
            var command = new CreateTodoItemCommand();

            Func<Task> act = () => SendAsync(command);
            await act.Should().ThrowAsync<ValidationException>();
        }

        [Test]
        public async Task ShouldCreateTodoItem()
        {
            var userId = await RunAsDefaultUserAsync();

            var listId = await SendAsync(new CreateTodoListCommand
            {
                Title = "New List"
            });

            var command = new CreateTodoItemCommand
            {
                ListId = listId,
                Title = "Tasks"
            };

            var itemId = await SendAsync(command);

            var item = await FindAsync<TodoItem>(itemId);

            item.Should().NotBeNull();
            item.ListId.Should().Be(command.ListId);
            item.Title.Should().Be(command.Title);
            item.CreatedBy.Should().Be(userId);
            item!.Created.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(10));
            item.LastModifiedBy.Should().BeNull();
            item.LastModified.Should().BeNull();
        }
    }
}
