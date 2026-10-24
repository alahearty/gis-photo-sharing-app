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

        public static async Task SeedSampleDataAsync(ApplicationDbContext context, string? samplePhotoOwnerId = null)
        {
            // Seed, if necessary
            if (!context.TodoLists.Any())
            {
                context.TodoLists.Add(new TodoList
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
                        new TodoItem { Title = "Tuna", Note = "" },
                        new TodoItem { Title = "Water", Note = "" }
                    }
                });

                await context.SaveChangesAsync();
            }

            if (!context.Photos.Any())
            {
                context.Photos.AddRange(
                    new Photo
                    {
                        Title = "Port Harcourt city centre",
                        Description = "Demonstration photo for the default map view.",
                        FilePath = "/uploads/sample1.jpg",
                        Latitude = 4.8156,
                        Longitude = 7.0498,
                        LocationName = "Port Harcourt, Rivers State, Nigeria",
                        Category = "General",
                        IsPublic = true,
                        CreatedByUserId = samplePhotoOwnerId,
                    },
                    new Photo
                    {
                        Title = "Rumuola flyover",
                        Description = "Demonstration photo near the Rumuola interchange.",
                        FilePath = "/uploads/sample2.jpg",
                        Latitude = 4.8246,
                        Longitude = 7.0358,
                        LocationName = "Rumuola, Port Harcourt",
                        Category = "Infrastructure",
                        IsPublic = true,
                        CreatedByUserId = samplePhotoOwnerId,
                    },
                    new Photo
                    {
                        Title = "Marine Base shoreline",
                        Description = "Demonstration photo along the Port Harcourt waterfront.",
                        FilePath = "/uploads/sample3.jpg",
                        Latitude = 4.7721,
                        Longitude = 7.0203,
                        LocationName = "Marine Base, Port Harcourt",
                        Category = "Environment",
                        IsPublic = true,
                        CreatedByUserId = samplePhotoOwnerId,
                    }
                );
                await context.SaveChangesAsync();
            }
        }
    }
}
