using gis_photo_sharing_app.Application.TodoLists.Queries.GetTodos;
using gis_photo_sharing_app.Domain.Entities;
using FluentAssertions;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace gis_photo_sharing_app.Application.IntegrationTests.TodoLists.Queries
{
    using static Testing;

    public class GetTodosTests : TestBase
    {
        [Test]
        public async Task ShouldReturnPriorityLevels()
        {
            var query = new GetTodosQuery();

            var result = await SendAsync(query);

            result.PriorityLevels.Should().NotBeEmpty();
        }

        [Test]
        public async Task ShouldReturnAllListsAndItems()
        {
            await AddAsync(new TodoList
            {
                Title = "Shopping",
                Colour = "#FFFFFF",
                Items =
                    {
                        new TodoItem { Title = "Apples", Note = "", Done = true },
                        new TodoItem { Title = "Milk", Note = "", Done = true },
                        new TodoItem { Title = "Bread", Note = "", Done = true },
                        new TodoItem { Title = "Toilet paper", Note = "" },
                        new TodoItem { Title = "Pasta", Note = "" },
                        new TodoItem { Title = "Tissues", Note = "" },
                        new TodoItem { Title = "Tuna", Note = "" }
                    }
            });

            var query = new GetTodosQuery();

            var result = await SendAsync(query);

            result.Lists.Should().HaveCount(1);
            result.Lists.First().Items.Should().HaveCount(7);
        }
    }
}
