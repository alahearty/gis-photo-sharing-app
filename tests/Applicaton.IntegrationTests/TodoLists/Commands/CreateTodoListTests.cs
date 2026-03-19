using gis_photo_sharing_app.Application.Common.Exceptions;
using gis_photo_sharing_app.Application.TodoLists.Commands.CreateTodoList;
using gis_photo_sharing_app.Domain.Entities;
using FluentAssertions;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace gis_photo_sharing_app.Application.IntegrationTests.TodoLists.Commands
{
    using static Testing;

    public class CreateTodoListTests : TestBase
    {
        [Test]
        public async Task ShouldRequireMinimumFields()
        {
            var command = new CreateTodoListCommand();

            Func<Task> act = () => SendAsync(command);
            await act.Should().ThrowAsync<ValidationException>();
        }

        [Test]
        public async Task ShouldRequireUniqueTitle()
        {
            await SendAsync(new CreateTodoListCommand
            {
                Title = "Shopping"
            });

            var command = new CreateTodoListCommand
            {
                Title = "Shopping"
            };

            Func<Task> act = () => SendAsync(command);
            await act.Should().ThrowAsync<ValidationException>();
        }

        [Test]
        public async Task ShouldCreateTodoList()
        {
            var userId = await RunAsDefaultUserAsync();

            var command = new CreateTodoListCommand
            {
                Title = "Tasks"
            };

            var id = await SendAsync(command);

            var list = await FindAsync<TodoList>(id);

            list.Should().NotBeNull();
            list.Title.Should().Be(command.Title);
            list.CreatedBy.Should().Be(userId);
            list.Created.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(10));
        }
    }
}
