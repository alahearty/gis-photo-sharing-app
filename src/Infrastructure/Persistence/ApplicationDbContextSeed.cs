using gis_photo_sharing_app.Domain.Entities;
using gis_photo_sharing_app.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;

namespace gis_photo_sharing_app.Infrastructure.Persistence
{
    public static class ApplicationDbContextSeed
    {
        public static async Task SeedDefaultUserAsync(UserManager<ApplicationUser> userManager)
        {
            var defaultUser = new ApplicationUser { UserName = "administrator@localhost", Email = "administrator@localhost" };

            if (userManager.Users.All(u => u.UserName != defaultUser.UserName))
            {
                await userManager.CreateAsync(defaultUser, "Administrator1!");
            }
        }

        public static async Task SeedSampleDataAsync(ApplicationDbContext context)
        {
            // Seed, if necessary
            if (!context.TodoLists.Any())
            {
                context.TodoLists.Add(new TodoList
                {
                    Title = "Shopping",
                    Items =
                    {
                        new TodoItem { Title = "Apples", Done = true },
                        new TodoItem { Title = "Milk", Done = true },
                        new TodoItem { Title = "Bread", Done = true },
                        new TodoItem { Title = "Toilet paper" },
                        new TodoItem { Title = "Pasta" },
                        new TodoItem { Title = "Tissues" },
                        new TodoItem { Title = "Tuna" },
                        new TodoItem { Title = "Water" }
                    }
                });

                await context.SaveChangesAsync();
            }

            if (!context.Photos.Any())
            {
                context.Photos.AddRange(
                    new Photo { Title = "Central Park", Description = "Beautiful day in NYC", FilePath = "/uploads/sample1.jpg", Latitude = 40.7851, Longitude = -73.9683 },
                    new Photo { Title = "Brooklyn Bridge", Description = "Sunset view", FilePath = "/uploads/sample2.jpg", Latitude = 40.7061, Longitude = -73.9969 },
                    new Photo { Title = "Times Square", Description = "Night lights", FilePath = "/uploads/sample3.jpg", Latitude = 40.7580, Longitude = -73.9855 }
                );
                await context.SaveChangesAsync();
            }
        }
    }
}
